using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Msg;
using TMPro;

/// <summary>承载 Launcher 登录界面交互，以及网络、提示和 TMP 效果的测试入口。</summary>
public class LoadingBehaviour : MonoBehaviour
{
  public UIProgressBar progressBar;
  /// <summary>登录按钮的缩放与点击组件；通过 Clicked 事件提交登录请求。</summary>
  public UIButton btnLogin;
  public TMP_Text tMP_Text;
  /// <summary>Launcher 场景中的异高虚拟列表；用于验证不同 Item 高度、选中状态和点击回调。</summary>
  public VariableHeightVirtualList virtualList;
  private Coroutine _outlineTestCoroutine;
  private Coroutine _loadingCoroutine;
  private bool _isEnteringGame;
  private readonly List<VariableHeightListTestData> _variableHeightListTestDatas = new();
  private RectTransform _variableHeightAlternateTemplate;

  /// <summary>初始化进度条引用，优先保留 Inspector 绑定，兼容尚未激活的子节点。</summary>
  private void Awake()
  {
    if (progressBar == null)
      progressBar = GetComponentInChildren<UIProgressBar>(true);
    if (btnLogin == null)
      btnLogin = transform.Find("btnLogin")?.GetComponent<UIButton>();
  }

  /// <summary>每次启用恢复登录状态，并注册本组件拥有的按钮回调。</summary>
  private void OnEnable()
  {
    SetLoadingVisible(false);
    if (btnLogin != null)
      btnLogin.Clicked += OnLogin;
    else
      Debug.LogError("[LoadingBehaviour] 未绑定 btnLogin UIButton。", this);

    InitializeVariableHeightVirtualListTest();
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
      Debug.LogError("[Login] SurvivorHome 打开失败，请检查资源键 Prefabs/Survivor/View/SurvivorHome。", this);
      SetLoadingVisible(false);
      return;
    }

