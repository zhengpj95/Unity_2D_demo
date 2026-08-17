using Msg;

public static class ProtoRegister
{
  public static void RegisterAll()
  {
    ProtoMgr.Register(MessageId.C2S_USER_REGISTER, c2s_user_register.Parser);
    ProtoMgr.Register(MessageId.S2C_USER_REGISTER, s2c_user_register.Parser);
    ProtoMgr.Register(MessageId.C2S_USER_LOGIN, c2s_user_login.Parser);
    ProtoMgr.Register(MessageId.S2C_USER_LOGIN, s2c_user_login.Parser);
    ProtoMgr.Register(MessageId.C2S_CONFIG, c2s_config.Parser);
    ProtoMgr.Register(MessageId.S2C_CONFIG, s2c_config.Parser);
    ProtoMgr.Register(MessageId.S2C_ERROR, s2c_error.Parser);
  }
}