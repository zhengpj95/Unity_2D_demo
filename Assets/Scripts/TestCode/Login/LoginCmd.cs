
using UnityEngine;

public sealed class LoginCmd : BaseCommand
{
  public override void Execute(object args = null)
  {
    Debug.Log("1111111111111111 loginCmd..." + args);
  }
}