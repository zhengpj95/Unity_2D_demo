using System.Collections;
using UnityEngine;

/// <summary>承载 Launcher 登录界面交互。</summary>
public class LoadingBehaviour : MonoBehaviour
{
  /// <summary>登录展示进度条；优先使用 Inspector 引用，缺失时 Awake 在子节点中查找。</summary>
  public UIProgressBar progressBar;
  /// <summary>登录按钮的缩放与点击组件；通过 Clicked 事件提交登录请求。</summary>
  public UIButton btnLogin;
  // 登录进度动画任务，结束后尝试打开 Home。
  private Coroutine _loadingCoroutine;
  // 防止登录流程尚未完成时被重复点击启动。
  private bool _isEnteringGame;
  private int _homeOpenGeneration;

  /// <summary>初始化进度条引用，优先保留 Inspector 绑定，兼容尚未激活的子节点。</summary>
  private void Awake()
  {
    if (progressBar == null)
    {
      // GetComponentInChildren<UIProgressBar>(true); // 递归所有层级的子节点，找到第一个匹配类型就返回
      progressBar = transform.Find("ProgressBarMask")?.GetComponent<UIProgressBar>();
    }
    if (btnLogin == null)
    {
      // 只查指定名字的直接子节点，路径写错或层级变深就返回 null
      btnLogin = transform.Find("btnLogin")?.GetComponent<UIButton>();
    }
  }

  /// <summary>每次启用恢复登录状态，并注册本组件拥有的按钮回调。</summary>
  private void OnEnable()
  {
    _isEnteringGame = false;
    _loadingCoroutine = null;
    SetLoadingVisible(false);
    if (btnLogin != null)
    {
      btnLogin.Clicked += OnLogin;
    }
  }

  /// <summary>按钮和进度条互斥显示；失败重试时回到登录按钮。</summary>
  /// <param name="isLoading">true 显示进度条，false 显示登录按钮。</param>
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
  private async void OpenSurvivorHome()
  {
    SurvivorModule survivorModule = ModuleManager.Instance.GetModule<SurvivorModule>(ModuleName.Survivor);
    if (survivorModule == null)
    {
      _isEnteringGame = false;
      Debug.LogError("[Login] SurvivorModule 尚未初始化，无法打开 SurvivorHome。", this);
      SetLoadingVisible(false);
      return;
    }

    int generation = ++_homeOpenGeneration;
    try
    {
      SurvivorHomePresenter homePresenter = await survivorModule.OpenSurvivorHome();
      if (this == null || generation != _homeOpenGeneration || !isActiveAndEnabled)
      {
        survivorModule.CancelPendingOpen(SurvivorViewType.Home);
        return;
      }

      if (homePresenter == null)
      {
        _isEnteringGame = false;
        Debug.LogError("[Login] SurvivorHome 打开失败，请检查资源键 Prefabs/Survivor/View/SurvivorHome。", this);
        SetLoadingVisible(false);
        return;
      }

      // Loading 是持久化 UIRoot 下的登录内容；隐藏后返回 Launcher 时不会再次遮挡 Home。
      gameObject.SetActive(false);
    }
    catch (System.Exception exception)
    {
      if (this == null || generation != _homeOpenGeneration) return;

      _isEnteringGame = false;
      Debug.LogError($"[Login] 异步打开 SurvivorHome 失败：{exception}", this);
      SetLoadingVisible(false);
    }
  }

  /// <summary>解除按钮回调，取消登录任务；下次启用时重新注册。</summary>
  private void OnDisable()
  {
    if (btnLogin != null)
      btnLogin.Clicked -= OnLogin;
    _homeOpenGeneration++;
    // 登录节点关闭或销毁时取消流程，避免延迟回调重新打开 Home；再次启用后允许重试。
    if (_loadingCoroutine != null)
    {
      StopCoroutine(_loadingCoroutine);
      _loadingCoroutine = null;
    }
    if (ModuleManager.IsCreated)
    {
      SurvivorModule module = ModuleManager.Instance.GetModule<SurvivorModule>(ModuleName.Survivor);
      module?.CancelPendingOpen(SurvivorViewType.Home);
    }
    _isEnteringGame = false;
  }
}
