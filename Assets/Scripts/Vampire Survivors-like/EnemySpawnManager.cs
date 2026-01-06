using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnManager : SingletonMono<EnemySpawnManager>
{
  [SerializeField] private GameObject[] enemyPrefab;
  [SerializeField] private Transform[] spawnPoints;
  [SerializeField] private float spawnInterval = 1f;
  [SerializeField] private int maxEnemies = 10;
  [SerializeField] private Transform enemyContainer;

  private float timer = 0f;

  private readonly List<EnemyChasing> enemies = new List<EnemyChasing>();

  void Start()
  {
    SpawnEnemy();
  }

  void Update()
  {
    timer += Time.deltaTime;

    if (timer >= spawnInterval)
    {
      timer = 0f;
      SpawnEnemy();
    }
  }

  private void SpawnEnemy()
  {
    int currentEnemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
    if (currentEnemyCount >= maxEnemies)
    {
      return;
    }

    if (enemyPrefab.Length > 0 && spawnPoints.Length > 0)
    {
      int randomIndex = Random.Range(0, enemyPrefab.Length);
      int spawnPointIndex = Random.Range(0, spawnPoints.Length);
      var spawnPoint = spawnPoints[spawnPointIndex];
      var point = PointUtil.RandomHorizontal(spawnPoint.position, 10f);
      if (spawnPointIndex % 2 != 0)
      {
        point = PointUtil.RandomVertical(spawnPoint.position, 6f);
      }
      Instantiate(enemyPrefab[randomIndex], point, Quaternion.identity, enemyContainer);
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
    enemies.Add(e);
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