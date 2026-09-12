using System.Collections;
using UnityEngine;
using Msg;
using TMPro;

/// <summary>承载 Launcher 登录界面交互，以及网络、提示和 TMP 效果的测试入口。</summary>
public class LoadingBehaviour : MonoBehaviour
{
  public TMP_Text tMP_Text;
  private Coroutine _outlineTestCoroutine;
  private bool _isEnteringGame;

  /// <summary>登录成功后打开 Survivor Home；战斗场景只由 Home 的开始按钮触发加载。</summary>
  public void OnLogin()
  {
    if (_isEnteringGame) return;
    _isEnteringGame = true;

    SurvivorModule survivorModule = ModuleManager.Instance.GetModule<SurvivorModule>(ModuleName.Survivor);
    if (survivorModule == null)
    {
      _isEnteringGame = false;
      Debug.LogError("[Login] SurvivorModule 尚未初始化，无法打开 SurvivorHome。", this);
      return;
    }

    SurvivorHomePresenter homePresenter = survivorModule.OpenSurvivorHome();
    if (homePresenter == null)
    {
      _isEnteringGame = false;
      Debug.LogError("[Login] SurvivorHome 打开失败，请检查 Resources/Prefabs/SurvivorHome。", this);
      return;
    }

    // Loading 是持久化 UIRoot 下的登录内容；隐藏后返回 Launcher 时不会再次遮挡 Home。
    gameObject.SetActive(false);
  }

  public void OnOpenAlert()
  {
    EventBus.Emit(UIEventDefine.MISC_OPEN_ALERT, new AlertTipsPanelArgs("警告标题", "警告信息！不允许随便修改！", null));
  }

  public async void OnSendLogin()
  {
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
    EventBus.Emit("login_cmd", "你好");
  }

  /// <summary>
  /// 2 秒后将指定 TMP_Text 的描边修改为绿色。
  /// </summary>
  public void OnTestTMPOutline()
  {
    if (tMP_Text == null)
    {
      Debug.LogWarning("[LoadingBehaviour] 未指定用于测试的 TMP_Text。", this);
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
      Debug.LogWarning("[LoadingBehaviour] 指定的 TMP_Text 没有挂载 TMPOutline。", tMP_Text);
    }
    else
    {
      if (ColorUtility.TryParseHtmlString("#806f03", out Color outlineColor))
        outline.SetOutlineColor(outlineColor);
      else
        Debug.LogWarning("[LoadingBehaviour] 无法解析描边颜色 #FFEB67。", this);
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
