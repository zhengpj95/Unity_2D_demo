using UnityEngine;

/// <summary>
/// 登录业务模块。
/// </summary>
public sealed class LoginModule : BaseModule
{
  public override ModuleName ModuleName => ModuleName.Login;

  protected override void OnInit()
  {
    RegisterProxy<LoginProxy>();
    Debug.Log("[LoginModule] Initialized");
  }

  protected override void OnUpdate()
  {
  }

  protected override void OnRelease()
  {
  }
}