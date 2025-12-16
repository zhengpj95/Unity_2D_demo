using System.Collections;
using System.Collections.Generic;
using System.Security.AccessControl;
using UnityEngine;

public class Hero : MonoBehaviour
{
	public float runSpeed = 2f;
	private Rigidbody2D _rb;
	private Animator _animator;

	void Start()
	{
		_rb = GetComponent<Rigidbody2D>();
		_animator = GetComponent<Animator>();
	}

	void Update()
	{
		var moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
		if (moveInput != Vector2.zero)
		{
			_animator.SetFloat("speed", 1);
			_animator.SetFloat("xVelocity", moveInput.x);
			_animator.SetFloat("yVelocity", moveInput.y);
		}
		else
		{
			_animator.SetFloat("speed", 0);
		}
		// rb.MovePosition(rb.position + moveInput.normalized * runSpeed * Time.deltaTime);
		_rb.velocity = moveInput.normalized * runSpeed;
	}
}
