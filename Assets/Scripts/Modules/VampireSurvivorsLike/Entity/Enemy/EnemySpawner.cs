using UnityEngine;

namespace VampireSurvivorsLike
{
  /// <summary>计算出生位置并从框架对象池取出指定敌人，不管理生成节奏或敌人列表。</summary>
  public sealed class EnemySpawner
  {
    private readonly Transform _enemyContainer;

    /// <summary>创建只负责实例生成的 EnemySpawner。</summary>
    /// <param name="enemyContainer">生成后用于整理层级的父节点；可为空。</param>
    public EnemySpawner(Transform enemyContainer)
    {
      _enemyContainer = enemyContainer;
    }

    /// <summary>按敌人类型配置，在玩家周围生成一名通用 Prefab 敌人。</summary>
    /// <param name="player">生成圆心及敌人追击目标。</param>
    /// <param name="spawnRadius">敌人相对玩家的出生半径。</param>
    /// <param name="director">负责登记敌人并提供回收入口的导演器。</param>
    /// <param name="prefab">所有敌人共用的通用 Prefab。</param>
    /// <param name="config">敌人类型配置；必须提供，初始化前会注入外形和数值。</param>
    public EnemyChaser Spawn(Transform player, float spawnRadius, EnemyManager director, GameObject prefab, EnemyTypeConfig config)
    {
      if (player == null || director == null || prefab == null || config == null)
        return null;

      Vector2 direction = Random.insideUnitCircle;
      if (direction.sqrMagnitude < Mathf.Epsilon)
        direction = Vector2.right;

      Vector2 spawnPosition = (Vector2)player.position + direction.normalized * spawnRadius;
      GameObject enemyObject = PoolManager.Instance.Alloc(prefab, spawnPosition, Quaternion.identity);
      if (enemyObject == null)
        return null;

      // PoolManager 的 OnAlloc 先于配置注入执行，因此这里覆盖数值并重启默认动画。
      if (!EnemyConfigUtil.Apply(enemyObject, config))
      {
        PoolManager.Instance.Free(enemyObject);
        return null;
      }

      if (_enemyContainer != null)
        enemyObject.transform.SetParent(_enemyContainer, true);

      EnemyChaser enemy = enemyObject.GetComponent<EnemyChaser>();
      if (enemy == null)
      {
        Debug.LogError($"[EnemySpawner] Prefab '{prefab.name}' is missing {nameof(EnemyChaser)}.");
        PoolManager.Instance.Free(enemyObject);
        return null;
      }

      // 目标和回收入口由上层在每次取出时注入，敌人不自行查找 Player。
      enemy.Initialize(player, director);
      return enemy;
    }
  }
}
