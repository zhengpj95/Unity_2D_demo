using UnityEngine;
using Msg;
using Google.Protobuf;

public class TestCodeMonoBehavior : MonoBehaviour
{
  private bool _testModuleRegistered;

  private void RegisterTestModule()
  {
    if (_testModuleRegistered)
    {
      return;
    }

    NetworkMgr.Instance.RegisterModule(new TestMessageModule());
    _testModuleRegistered = true;
  }

  public async void OnClickConfirm()
  {
    // UIManager.Instance.OpenWindow<ConfirmPanel2Presenter>("Prefabs/ConfirmPanel2", UILayerIndex.Model, new { title = "提示", desc = "Are you sure? \nAre you close?" });
    // UIManager.Instance.OpenWindow<AlertTipsPanelPresenter>("Prefabs/AlertTipsPanel", UILayerIndex.Model, new AlertTipsPanelArgs("警告标题", "警告信息！不允许随便修改！"));

    c2s_user_login message = new c2s_user_login
    {
      AccountId = 1001,
    };
    await NetworkMgr.Instance.Send<c2s_user_login>(MessageId.C2S_USER_LOGIN, message);
  }

  // 测试 Protobuf 序列化和反序列化
  public async void OnTestProtobuf()
  {
    RegisterTestModule();
    await NetworkMgr.Instance.Connect("ws://localhost:3000");
  }
}

public class TestMessageModule : BaseMessageModule
{
  public override string ModuleName => "Test";

  public override void Register(NetworkMgr network)
  {
    base.Register(network);
    network.RegisterHandler<s2c_user_login>(ModuleName, MessageId.S2C_USER_LOGIN, OnS2CUserLogin);
    Debug.Log("1111111111111111111111111");
  }

  private void OnS2CUserLogin(s2c_user_login data)
  {
    Debug.Log($"[Test] 收到 s2c_user_login => accountId={data.AccountId}, userName={data.UserName}");
  }
}