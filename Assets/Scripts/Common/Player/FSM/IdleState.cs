public class IdleState : BaseState
{
  public IdleState(Character owner) : base(owner)
  {
    //
  }

  public override void OnEnter()
  {
    owner.Animation.Play(EntityState.Idle);
  }
}