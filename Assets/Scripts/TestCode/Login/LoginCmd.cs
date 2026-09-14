
using UnityEngine;

public sealed class LoginCmd : BaseCommand
{
  public override void Execute(EventContext context)
  {
    Debug.Log("1111111111111111 loginCmd..." + context.Data);
  }
}
