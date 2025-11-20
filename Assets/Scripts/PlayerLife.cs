using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerLife : MonoBehaviour
{
  private Animator _animator;
  private Rigidbody2D _rb;

  private void Start()
  {
    _animator = GetComponent<Animator>();
    _rb = GetComponent<Rigidbody2D>();
  }

  private void OnCollisionEnter2D(Collision2D other)
  {
    if (other.gameObject.CompareTag("Traps"))
    {
      Death();
    }
  }

  private void Death()
  {
    _rb.simulated = false; // 关掉物理系统
    _animator.SetTrigger("isDeath");
  }

  // 在死亡动画播放完成后，编辑器调用
  private void Revive()
  {
    Debug.Log("Revive");
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
  }
}