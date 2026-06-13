/**
 * 状态机
 */
public class StateMachine
{
  private IState _currentState;

  public IState CurrentState => _currentState;

  public void ChangeState(IState newState)
  {
    if (_currentState == newState)
    {
      return;
    }

    _currentState?.OnExit();
    _currentState = newState;
    _currentState?.OnEnter();
  }

  public void OnUpdate()
  {
    _currentState?.OnUpdate();
  }
}