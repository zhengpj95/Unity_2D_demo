using UnityEngine;

public class RunState : BaseState
{
  public RunState(Character owner) : base(owner)
  {
    //
  }

  public override void OnEnter()
  {
    owner.Animation.Play(EntityState.Run);
  }

  public override void OnUpdate()
  {
    if (Input.GetAxisRaw("Horizontal") == 0 && Input.GetAxisRaw("Vertical") == 0)
    {
      owner.FSM.ChangeState(owner.IdleState);
    }
  }
}