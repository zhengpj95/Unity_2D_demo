using System;
using UnityEngine;

namespace Melee
{
  public class PlayerCharacter : Character
  {
    private Rigidbody2D rb;

    public override void Start()
    {
      base.Start();
      rb = GetComponent<Rigidbody2D>();
    }

    public override void Update()
    {
      base.Update();
      float moveX = Input.GetAxisRaw("Horizontal");
      rb.velocity = new Vector2(moveX * 5f, rb.velocity.y);
      float speed = rb.velocity.magnitude;
      if (speed > 0.1f)
      {
        FSM.ChangeState(RunState);
        if (moveX != 0)
        {
          transform.localScale = new Vector3(Mathf.Sign(moveX), 1, 1);
        }
      }

      if (Input.GetButtonDown("Fire1"))
      {
        FSM.ChangeState(AttackState);
      }
    }
  }
}