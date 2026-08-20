using UnityEngine;
using Msg;

/// <summary>
/// 登录模块的数据和协议处理。
/// </summary>
public sealed class LoginProxy : BaseProxy
{
  protected override void OnInit()
  {
    NetworkMgr.Instance.RegisterHandler<s2c_user_login>(
      "Login",
      MessageId.S2C_USER_LOGIN,
      OnS2CUserLogin);
  }

  protected override void OnRelease()
  {
    NetworkMgr.Instance.UnregisterHandler(MessageId.S2C_USER_LOGIN);
  }

  private void OnS2CUserLogin(s2c_user_login data)
  {
    Debug.Log($"[Login] 收到 s2c_user_login => accountId={data.AccountId}, userName={data.UserName}");
  }
}