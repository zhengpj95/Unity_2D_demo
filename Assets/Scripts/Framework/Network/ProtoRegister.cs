using Msg;

public static class ProtoRegister
{
  public static void RegisterAll()
  {
    ProtoMgr.Register(Cmd.C2S_USER_REGISTER, c2s_user_register.Parser);
    ProtoMgr.Register(Cmd.S2C_USER_REGISTER, s2c_user_register.Parser);
    ProtoMgr.Register(Cmd.C2S_USER_LOGIN, c2s_user_login.Parser);
    ProtoMgr.Register(Cmd.S2C_USER_LOGIN, s2c_user_login.Parser);
  }
}