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
}