using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyChasing : MonoBehaviour
{
  [SerializeField] private float chaseSpeed = 0.5f;
  [SerializeField] private int damage = 1;
  [SerializeField] private DropItemType dropItemType;

  private Transform player;
  private Transform spriteTransform;

  public int Damage => damage;
  public DropItemType DropItemType => dropItemType;

  void Start()
  {
    player = GameObject.FindGameObjectWithTag("Player").transform;
    spriteTransform = transform.Find("Sprite").transform;
  }

  private void FixedUpdate()
  {
    Vector3 direction = (player.position - transform.position).normalized;
    transform.position += direction * chaseSpeed * Time.fixedDeltaTime;

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
