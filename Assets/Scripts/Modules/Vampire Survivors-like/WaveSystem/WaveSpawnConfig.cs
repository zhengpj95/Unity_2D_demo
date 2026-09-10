using System;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike
{
  /// <summary>描述一种敌人在 Wave 中的生成节奏，不保存运行时计时器。</summary>
  [Serializable]
  public sealed class WaveSpawnEntry
  {
    [Tooltip("该条目生成的敌人预制体；预制体必须挂载 EnemyChasing。")]
    [SerializeField] private GameObject enemyPrefab;
    [Tooltip("该敌人每次生成之间的间隔（秒）。")]
    [SerializeField, Min(0.01f)] private float spawnInterval = 1f;
    [Tooltip("每次触发时生成的敌人数量。")]
    [SerializeField, Min(1)] private int spawnCount = 1;

    /// <summary>该条目使用的敌人预制体。</summary>
    public GameObject EnemyPrefab => enemyPrefab;
    /// <summary>相邻两次生成触发的间隔。</summary>
    public float SpawnInterval => spawnInterval;
    /// <summary>一次触发需要生成的数量。</summary>
    public int SpawnCount => spawnCount;
    /// <summary>是否具备可以交给 EnemySpawner 执行的最小配置。</summary>
    public bool IsValid => enemyPrefab != null && spawnInterval > 0f && spawnCount > 0;
  }

  /// <summary>
  /// 保存一组可复用的 Wave 生成规则。时间位置由 WaveTimelineConfig 决定，运行时状态由 EnemyDirector 保存。
  /// </summary>
  [CreateAssetMenu(fileName = "WaveSpawnConfig", menuName = "Survivor/Wave/Wave Spawn Config")]
  public sealed class WaveSpawnConfig : ScriptableObject
  {
    [Tooltip("该 Wave 中各敌人类型独立的生成条目；至少需要一条有效配置。")]
    [SerializeField] private List<WaveSpawnEntry> spawnEntries = new List<WaveSpawnEntry>();

    /// <summary>该 Wave 的静态刷怪条目。</summary>
    public IReadOnlyList<WaveSpawnEntry> SpawnEntries => spawnEntries;

    private void OnValidate()
    {
      if (spawnEntries == null || spawnEntries.Count == 0)
      {
        Debug.LogWarning($"[WaveSpawnConfig] '{name}' 至少需要一条 WaveSpawnEntry。", this);
        return;
      }

      for (int i = 0; i < spawnEntries.Count; i++)
      {
        if (spawnEntries[i] == null || !spawnEntries[i].IsValid)
          Debug.LogWarning($"[WaveSpawnConfig] '{name}' 的 WaveSpawnEntry[{i}] 配置无效。", this);
      }
    }
  }
}
