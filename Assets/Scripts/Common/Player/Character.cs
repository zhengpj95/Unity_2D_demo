using System;
using UnityEngine;

/**
 * 角色
 */
public class Character : MonoBehaviour
{
  private string _username;

  public StateMachine FSM { get; private set; }
  public AnimationComponent Animation { get; private set; }

  public IdleState IdleState { get; private set; }
  public RunState RunState { get; private set; }
  public AttackState AttackState { get; private set; }

  public virtual void Awake()
  {
    FSM = new StateMachine();
    Animation = new AnimationComponent(GetComponent<Animator>());
    CreateStates();
  }

  public virtual void Start()
  {
    FSM.ChangeState(IdleState);
  }

  public virtual void Update()
  {
    FSM.OnUpdate();
  }

  private void CreateStates()
  {
    IdleState = new IdleState(this);
    RunState = new RunState(this);
    AttackState = new AttackState(this);
  }
}