    // Loading 是持久化 UIRoot 下的登录内容；隐藏后返回 Launcher 时不会再次遮挡 Home。
    gameObject.SetActive(false);
  }

  public void OnOpenAlert()
  {
    EventBus.Emit(EventDefine.MISC_OPEN_ALERT, new AlertTipsPanelArgs("警告标题", "警告信息！不允许随便修改！", null));
  }

  public async void OnSendLogin()
  {
    c2s_user_login message = new c2s_user_login
    {
      AccountId = 1001,
    };
    NetworkSendResult sendResult = await NetworkMgr.Instance.Send(MessageId.C2S_USER_LOGIN, message);
    if (sendResult != NetworkSendResult.Sent)
      Debug.LogWarning($"[LoadingBehaviour] Login message was not sent. Result: {sendResult}", this);
  }

  // 测试 Protobuf 序列化和反序列化
  public async void OnConnectProtobuf()
  {
    await NetworkMgr.Instance.Connect("ws://localhost:3000");
  }

  public void OnClickBtnCmd()
  {
    EventBus.Emit(EventDefine.TEST_LOGIN_COMMAND, "你好");
  }

  /// <summary>
  /// 初始化异高虚拟列表测试数据。
  /// 每项在渲染时写入不同 preferredHeight，用于验证布局测量、对象池复用、选中与点击回调。
  /// </summary>
  private void InitializeVariableHeightVirtualListTest()
  {
    if (virtualList == null)
      return;

    _variableHeightListTestDatas.Clear();
    for (int i = 0; i < 40; i++)
    {
      float height = 56f + i % 4 * 32f;
      _variableHeightListTestDatas.Add(new VariableHeightListTestData(
        i,
        $"异高测试项 {i + 1}（高度 {height:0}）\n滚动、点击并观察对象池复用。",
        height));
    }

    // LoadingBehaviour 是该测试列表回调的唯一持有者；重复启用时整体替换旧回调。
    virtualList.SetHandlers(OnRenderVariableHeightListItem, OnClickVariableHeightListItem);
    _variableHeightAlternateTemplate = GetVariableHeightAlternateTemplate();
    virtualList.SetItemTemplateSelector(SelectVariableHeightListItemTemplate);
    virtualList.SetSelectionKeySelector(data => ((VariableHeightListTestData)data).Id);
    virtualList.RefreshData(_variableHeightListTestDatas);
  }

  /// <summary>
  /// 获取 Inspector 中配置的第一个有效备用模板。
  /// Launcher 的测试配置中该模板为 render2；未配置时列表会自动回退到默认 render1。
  /// </summary>
  private RectTransform GetVariableHeightAlternateTemplate()
  {
    foreach (RectTransform template in virtualList.ItemTemplates)
    {
      if (template != null)
        return template;
    }

    return null;
  }

  /// <summary>
  /// 按 3 个默认模板、1 个备用模板的节奏选择异高测试项模板。
  /// </summary>
  /// <param name="data">当前数据项；本测试不依赖其内容选择模板。</param>
  /// <param name="index">当前数据项索引。</param>
  /// <returns>索引为 3、7、11 等时返回 render2，其余返回 null 以使用默认 render1。</returns>
  private RectTransform SelectVariableHeightListItemTemplate(object data, int index)
  {
    return index % 4 == 3 ? _variableHeightAlternateTemplate : null;
  }

  /// <summary>
  /// 渲染异高测试项，并通过 LayoutElement 提供可测量的目标高度。
  /// LayoutElement 添加在运行时实例上，不会修改场景中的模板资源。
  /// </summary>
  /// <param name="info">虚拟列表提供的当前数据、索引、选中状态与 Item 实例。</param>
  private void OnRenderVariableHeightListItem(VirtualListRenderInfo info)
  {
    if (info.itemTransform == null || info.data is not VariableHeightListTestData data)
      return;

    LayoutElement layoutElement = info.itemTransform.GetComponent<LayoutElement>();
    if (layoutElement == null)
      layoutElement = info.itemTransform.gameObject.AddComponent<LayoutElement>();
    layoutElement.preferredHeight = data.Height;

    TMP_Text itemText = info.itemTransform.GetComponentInChildren<TMP_Text>(true);
    if (itemText != null)
      itemText.text = data.Content;

    Image background = info.itemTransform.GetComponent<Image>();
    if (background != null)
      background.color = info.selectedIndex == info.index
        ? new Color(0.3f, 0.65f, 1f, 1f)
        : Color.white;
  }

  /// <summary>
  /// 输出异高测试项点击信息，确认点击索引与数据映射正确。
  /// </summary>
  /// <param name="info">虚拟列表提供的点击数据；点击回调中的 Item 实例固定为 null。</param>
  private void OnClickVariableHeightListItem(VirtualListRenderInfo info)
  {
    if (info.data is VariableHeightListTestData data)
      Debug.Log($"[LoadingBehaviour] 点击异高列表：索引={info.index}，Id={data.Id}，高度={data.Height:0}", this);
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
      btnLogin.Clicked -= OnLogin;
    // 列表可能被独立缓存，界面停用时先清除对当前 LoadingBehaviour 的回调引用。
    if (virtualList != null)
      virtualList.ClearHandlers();
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

  /// <summary>
  /// 异高虚拟列表的测试数据；Height 会在渲染时传给 LayoutElement 参与实际高度测量。
  /// </summary>
  private sealed class VariableHeightListTestData
  {
    public int Id { get; }
    public string Content { get; }
    public float Height { get; }

    /// <summary>
    /// 创建一条异高列表测试数据。
    /// </summary>
    /// <param name="id">用于选中状态恢复的稳定标识。</param>
    /// <param name="content">Item 中显示的测试文本。</param>
    /// <param name="height">Item 期望高度，单位为 UI 像素。</param>
    public VariableHeightListTestData(int id, string content, float height)
    {
      Id = id;
      Content = content;
      Height = height;
    }
  }
}
