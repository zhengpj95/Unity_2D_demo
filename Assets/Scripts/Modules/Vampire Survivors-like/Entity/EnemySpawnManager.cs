using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike
{

  public class EnemySpawnManager : SingletonMono<EnemySpawnManager>
  {
    [Tooltip("可随机生成的敌人 Prefab 列表；列表为空时不会生成敌人。")]
    [SerializeField] private GameObject[] enemyPrefab;
    [Tooltip("生成尝试的时间间隔（秒）；达到最大敌人数时本次尝试会跳过。")]
    [SerializeField] private float spawnInterval = 1f;
    [Tooltip("场景中同时存活的敌人上限，不包含已回收到对象池的敌人。")]
    [SerializeField] private int maxEnemies = 20;
    [Tooltip("生成后的敌人父节点；只用于整理层级，不改变敌人的世界坐标。")]
    [SerializeField] private Transform enemyContainer;
    [Header("Infinite Map Spawn")]
    [Tooltip("生成距离和回收距离的中心。留空时优先查找 Player，未找到则使用 Main Camera。")]
    [SerializeField] private Transform spawnCenter;
    [Tooltip("敌人生成环的最小半径（世界单位）。应小于或等于最大生成距离。")]
    [SerializeField, Min(0f)] private float minSpawnDistance = 6f;
    [Tooltip("敌人生成环的最大半径（世界单位）。建议略大于最小生成距离。")]
    [SerializeField, Min(0f)] private float maxSpawnDistance = 8f;
    [Tooltip("敌人与生成中心超过此距离时回收到对象池（世界单位）。设为 0 可关闭距离回收。")]
    [SerializeField, Min(0f)] private float recycleDistance = 20f;
    [Tooltip("启动时为每种敌人预创建的对象数量。设为 0 可关闭预热。")]
    [SerializeField, Min(0)] private int preloadCountPerPrefab = 3;

    private float timer = 0f;

    private readonly List<EnemyChasing> enemies = new List<EnemyChasing>();
    public int KillEnemyCount { get; set; } = 0;

    void Start()
    {
      ResolveSpawnCenter();
      PreloadEnemies();
      SpawnEnemy();
    }

    void Update()
    {
      timer += Time.deltaTime;

      RecycleDistantEnemies();

      if (timer >= spawnInterval)
      {
        timer = 0f;
        SpawnEnemy();
      }
    }

    private void SpawnEnemy()
    {
      if (enemies.Count >= maxEnemies)
      {
        return;
      }

      if (enemyPrefab == null || enemyPrefab.Length == 0 || !ResolveSpawnCenter())
      {
        return;
      }

      int randomEnemyIndex = Random.Range(0, enemyPrefab.Length);
      GameObject prefab = enemyPrefab[randomEnemyIndex];
      if (prefab == null) return;

      float outerDistance = Mathf.Max(minSpawnDistance, maxSpawnDistance);
      float distance = Random.Range(Mathf.Min(minSpawnDistance, outerDistance), outerDistance);
      Vector2 direction = Random.insideUnitCircle;
      if (direction.sqrMagnitude < Mathf.Epsilon) direction = Vector2.right;
      Vector2 spawnPoint = (Vector2)spawnCenter.position + direction.normalized * distance;
      GameObject enemy = PoolManager.Instance.Alloc(prefab, spawnPoint, Quaternion.identity);
      if (enemy != null && enemyContainer != null)
      {
        enemy.transform.SetParent(enemyContainer, true);
      }
    }

    private bool ResolveSpawnCenter()
    {
      if (spawnCenter != null) return true;

      GameObject player = GameObject.FindGameObjectWithTag("Player");
      if (player != null)
      {
        spawnCenter = player.transform;
      }
      else if (Camera.main != null)
      {
        spawnCenter = Camera.main.transform;
      }

      return spawnCenter != null;
    }

    private void PreloadEnemies()
    {
      if (enemyPrefab == null || preloadCountPerPrefab <= 0) return;

      foreach (GameObject prefab in enemyPrefab)
      {
        if (prefab != null) PoolManager.Instance.Preload(prefab, preloadCountPerPrefab);
      }
    }

    private void RecycleDistantEnemies()
    {
      if (!ResolveSpawnCenter() || recycleDistance <= 0f) return;

      float recycleSqrDistance = recycleDistance * recycleDistance;
      for (int i = enemies.Count - 1; i >= 0; i--)
      {
        EnemyChasing enemy = enemies[i];
        if (enemy == null)
        {
          enemies.RemoveAt(i);
          continue;
        }

        if ((enemy.transform.position - spawnCenter.position).sqrMagnitude > recycleSqrDistance)
        {
          RecycleEnemy(enemy.gameObject);
        }
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
