using Msg;

/// <summary>
/// 管理 Misc 模块的通用协议边界，包括服务端错误协议的生命周期与业务转发。
/// </summary>
public sealed class MiscProxy : BaseProxy
{
  protected override void OnInit()
  {
    RegisterHandler<s2c_error>(MessageId.S2C_ERROR, OnServerError);
  }

  /// <summary>
  /// 将服务端通用错误转为 Misc 弹窗事件；Proxy 不直接访问 Presenter 或 View。
  /// </summary>
  private void OnServerError(s2c_error message)
  {
    string description = string.IsNullOrWhiteSpace(message.Msg)
        ? $"服务器返回错误，错误码：{message.Code}。"
        : $"{message.Msg}\n错误码：{message.Code}";

    Emit(
        EventDefine.MISC_OPEN_ALERT,
        new AlertTipsPanelArgs("服务器提示", description, null));
  }
}
