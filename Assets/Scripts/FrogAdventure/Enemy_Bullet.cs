using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Bullet : MonoBehaviour
{
  private Rigidbody2D _rb2d;

  private float _timer;
  private readonly float _removeTime = 8f;
  private readonly float _speed = 100f;

  private void Start()
  {
    _rb2d = GetComponent<Rigidbody2D>();
    _rb2d.AddForce(Vector2.left * _speed, ForceMode2D.Force);
  }

  private void Update()
  {
    _timer += Time.deltaTime;
    if (_timer >= _removeTime)
    {
      Destroy(gameObject);
    }
  }

  private void OnTriggerEnter2D(Collider2D other)
  {
    if (other.gameObject.CompareTag("Player"))
    {
      var knockBack = other.gameObject.GetComponent<PlayerKnockBack>();
      knockBack.KnockBack(transform); // 玩家 knockback
      Destroy(gameObject);
    }
  }
}