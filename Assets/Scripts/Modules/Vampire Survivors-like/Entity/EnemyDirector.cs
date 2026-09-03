using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace VampireSurvivorsLike
{

  public class EnemyDirector : SingletonMono<EnemyDirector>
  {
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
    [SerializeField, Min(0f)] private float spawnRadius = 15f;
    [Tooltip("敌人与 Player 超过此距离时回收到对象池（世界单位）。应显著大于生成半径。")]
    [FormerlySerializedAs("recycleDistance")]
    [SerializeField, Min(0f)] private float despawnRadius = 25f;
    [Tooltip("启动时为每种敌人预创建的对象数量。设为 0 可关闭预热。")]
    [SerializeField, Min(0)] private int preloadCountPerPrefab = 3;

    private float timer;
    private EnemySpawner _spawner;
    private bool _playerResolutionAttempted;

    private readonly List<EnemyChasing> enemies = new List<EnemyChasing>();
    public int KillEnemyCount { get; set; } = 0;
    public float DespawnSqrDistance => despawnRadius * despawnRadius;

    protected override void Awake()
    {
      base.Awake();
      ResolvePlayer();
      _spawner = new EnemySpawner(enemyPrefab, enemyContainer);
    }

    private void Start()
    {
      PreloadEnemies();
      SpawnEnemies();
    }

    private void Update()
    {
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

    public void RecycleEnemy(GameObject enemy)
    {
      if (enemy != null && enemy.activeSelf)
      {
        PoolManager.Instance.Free(enemy);
      }
    }

    public void SpeedUpSpawnRate()
    {
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
