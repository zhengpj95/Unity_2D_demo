using UnityEngine;
using Msg;
using Google.Protobuf;

public class TestCodeMonoBehavior : MonoBehaviour
{
  private void Awake()
  {
    ModuleManager.Instance.PushModules<LoginModule>();
    ModuleManager.Instance.InitializeAll();
  }

  public async void OnSendLogin()
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
  public async void OnConnectProtobuf()
  {
    await NetworkMgr.Instance.Connect("ws://localhost:3000");
  }

  public void OnClickBtnCmd()
  {
    EventBus.Dispatch("login_cmd");
  }
}