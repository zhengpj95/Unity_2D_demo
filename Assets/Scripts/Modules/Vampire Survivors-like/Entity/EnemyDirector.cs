using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace VampireSurvivorsLike
{

  public class EnemyDirector : SingletonMono<EnemyDirector>
  {
    // EnemyDirector 持有当前场景的 Player、敌人容器和 Wave 运行时计时，重开时必须随场景重建。
    protected override bool PersistAcrossScenes => false;

    [Tooltip("可随机生成的敌人 Prefab 列表；列表为空时不会生成敌人。")]
    [SerializeField] private GameObject[] enemyPrefab;
    [Tooltip("每次生成的时间间隔（秒）。")]
    [SerializeField, Min(0.01f)] private float spawnInterval = 1f;
    [Tooltip("每次生成尝试的敌人数量。")]
    [SerializeField, Min(1)] private int spawnCount = 1;
    [Tooltip("场景中同时存活的敌人上限，不包含已回收到对象池的敌人。")]
    [SerializeField] private int maxEnemies = 20;
    [Tooltip("生成后的敌人父节点；只用于整理层级，不改变敌人的世界坐标。")]
    [SerializeField] private Transform enemyContainer;
    [Header("Infinite Map Spawn")]
    [Tooltip("敌人生成与回收的中心。应直接绑定 Hero；缺省时仅在启动阶段按 Player 标签解析一次。")]
    [FormerlySerializedAs("spawnCenter")]
    [SerializeField] private Transform player;
    [Tooltip("敌人以 Player 为圆心的生成半径（世界单位）。")]
    [FormerlySerializedAs("maxSpawnDistance")]
    // 相机正交 Size=10 时，10 的半径可让敌人从镜头外开始追击。
    [SerializeField, Min(0f)] private float spawnRadius = 10f;
    [Tooltip("敌人与 Player 超过此距离时回收到对象池（世界单位）。应显著大于生成半径。")]
    [FormerlySerializedAs("recycleDistance")]
    // 回收半径比生成半径多 10，避免敌人在镜头边缘反复生成/回收。
    [SerializeField, Min(0f)] private float despawnRadius = 20f;
    [Tooltip("启动时为每种敌人预创建的对象数量。设为 0 可关闭预热。")]
    [SerializeField, Min(0)] private int preloadCountPerPrefab = 3;

    [Header("Wave System")]
    [Tooltip("按游戏时间切换的 Wave 配置；为空时继续使用上面的固定频率刷怪参数。")]
    [SerializeField] private WaveConfig[] waves;

    private float timer;
    private float gameTime;
    private EnemySpawner _spawner;
    private bool _playerResolutionAttempted;
    private bool _waveSystemEnabled;
    private int _currentWaveIndex = -1;
    private readonly List<WaveConfig> _orderedWaves = new List<WaveConfig>();
    private readonly List<SpawnEntryRuntime> _activeSpawnEntries = new List<SpawnEntryRuntime>();

    private readonly List<EnemyChasing> enemies = new List<EnemyChasing>();
    public int KillEnemyCount { get; set; } = 0;
    public float DespawnSqrDistance => despawnRadius * despawnRadius;
    /// <summary>当前战斗已累计的游戏时间；受 Time.timeScale 影响。</summary>
    public float GameTime => gameTime;
    /// <summary>当前 Wave 的从零开始索引；没有生效 Wave 时返回 -1。</summary>
    public int CurrentWaveIndex => _currentWaveIndex;
    /// <summary>当前 Wave 的显示编号；没有生效 Wave 时返回 0。</summary>
    public int CurrentWaveNumber => _currentWaveIndex < 0 ? 0 : _currentWaveIndex + 1;

    /// <summary>记录一个 SpawnEntry 在当前 Wave 中的运行时计时器，不修改配置资源。</summary>
    private sealed class SpawnEntryRuntime
    {
      public readonly SpawnEntry Config;
      public float Timer;

      public SpawnEntryRuntime(SpawnEntry config)
      {
        Config = config;
        // 首次进入 Wave 时允许下一帧立即生成，保持旧版启动即刷怪的体验。
        Timer = config.SpawnInterval;
      }
    }

    protected override void Awake()
    {
      base.Awake();
      ResolvePlayer();
      _spawner = new EnemySpawner(enemyPrefab, enemyContainer);
      BuildWaveSchedule();
      _waveSystemEnabled = waves != null && waves.Length > 0;
    }

    private void Start()
    {
      if (_waveSystemEnabled)
      {
        PreloadWaveEnemies();
        return;
      }

      PreloadEnemies();
      SpawnEnemies();
    }

    private void Update()
    {
      if (_waveSystemEnabled)
      {
        UpdateWave();
        return;
      }

      timer += Time.deltaTime;

      if (timer >= spawnInterval)
      {
        timer -= spawnInterval;
        SpawnEnemies();
      }
    }

    private void SpawnEnemies()
    {
      if (_spawner == null || !ResolvePlayer() || enemies.Count >= maxEnemies)
        return;

      int count = Mathf.Min(spawnCount, maxEnemies - enemies.Count);
      for (int i = 0; i < count; i++)
        _spawner.Spawn(player, spawnRadius, this);
    }

    /// <summary>累计游戏时间、切换生效 Wave，并驱动每个 SpawnEntry 的独立计时器。</summary>
    private void UpdateWave()
    {
      gameTime += Time.deltaTime;
      int nextWaveIndex = FindActiveWaveIndex(gameTime);
      if (nextWaveIndex != _currentWaveIndex)
        ActivateWave(nextWaveIndex);

      if (_currentWaveIndex < 0)
        return;

      for (int i = 0; i < _activeSpawnEntries.Count; i++)
        TickSpawnEntry(_activeSpawnEntries[i]);
    }

    /// <summary>查找满足 StartTime <= GameTime < EndTime 的 Wave。</summary>
    private int FindActiveWaveIndex(float currentTime)
    {
      for (int i = 0; i < _orderedWaves.Count; i++)
      {
        if (_orderedWaves[i] != null && _orderedWaves[i].Contains(currentTime))
          return i;
      }

      return -1;
    }

    /// <summary>切换 Wave 时重建运行时条目，旧 Wave 的计时器不会带入下一 Wave。</summary>
    private void ActivateWave(int waveIndex)
    {
      _currentWaveIndex = waveIndex;
      _activeSpawnEntries.Clear();

      if (waveIndex < 0 || waveIndex >= _orderedWaves.Count)
        return;

      WaveConfig wave = _orderedWaves[waveIndex];
      if (wave == null || wave.SpawnEntries == null)
        return;

      for (int i = 0; i < wave.SpawnEntries.Count; i++)
      {
        SpawnEntry entry = wave.SpawnEntries[i];
        if (entry == null || !entry.IsValid)
        {
          Debug.LogWarning($"[EnemyDirector] Wave '{wave.name}' 包含无效 SpawnEntry，已跳过。", wave);
          continue;
        }

        _activeSpawnEntries.Add(new SpawnEntryRuntime(entry));
      }
    }

    /// <summary>推进单个条目的计时器并按其配置向 EnemySpawner 请求生成。</summary>
    private void TickSpawnEntry(SpawnEntryRuntime runtime)
    {
      if (runtime == null || runtime.Config == null || !runtime.Config.IsValid)
        return;

      float interval = runtime.Config.SpawnInterval;
      runtime.Timer += Time.deltaTime;
      if (runtime.Timer < interval)
        return;

      // 满载时保留一个触发周期，等场上敌人回收后再继续，不因暂停期间积累大量补刷。
      if (enemies.Count < maxEnemies)
      {
        int count = Mathf.Min(runtime.Config.SpawnCount, maxEnemies - enemies.Count);
        for (int i = 0; i < count; i++)
          _spawner.Spawn(player, spawnRadius, this, runtime.Config.EnemyPrefab);
      }

      // 只处理一次触发，避免低帧率时一帧补刷过多；减去间隔可避免长期节奏漂移。
      runtime.Timer -= interval;
      if (runtime.Timer > interval)
        runtime.Timer = interval;
    }

    /// <summary>复制并排序 Wave 引用，同时对空配置、时间倒置和重叠区间输出警告。</summary>
    private void BuildWaveSchedule()
    {
      _orderedWaves.Clear();
      if (waves == null)
        return;

      for (int i = 0; i < waves.Length; i++)
      {
        if (waves[i] != null)
          _orderedWaves.Add(waves[i]);
      }

      _orderedWaves.Sort((left, right) => left.StartTime.CompareTo(right.StartTime));
      for (int i = 0; i < _orderedWaves.Count; i++)
      {
        WaveConfig wave = _orderedWaves[i];
        if (wave.EndTime <= wave.StartTime)
          Debug.LogWarning($"[EnemyDirector] Wave '{wave.name}' 的 EndTime 必须大于 StartTime。", wave);

        if (i > 0 && _orderedWaves[i - 1].EndTime > wave.StartTime)
        {
          Debug.LogWarning(
            $"[EnemyDirector] Wave '{_orderedWaves[i - 1].name}' 与 '{wave.name}' 的时间区间重叠。",
            wave);
        }
      }
    }

    private bool ResolvePlayer()
    {
      if (player != null) return true;
      if (_playerResolutionAttempted) return false;

      _playerResolutionAttempted = true;
      GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
      if (playerObject != null)
        player = playerObject.transform;

      return player != null;
    }

    private void PreloadEnemies()
    {
      if (enemyPrefab == null || preloadCountPerPrefab <= 0) return;

      foreach (GameObject prefab in enemyPrefab)
      {
        if (prefab != null) PoolManager.Instance.Preload(prefab, preloadCountPerPrefab);
      }
    }

    /// <summary>预热所有 Wave 引用的敌人预制体；相同预制体只预热一次。</summary>
    private void PreloadWaveEnemies()
    {
      if (preloadCountPerPrefab <= 0)
        return;

      var prefabs = new HashSet<GameObject>();
      for (int i = 0; i < _orderedWaves.Count; i++)
      {
        WaveConfig wave = _orderedWaves[i];
        if (wave == null || wave.SpawnEntries == null)
          continue;

        for (int j = 0; j < wave.SpawnEntries.Count; j++)
        {
          SpawnEntry entry = wave.SpawnEntries[j];
          if (entry != null && entry.IsValid)
            prefabs.Add(entry.EnemyPrefab);
        }
      }

      foreach (GameObject prefab in prefabs)
        PoolManager.Instance.Preload(prefab, preloadCountPerPrefab);
    }

    public void RecycleEnemy(GameObject enemy)
    {
      if (enemy != null && enemy.activeSelf)
      {
        PoolManager.Instance.Free(enemy);
      }
    }

    public void SpeedUpSpawnRate()
    {
      // Wave 模式由 SpawnEntry 控制节奏，旧版 spawnInterval 只在兼容模式下调整。
      if (!_waveSystemEnabled)
        spawnInterval = Mathf.Max(0.1f, spawnInterval - 0.2f);
      maxEnemies *= 2;
    }

    #region Enemy Register
    public void RegisterEnemy(EnemyChasing e)
    {
      if (e != null && !enemies.Contains(e)) enemies.Add(e);
    }

    public void UnregisterEnemy(EnemyChasing e)
    {
      enemies.Remove(e);
    }

    /**
     * Get the closest enemy from center.
     * @param center The center point to compare distance.
     * @param maxRange The maximum range to compare distance.
     * @return The closest enemy from center.
     */
    public EnemyChasing GetCloseest(Vector3 center, float maxRange = Mathf.Infinity)
    {
      if (enemies.Count == 0)
      {
        return null;
      }
      EnemyChasing enemy = null;
      float bestSqrDist = float.MaxValue;
      float maxSqr = float.IsInfinity(maxRange) ? float.MaxValue : maxRange * maxRange;
      foreach (var e in enemies)
      {
        if (e == null) continue;

        float sqrDist = (e.transform.position - center).sqrMagnitude;
        if (sqrDist < bestSqrDist && sqrDist <= maxSqr)
        {
          bestSqrDist = sqrDist;
          enemy = e;
        }
      }
      return enemy;
    }

    /**
     * Get random enemy from enemies list sorted by distance from center.
     * @param center The center point to compare distance.
     * @param maxRange The maximum range to compare distance.
     * @return The random enemy from enemies list sorted by distance from center.
     */
    public EnemyChasing GetRandom(Vector3 center, float maxRange = Mathf.Infinity)
    {
      if (enemies.Count == 0)
      {
        return null;
      }
      var list = GetSortedByDistance(center, maxRange);
      if (list.Count == 0)
      {
        return null;
      }
      int randomIndex = Random.Range(0, list.Count);
      return list[randomIndex];
    }

    /**
     * Get enemies sorted by distance from center.
     * @param center The center point to compare distance.
     * @param maxRange The maximum range to compare distance.
     * @return A list of enemies sorted by distance from center.
     */
    public List<EnemyChasing> GetSortedByDistance(Vector3 center, float maxRange = Mathf.Infinity)
    {
      var candidates = new List<EnemyChasing>(enemies.Count);
      float maxSqr = float.IsInfinity(maxRange) ? float.MaxValue : maxRange * maxRange;
      foreach (var e in enemies)
      {
        if (e == null) continue;
        if (!e.gameObject.activeInHierarchy) continue;

        float sqr = (e.transform.position - center).sqrMagnitude;
        if (sqr > maxSqr) continue; // out of range
        candidates.Add(e);
      }

      candidates.Sort((a, b) =>
      {
        if (a == null) return 1;
        if (b == null) return -1;
        return (a.transform.position - center).sqrMagnitude.CompareTo((b.transform.position - center).sqrMagnitude);
      });
      return candidates;
    }
    #endregion
  }
}
