using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Msg;
using TMPro;

public class TestCodeMonoBehavior : MonoBehaviour
{
  public TMP_Text tMP_Text;
  private Coroutine _outlineTestCoroutine;
  private bool _isEnteringGame;

  private const string SurvivorsSceneName = "SurvivorsDemo";

  public void OnLogin()
  {
    if (_isEnteringGame) return;
    _isEnteringGame = true;
    StartCoroutine(LoadSurvivorsScene());
  }

  private IEnumerator LoadSurvivorsScene()
  {
    AsyncOperation loadOperation = SceneManager.LoadSceneAsync(SurvivorsSceneName);
    if (loadOperation == null)
    {
      _isEnteringGame = false;
      Debug.LogError($"[Login] Failed to start loading scene: {SurvivorsSceneName}", this);
      yield break;
    }

    yield return loadOperation;

    // 等待新场景完成首帧初始化，避免隐藏登录页时短暂显示相机清屏色。
    yield return null;
    gameObject.SetActive(false);
    SurvivorModule survivorModule = ModuleManager.Instance.GetModule<SurvivorModule>(ModuleName.Survivor);
    survivorModule?.OpenSurvivorMain();
  }

  public void OnOpenAlert()
  {
    EventBus.Dispatch(UIEventDefine.MISC_OPEN_ALERT, new AlertTipsPanelArgs("警告标题", "警告信息！不允许随便修改！", null));
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
