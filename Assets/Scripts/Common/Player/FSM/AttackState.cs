public class AttackState : BaseState
{
  public AttackState(Character owner) : base(owner)
  {
    //
  }

  public override void OnEnter()
  {
    owner.Animation.Play(EntityState.Attack, 0.5f);
  }

  public override void OnUpdate()
  {
    base.OnUpdate();
    if (owner.Animation.IsCurrentAnimFinished())
    {
      owner.FSM.ChangeState(owner.IdleState);
    }
  }
}