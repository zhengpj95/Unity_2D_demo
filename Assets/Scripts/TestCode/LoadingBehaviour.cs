using System.Collections;
using UnityEngine;
using Msg;
using TMPro;

public class TestCodeMonoBehavior : MonoBehaviour
{
  public TMP_Text tMP_Text;
  private Coroutine _outlineTestCoroutine;

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
    EventBus.Dispatch("login_cmd", "你好");
  }

  /// <summary>
  /// 2 秒后将指定 TMP_Text 的描边修改为绿色。
  /// </summary>
  public void OnTestTMPOutline()
  {
    if (tMP_Text == null)
    {
      Debug.LogWarning("[TestCodeMonoBehavior] 未指定用于测试的 TMP_Text。", this);
      return;
    }

    if (_outlineTestCoroutine != null)
      StopCoroutine(_outlineTestCoroutine);

    _outlineTestCoroutine = StartCoroutine(ChangeOutlineColorAfterDelay());
  }

  private IEnumerator ChangeOutlineColorAfterDelay()
  {
    yield return new WaitForSeconds(2f);

    if (tMP_Text == null)
    {
      _outlineTestCoroutine = null;
      yield break;
    }

    TMPOutline outline = tMP_Text.GetComponent<TMPOutline>();
    if (outline == null)
    {
      Debug.LogWarning("[TestCodeMonoBehavior] 指定的 TMP_Text 没有挂载 TMPOutline。", tMP_Text);
    }
    else
    {
      if (ColorUtility.TryParseHtmlString("#806f03", out Color outlineColor))
        outline.SetOutlineColor(outlineColor);
      else
        Debug.LogWarning("[TestCodeMonoBehavior] 无法解析描边颜色 #FFEB67。", this);
    }

    _outlineTestCoroutine = null;
  }

  private void OnDisable()
  {
    if (_outlineTestCoroutine == null)
      return;

    StopCoroutine(_outlineTestCoroutine);
    _outlineTestCoroutine = null;
  }
}
