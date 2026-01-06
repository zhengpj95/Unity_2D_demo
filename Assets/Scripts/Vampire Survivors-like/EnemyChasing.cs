using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyChasing : MonoBehaviour
{
  [SerializeField] private float chaseSpeed = 0.5f;
  [SerializeField] private int damage = 1;
  [SerializeField] private DropItemType dropItemType;

  private Transform player;
  private Transform spriteTransform;
  private Rigidbody2D rb;

  public int Damage => damage;
  public DropItemType DropItemType => dropItemType;

  private void OnEnable()
  {
    if (EnemySpawnManager.Instance != null)
    {
      EnemySpawnManager.Instance.RegisterEnemy(this);
    }
  }

  private void OnDisable()
  {
    if (EnemySpawnManager.Instance != null)
    {
      EnemySpawnManager.Instance.UnregisterEnemy(this);
    }
  }

  void Start()
  {
    player = GameObject.FindGameObjectWithTag("Player").transform;
    spriteTransform = transform.Find("Sprite").transform;
    rb = GetComponent<Rigidbody2D>();
  }

  private void FixedUpdate()
  {
    Vector3 direction = (player.position - transform.position).normalized;
    Vector2 newPosition = transform.position + direction * chaseSpeed * Time.fixedDeltaTime;
    rb.MovePosition(newPosition);

    if (player.position.x > transform.position.x)
    {
      spriteTransform.localScale = new Vector3(-1, 1, 1);
    }
    else
    {
      spriteTransform.localScale = new Vector3(1, 1, 1);
    }
  }

  private void OnCollisionEnter2D(Collision2D collision)
  {
    if (collision.gameObject.CompareTag("Player"))
    {
      var health = collision.gameObject.GetComponent<VSPlayerHealth>();
      if (health != null)
      {
        health.TakeDamage(damage);
      }
      Destroy(gameObject); // 碰撞造成伤害
    }
  }
}
