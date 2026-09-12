using System.Collections;
using UnityEngine;
using Msg;
using TMPro;
using UnityEngine.UI;

/// <summary>承载 Launcher 登录界面交互，以及网络、提示和 TMP 效果的测试入口。</summary>
public class LoadingBehaviour : MonoBehaviour
{
  public UIProgressBar progressBar;
  public Button btnLogin;
  public TMP_Text tMP_Text;
  private Coroutine _outlineTestCoroutine;
  private Coroutine _loadingCoroutine;
  private bool _isEnteringGame;

  /// <summary>初始化进度条引用，优先保留 Inspector 绑定，兼容尚未激活的子节点。</summary>
  private void Awake()
  {
    if (progressBar == null)
      progressBar = GetComponentInChildren<UIProgressBar>(true);
    if (btnLogin == null)
      btnLogin = transform.Find("btnLogin")?.GetComponent<Button>();
  }

  /// <summary>每次启用恢复登录状态，并注册本组件拥有的按钮回调。</summary>
  private void OnEnable()
  {
    SetLoadingVisible(false);
    if (btnLogin != null)
      btnLogin.onClick.AddListener(OnLogin);
    else
      Debug.LogError("[LoadingBehaviour] 未绑定 btnLogin Button。", this);
  }

  /// <summary>按钮和进度条互斥显示；失败重试时回到登录按钮。</summary>
  private void SetLoadingVisible(bool isLoading)
  {
    if (btnLogin != null)
      btnLogin.gameObject.SetActive(!isLoading);
    if (progressBar != null)
      progressBar.gameObject.SetActive(isLoading);
  }

  /// <summary>登录后播放一秒进度条，完成后才打开 Survivor Home；重复点击不会重启进度。</summary>
  public void OnLogin()
  {
    if (_isEnteringGame || !isActiveAndEnabled) return;
    if (progressBar == null)
    {
      Debug.LogError("[LoadingBehaviour] 未绑定 UIProgressBar，无法播放登录进度。", this);
      return;
    }

    _isEnteringGame = true;
    // 显示前重置，避免重试时短暂露出上一次的 100%。
    progressBar.SetValue(0f);
    SetLoadingVisible(true);
    _loadingCoroutine = StartCoroutine(ShowLoadingProgress());
  }

  /// <summary>使用非缩放时间播放展示进度；这段进度不代表网络或资源加载的真实完成率。</summary>
  private IEnumerator ShowLoadingProgress()
  {
    float elapsed = 0f;
    const float duration = 1f;
    progressBar.SetValue(0f);
    while (elapsed < duration)
    {
      yield return null;
      if (progressBar == null)
      {
        Debug.LogError("[LoadingBehaviour] 登录进度条已被销毁，取消进入 Home。", this);
        _loadingCoroutine = null;
        _isEnteringGame = false;
        SetLoadingVisible(false);
        yield break;
      }
      elapsed += Time.unscaledDeltaTime;
      progressBar.SetValue(Mathf.Clamp01(elapsed / duration));
    }

    // 满进度保留一个渲染帧，再隐藏登录界面，确保玩家能看到完成状态。
    yield return null;
    _loadingCoroutine = null;
    OpenSurvivorHome();
  }

  /// <summary>进度完成后打开 Home；失败时保留登录界面并允许重新点击。</summary>
  private void OpenSurvivorHome()
  {
    SurvivorModule survivorModule = ModuleManager.Instance.GetModule<SurvivorModule>(ModuleName.Survivor);
    if (survivorModule == null)
    {
      _isEnteringGame = false;
      Debug.LogError("[Login] SurvivorModule 尚未初始化，无法打开 SurvivorHome。", this);
      SetLoadingVisible(false);
      return;
    }

    SurvivorHomePresenter homePresenter = survivorModule.OpenSurvivorHome();
    if (homePresenter == null)
    {
      _isEnteringGame = false;
      Debug.LogError("[Login] SurvivorHome 打开失败，请检查 Resources/Prefabs/SurvivorHome。", this);
      SetLoadingVisible(false);
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
    if (btnLogin != null)
      btnLogin.onClick.RemoveListener(OnLogin);
    // 登录节点关闭或销毁时取消流程，避免延迟回调重新打开 Home；再次启用后允许重试。
    if (_loadingCoroutine != null)
    {
      StopCoroutine(_loadingCoroutine);
      _loadingCoroutine = null;
    }
    _isEnteringGame = false;

    if (_outlineTestCoroutine == null)
      return;

    StopCoroutine(_outlineTestCoroutine);
    _outlineTestCoroutine = null;
  }
}
