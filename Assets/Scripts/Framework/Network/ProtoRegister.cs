using Google.Protobuf;
using Msg;

public static class ProtoRegister
{
  public static void RegisterAll()
  {
    // 重连时 RegisterAll 可能被再次调用，因此每个 cmd 必须幂等注册。
    RegisterIfMissing(MessageId.C2S_USER_REGISTER, c2s_user_register.Parser);
    RegisterIfMissing(MessageId.S2C_USER_REGISTER, s2c_user_register.Parser);
    RegisterIfMissing(MessageId.C2S_USER_LOGIN, c2s_user_login.Parser);
    RegisterIfMissing(MessageId.S2C_USER_LOGIN, s2c_user_login.Parser);
    RegisterIfMissing(MessageId.C2S_CONFIG, c2s_config.Parser);
    RegisterIfMissing(MessageId.S2C_CONFIG, s2c_config.Parser);
    RegisterIfMissing(MessageId.S2C_ERROR, s2c_error.Parser);
  }

  private static void RegisterIfMissing(uint command, MessageParser parser)
  {
    if (!ProtoMgr.Contains(command))
      ProtoMgr.Register(command, parser);
  }
}
