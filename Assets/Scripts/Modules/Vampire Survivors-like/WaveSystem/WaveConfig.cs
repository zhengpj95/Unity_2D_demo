using System;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike
{
  /// <summary>
  /// 描述一个敌人类型在当前 Wave 中的生成节奏。该对象只保存静态配置，不保存运行时计时器。
  /// </summary>
  [Serializable]
  public sealed class SpawnEntry
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

    /// <summary>判断条目是否具备可以交给 EnemySpawner 执行的最小配置。</summary>
    public bool IsValid => enemyPrefab != null && spawnInterval > 0f && spawnCount > 0;
  }

  /// <summary>
  /// 描述一个游戏时间区间内的敌人生成规则。WaveConfig 是只读资源，运行时状态由 EnemyDirector 单独保存。
  /// </summary>
  [CreateAssetMenu(fileName = "WaveConfig", menuName = "Survivor/Wave/Wave Config")]
  public sealed class WaveConfig : ScriptableObject
  {
    [Tooltip("Wave 开始生效的游戏时间（秒，包含该时间点）。")]
    [SerializeField, Min(0f)] private float startTime;
    [Tooltip("Wave 结束生效的游戏时间（秒，不包含该时间点）。")]
    [SerializeField, Min(0f)] private float endTime = 60f;
    [Tooltip("该 Wave 中各敌人类型独立的生成条目。")]
    [SerializeField] private List<SpawnEntry> spawnEntries = new List<SpawnEntry>();

    /// <summary>Wave 的起始游戏时间，区间包含该时间点。</summary>
    public float StartTime => startTime;
    /// <summary>Wave 的结束游戏时间，区间不包含该时间点。</summary>
    public float EndTime => endTime;
    /// <summary>该 Wave 的静态刷怪条目。</summary>
    public IReadOnlyList<SpawnEntry> SpawnEntries => spawnEntries;

    /// <summary>判断指定游戏时间是否处于该 Wave 的生效区间。</summary>
    /// <param name="gameTime">从战斗开始累计的游戏时间。</param>
    public bool Contains(float gameTime)
    {
      return startTime <= gameTime && gameTime < endTime;
    }

    private void OnValidate()
    {
      if (endTime <= startTime)
        Debug.LogWarning($"[WaveConfig] '{name}' 的 EndTime 必须大于 StartTime。", this);
    }
  }
}
