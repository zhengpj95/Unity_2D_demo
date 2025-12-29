using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

/**
 * 敌人管理器
 */
public class EnemyManager : MonoBehaviour
{
  public static EnemyManager Instance { get; private set; }

  public Transform enemyPrefab;
  public LayerMask enemyLayer;
  public float detectionRadius = 10f;

  // spawn 配置
  public float spawnRadius = 5f;
  public int maxSpawnAttempts = 10;
  public float spawnClearRadius = 0.5f; // 碰撞检测用半径

  // 用于非分配检测的缓冲区，按需调整大小
  private Collider2D[] _detectionEnemy = new Collider2D[50];

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
      DontDestroyOnLoad(gameObject);
    }
    else if (Instance != this)
    {
      Destroy(gameObject);
    }
  }

  private void OnDestroy()
  {
    if (Instance == this)
    {
      Instance = null;
    }
  }

  private IEnumerator DetectionEnemy()
  {
    yield return new WaitForSeconds(2f);
    int count = CountEnemies();
    if (count < maxSpawnAttempts)
    {
      SpawnEnemy(count);
    }
  }

  public int CountEnemies()
  {
    int count = Physics2D.OverlapCircleNonAlloc(transform.position, detectionRadius, _detectionEnemy, enemyLayer);
    return count;
  }

  private void Update()
  {
    StartCoroutine(DetectionEnemy());
  }

  private void SpawnEnemy(int cnt)
  {
    Vector2 spawnPos = transform.position;
    bool found = false;

    for (int i = cnt; i < maxSpawnAttempts; i++)
    {
      Vector2 offset = Random.insideUnitCircle * spawnRadius;
      Vector2 candidate = (Vector2)transform.position + offset;

      // 检查该点附近是否有其他敌人或障碍（使用 spawnClearRadius）
      Collider2D hit = Physics2D.OverlapCircle(candidate, spawnClearRadius, enemyLayer);
      if (!hit)
      {
        spawnPos = candidate;
        found = true;
        break;
      }
    }

    if (found)
    {
      // 若多次尝试仍未找到空位则在最后的 candidate 位置生成（或选择不生成）
      Instantiate(enemyPrefab, (Vector3)spawnPos, Quaternion.identity);
    }
  }

  private void OnDrawGizmosSelected()
  {
    Gizmos.color = Color.cyan;
    Gizmos.DrawWireSphere(transform.position, detectionRadius);

    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, spawnRadius);
  }
}