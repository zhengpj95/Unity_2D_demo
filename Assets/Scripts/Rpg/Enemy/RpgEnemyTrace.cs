using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * RpgEnemyTrace handles the movement of an enemy towards the player character.
 * It uses Rigidbody2D to move the enemy at a specified speed in the direction of the player.
 */
public class RpgEnemyTrace : MonoBehaviour
{
  public float moveSpeed = 0.5f;

  private Rigidbody2D _rb;
  private Transform _target;

  private void Start()
  {
    _rb = GetComponent<Rigidbody2D>();
    _target = GameObject.FindGameObjectWithTag("Player").transform;
  }

  private void Update()
  {
    if (Vector2.Distance(transform.position, _target.position) < 0.5f)
    {
      _rb.velocity = Vector2.zero;
      return;
    }

    Vector2 direction = _target.position - transform.position;
    direction.Normalize();
    _rb.velocity = direction * moveSpeed;
    if (_target.position.x > transform.position.x)
    {
      transform.localScale = new Vector3(-1, 1, 1);
    }
    else
    {
      transform.localScale = new Vector3(1, 1, 1);
    }
  }

  private void OnCollisionEnter2D(Collision2D collision)
  {
    if (collision.gameObject.CompareTag("Player"))
    {
      Destroy(gameObject);
      // _rb.velocity = Vector2.zero;
    }
  }
}
