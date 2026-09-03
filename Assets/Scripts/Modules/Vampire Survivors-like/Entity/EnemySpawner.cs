using UnityEngine;

namespace VampireSurvivorsLike
{
  /// <summary>
  /// 计算出生位置并从框架对象池取出敌人，不管理生成节奏或敌人列表。
  /// </summary>
  public sealed class EnemySpawner
  {
    private readonly GameObject[] _enemyPrefabs;
    private readonly Transform _enemyContainer;

    public EnemySpawner(GameObject[] enemyPrefabs, Transform enemyContainer)
    {
      _enemyPrefabs = enemyPrefabs;
      _enemyContainer = enemyContainer;
    }

    public EnemyChasing Spawn(Transform player, float spawnRadius, EnemyDirector director)
    {
      if (player == null || director == null || _enemyPrefabs == null || _enemyPrefabs.Length == 0)
        return null;

      GameObject prefab = GetRandomPrefab();
      if (prefab == null)
        return null;

      Vector2 direction = Random.insideUnitCircle;
      if (direction.sqrMagnitude < Mathf.Epsilon)
        direction = Vector2.right;

      Vector2 spawnPosition = (Vector2)player.position + direction.normalized * spawnRadius;
      GameObject enemyObject = PoolManager.Instance.Alloc(prefab, spawnPosition, Quaternion.identity);
      if (enemyObject == null)
        return null;

      if (_enemyContainer != null)
        enemyObject.transform.SetParent(_enemyContainer, true);

      EnemyChasing enemy = enemyObject.GetComponent<EnemyChasing>();
      if (enemy == null)
      {
        Debug.LogError($"[EnemySpawner] Prefab '{prefab.name}' is missing {nameof(EnemyChasing)}.");
        PoolManager.Instance.Free(enemyObject);
        return null;
      }

      // 目标和回收入口由上层在每次取出时注入，敌人不自行查找 Player。
      enemy.Initialize(player, director);
      return enemy;
    }

    private GameObject GetRandomPrefab()
    {
      int firstIndex = Random.Range(0, _enemyPrefabs.Length);
      for (int offset = 0; offset < _enemyPrefabs.Length; offset++)
      {
        GameObject prefab = _enemyPrefabs[(firstIndex + offset) % _enemyPrefabs.Length];
        if (prefab != null)
          return prefab;
      }

      return null;
    }
  }
}
