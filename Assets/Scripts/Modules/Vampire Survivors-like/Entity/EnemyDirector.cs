using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace VampireSurvivorsLike
{

  public class EnemyDirector : SingletonMono<EnemyDirector>
  {
    // EnemyDirector 持有当前场景的 Player、敌人容器和 Wave 运行时计时，重开时必须随场景重建。
    protected override bool PersistAcrossScenes => false;

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
    [Tooltip("本局使用的 Wave 时间轴；为空时不会生成敌人。")]
    [SerializeField] private WaveTimelineConfig waveTimeline;

    private float gameTime;
    private EnemySpawner _spawner;
    private bool _playerResolutionAttempted;
    private int _currentWaveIndex = -1;
    private readonly List<WaveScheduleRuntime> _waveSchedule = new List<WaveScheduleRuntime>();
    private readonly List<WaveSpawnRuntime> _activeSpawnEntries = new List<WaveSpawnRuntime>();

    private readonly List<EnemyChasing> enemies = new List<EnemyChasing>();
    public int KillEnemyCount { get; set; } = 0;
    public float DespawnSqrDistance => despawnRadius * despawnRadius;
    /// <summary>当前战斗已累计的游戏时间；受 Time.timeScale 影响。</summary>
    public float GameTime => gameTime;
    /// <summary>当前 Wave 的从零开始索引；没有生效 Wave 时返回 -1。</summary>
    public int CurrentWaveIndex => _currentWaveIndex;
    /// <summary>当前 Wave 的显示编号；没有生效 Wave 时返回 0。</summary>
    public int CurrentWaveNumber => _currentWaveIndex < 0 ? 0 : _currentWaveIndex + 1;

    /// <summary>记录一个 WaveSpawnEntry 在当前 Wave 中的运行时计时器，不修改配置资源。</summary>
    private sealed class WaveSpawnRuntime
    {
      public readonly WaveSpawnEntry Config;
      public float Timer;

      public WaveSpawnRuntime(WaveSpawnEntry config)
      {
        Config = config;
        // 首次进入 Wave 时允许下一帧立即生成，保持旧版启动即刷怪的体验。
        Timer = config.SpawnInterval;
      }
    }

    /// <summary>保存由时间轴顺序计算出的 Wave 起止时间，不修改配置资源。</summary>
    private sealed class WaveScheduleRuntime
    {
      public readonly WaveSpawnConfig Config;
      public readonly float StartTime;
      public readonly float EndTime;

      public WaveScheduleRuntime(WaveSpawnConfig config, float startTime, float endTime)
      {
        Config = config;
        StartTime = startTime;
        EndTime = endTime;
      }

      public bool Contains(float currentTime)
      {
        return StartTime <= currentTime && currentTime < EndTime;
      }
    }

    protected override void Awake()
    {
      base.Awake();
      ResolvePlayer();
      _spawner = new EnemySpawner(enemyContainer);
      BuildWaveSchedule();
    }

    private void Start()
    {
      PreloadWaveEnemies();
    }

    private void Update()
    {
      UpdateWave();
    }

    /// <summary>累计游戏时间、更新时间轴，并驱动每个 WaveSpawnEntry 的独立计时器。</summary>
    private void UpdateWave()
    {
      gameTime += Time.deltaTime;
      int nextWaveIndex = FindActiveWaveIndex(gameTime);
      if (nextWaveIndex != _currentWaveIndex)
        ActivateWave(nextWaveIndex);

      if (_currentWaveIndex < 0)
        return;

      // 波次已开始但运行依赖尚未就绪时先等待，避免向生成器传入空引用。
      if (_spawner == null || player == null)
        return;

      for (int i = 0; i < _activeSpawnEntries.Count; i++)
        TickSpawnEntry(_activeSpawnEntries[i]);
    }

    /// <summary>查找满足 StartTime <= GameTime < EndTime 的 Wave。</summary>
    private int FindActiveWaveIndex(float currentTime)
    {
      for (int i = 0; i < _waveSchedule.Count; i++)
      {
        if (_waveSchedule[i].Contains(currentTime))
          return i;
      }

      return -1;
    }

    /// <summary>切换 Wave 时重建运行时条目，旧 Wave 的计时器不会带入下一 Wave。</summary>
    private void ActivateWave(int waveIndex)
    {
      _currentWaveIndex = waveIndex;
      _activeSpawnEntries.Clear();

      if (waveIndex < 0 || waveIndex >= _waveSchedule.Count)
        return;

      WaveSpawnConfig wave = _waveSchedule[waveIndex].Config;
      if (wave == null || wave.SpawnEntries == null)
        return;

      for (int i = 0; i < wave.SpawnEntries.Count; i++)
      {
        WaveSpawnEntry entry = wave.SpawnEntries[i];
        if (entry == null || !entry.IsValid)
        {
          Debug.LogWarning($"[EnemyDirector] Wave '{wave.name}' 包含无效 WaveSpawnEntry，已跳过。", wave);
          continue;
        }

        _activeSpawnEntries.Add(new WaveSpawnRuntime(entry));
      }
    }

    /// <summary>推进单个条目的计时器并按其配置向 EnemySpawner 请求生成。</summary>
    private void TickSpawnEntry(WaveSpawnRuntime runtime)
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

    /// <summary>按时间轴顺序累加每段持续时间，构建只读运行时调度表。</summary>
    private void BuildWaveSchedule()
    {
      _waveSchedule.Clear();
      if (waveTimeline == null || waveTimeline.WaveEntries == null)
      {
        Debug.LogWarning("[EnemyDirector] 未配置 WaveTimelineConfig，本局不会生成敌人。", this);
        return;
      }

      float startTime = 0f;
      for (int i = 0; i < waveTimeline.WaveEntries.Count; i++)
      {
        WaveTimelineEntry entry = waveTimeline.WaveEntries[i];
        if (entry == null || !entry.IsValid)
        {
          Debug.LogWarning($"[EnemyDirector] 时间轴 Wave[{i}] 配置无效，已跳过。", waveTimeline);
          continue;
        }

        float endTime = entry.IsInfinite ? float.PositiveInfinity : startTime + entry.Duration;
        _waveSchedule.Add(new WaveScheduleRuntime(entry.WaveSpawnConfig, startTime, endTime));

        if (entry.IsInfinite)
        {
          if (i != waveTimeline.WaveEntries.Count - 1)
            Debug.LogWarning("[EnemyDirector] 无限 Wave 后的时间段不会执行。", waveTimeline);
          break;
        }

        startTime = endTime;
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

    /// <summary>预热所有 Wave 引用的敌人预制体；相同预制体只预热一次。</summary>
    private void PreloadWaveEnemies()
    {
      if (preloadCountPerPrefab <= 0)
        return;

      var prefabs = new HashSet<GameObject>();
      for (int i = 0; i < _waveSchedule.Count; i++)
      {
        WaveSpawnConfig wave = _waveSchedule[i].Config;
        if (wave == null || wave.SpawnEntries == null)
          continue;

        for (int j = 0; j < wave.SpawnEntries.Count; j++)
        {
          WaveSpawnEntry entry = wave.SpawnEntries[j];
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

    /// <summary>
    /// 回收本局所有仍在场的敌人。
    /// 重开前使用对象池回收而非销毁，避免旧回合实体在场景卸载前继续参与碰撞或被武器选中。
    /// </summary>
    public void ClearActiveEnemies()
    {
      for (int i = enemies.Count - 1; i >= 0; i--)
      {
        EnemyChasing enemy = enemies[i];
        if (enemy == null)
        {
          enemies.RemoveAt(i);
          continue;
        }

        RecycleEnemy(enemy.gameObject);
      }
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

    /// <summary>在攻击范围内查找距离中心点最近的有效敌人，不创建临时集合。</summary>
    public EnemyChasing GetCloseest(Vector3 center, float maxRange = Mathf.Infinity)
    {
      if (enemies.Count == 0)
        return null;

      EnemyChasing enemy = null;
      float bestSqrDist = float.MaxValue;
      float maxSqr = float.IsInfinity(maxRange) ? float.MaxValue : maxRange * maxRange;
      for (int i = 0; i < enemies.Count; i++)
      {
        EnemyChasing candidate = enemies[i];
        if (candidate == null || !candidate.gameObject.activeInHierarchy)
          continue;

        float sqrDist = (candidate.transform.position - center).sqrMagnitude;
        if (sqrDist < bestSqrDist && sqrDist <= maxSqr)
        {
          bestSqrDist = sqrDist;
          enemy = candidate;
        }
      }
      return enemy;
    }

    /// <summary>
    /// 在攻击范围内查找距离中心点最近、且未被指定目标集合占用的有效敌人。
    /// 用于直线投射物优先分散目标，调用方应复用 excludedTargets，避免攻击循环创建临时集合。
    /// </summary>
    /// <param name="center">用于计算距离的中心点。</param>
    /// <param name="maxRange">允许选中的最大距离。</param>
    /// <param name="excludedTargets">本次选敌需要跳过的敌人 Transform；传 null 时等价于普通最近目标查询。</param>
    public EnemyChasing GetClosestExcluding(Vector3 center, float maxRange, ISet<Transform> excludedTargets)
    {
      if (excludedTargets == null || excludedTargets.Count == 0)
        return GetCloseest(center, maxRange);

      EnemyChasing enemy = null;
      float bestSqrDist = float.MaxValue;
      float maxSqr = float.IsInfinity(maxRange) ? float.MaxValue : maxRange * maxRange;
      for (int i = 0; i < enemies.Count; i++)
      {
        EnemyChasing candidate = enemies[i];
        if (candidate == null || !candidate.gameObject.activeInHierarchy || excludedTargets.Contains(candidate.transform))
          continue;

        float sqrDist = (candidate.transform.position - center).sqrMagnitude;
        if (sqrDist < bestSqrDist && sqrDist <= maxSqr)
        {
          bestSqrDist = sqrDist;
          enemy = candidate;
        }
      }

      return enemy;
    }

    /// <summary>在攻击范围内等概率选择一名有效敌人，不创建候选列表或执行排序。</summary>
    public EnemyChasing GetRandom(Vector3 center, float maxRange = Mathf.Infinity)
    {
      if (enemies.Count == 0)
        return null;

      float maxSqr = float.IsInfinity(maxRange) ? float.MaxValue : maxRange * maxRange;
      EnemyChasing selected = null;
      int eligibleCount = 0;
      for (int i = 0; i < enemies.Count; i++)
      {
        EnemyChasing candidate = enemies[i];
        if (candidate == null || !candidate.gameObject.activeInHierarchy)
          continue;

        float sqrDist = (candidate.transform.position - center).sqrMagnitude;
        if (sqrDist > maxSqr)
          continue;

        eligibleCount++;
        // 蓄水池抽样：第 n 个候选以 1/n 概率替换，最终所有候选等概率。
        if (Random.Range(0, eligibleCount) == 0)
          selected = candidate;
      }
      return selected;
    }

    /// <summary>
    /// 在攻击范围内等概率选择一名未被排除的有效敌人。
    /// 范围武器的 count 大于 1 时复用调用方提供的集合，确保同一轮多目标施放优先分散到不同敌人而不创建候选列表。
    /// </summary>
    public EnemyChasing GetRandomExcluding(Vector3 center, float maxRange, ISet<Transform> excludedTargets)
    {
      if (excludedTargets == null || excludedTargets.Count == 0)
        return GetRandom(center, maxRange);

      float maxSqr = float.IsInfinity(maxRange) ? float.MaxValue : maxRange * maxRange;
      EnemyChasing selected = null;
      int eligibleCount = 0;
      for (int i = 0; i < enemies.Count; i++)
      {
        EnemyChasing candidate = enemies[i];
        if (candidate == null || !candidate.gameObject.activeInHierarchy || excludedTargets.Contains(candidate.transform))
          continue;

        float sqrDist = (candidate.transform.position - center).sqrMagnitude;
        if (sqrDist > maxSqr)
          continue;

        eligibleCount++;
        if (Random.Range(0, eligibleCount) == 0)
          selected = candidate;
      }

      return selected;
    }
    #endregion
  }
}
