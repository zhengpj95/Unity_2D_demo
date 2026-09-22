using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Msg;
using TMPro;

/// <summary>承载 Launcher 登录界面交互，以及网络、提示、TMP 效果和异高虚拟列表的测试入口。</summary>
/// <remarks>
/// 异高列表示例的阅读顺序：
/// OnEnable → InitializeVariableHeightVirtualListTest（造数据和注册回调）→ RefreshData（交给列表）。
/// 列表按需调用 SelectVariableHeightListItemTemplate → OnRenderVariableHeightListItem → MeasureVariableHeightListItem。
/// 点击时 OnClickVariableHeightListItem 修改数据的 Expanded，列表随后自动重渲染并重排。
/// 每四项使用三个默认模板 render1、一个备用模板 render2；模板选择与高度计算是两个独立步骤。
/// 这里只验证代码控制高度和 LayoutElement 首选高度，不根据文本长度自动决定整项高度。
/// </remarks>
public class LoadingBehaviour : MonoBehaviour
{
  /// <summary>登录展示进度条；优先使用 Inspector 引用，缺失时 Awake 在子节点中查找。</summary>
  public UIProgressBar progressBar;
  /// <summary>登录按钮的缩放与点击组件；通过 Clicked 事件提交登录请求。</summary>
  public UIButton btnLogin;
  /// <summary>用于延迟修改描边颜色的 TMP 测试文本，与列表 Item 内的 TMP 无关。</summary>
  public TMP_Text tMP_Text;
  /// <summary>Launcher 场景中的异高虚拟列表；用于验证不同 Item 高度、选中状态和点击回调。</summary>
  [Tooltip("测试：3 个 render1 直接修改根节点高度，1 个 render2 使用 LayoutElement；点击任意项切换展开/收起。")]
  public VariableHeightVirtualList virtualList;
  // 延迟修改描边的任务；重复测试或界面关闭时停止。
  private Coroutine _outlineTestCoroutine;
  // 登录进度动画任务，结束后尝试打开 Home。
  private Coroutine _loadingCoroutine;
  // 防止登录流程尚未完成时被重复点击启动。
  private bool _isEnteringGame;
  // 展开状态的持有者是这批数据，而不是滚动时会被复用的 Item；每次启用测试界面重新生成。
  private readonly List<VariableHeightListTestData> _variableHeightListTestDatas = new();
  // Inspector 第一个非空备用模板；只用于选择来源，不在此修改模板本身。
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

  /// <summary>按钮测试入口：发送打开通用提示窗的业务事件，由 Misc 模块处理显示。</summary>
  public void OnOpenAlert()
  {
    EventBus.Emit(EventDefine.MISC_OPEN_ALERT, new AlertTipsPanelArgs("警告标题", "警告信息！不允许随便修改！", null));
  }

  /// <summary>按钮测试入口：异步发送固定账号 1001 的登录消息；非 Sent 结果会输出日志。</summary>
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

  /// <summary>按钮测试入口：连接本机 3000 端口的 WebSocket 服务，供协议收发测试使用。</summary>
  public async void OnConnectProtobuf()
  {
    await NetworkMgr.Instance.Connect("ws://localhost:3000");
  }

  /// <summary>按钮测试入口：派发测试登录命令事件，验证事件到业务 Command 的调用。</summary>
  public void OnClickBtnCmd()
  {
    EventBus.Emit(EventDefine.TEST_LOGIN_COMMAND, "你好");
  }

  /// <summary>
  /// 初始化异高虚拟列表测试数据。
  /// 混合直接设置 RectTransform 和 LayoutElement 两种高度来源，点击后切换展开状态。
  /// </summary>
  private void InitializeVariableHeightVirtualListTest()
  {
    if (virtualList == null)
      return;

    // 高度按 100/132/164/196 循环，展开时再增加 120；数据数量超过一屏以便验证复用。
    _variableHeightListTestDatas.Clear();
    for (int i = 0; i < 40; i++)
    {
      float height = 100f + i % 4 * 32f;
      _variableHeightListTestDatas.Add(new VariableHeightListTestData(
        i,
        $"测试项 {i + 1}：{(i % 4 == 3 ? "LayoutElement" : "代码设置高度")}\n点击展开/收起",
        height));
    }

    // LoadingBehaviour 是该测试列表回调的唯一持有者；重复启用时整体替换旧回调。
    virtualList.SetHandlers(OnRenderVariableHeightListItem, OnClickVariableHeightListItem);
    // render 先写显示状态，计算器再决定读取实际尺寸还是布局首选值；两者配套使用。
    virtualList.SetItemHeightResolver(MeasureVariableHeightListItem);
    _variableHeightAlternateTemplate = GetVariableHeightAlternateTemplate();
    virtualList.SetItemTemplateSelector(SelectVariableHeightListItemTemplate);
    virtualList.SetSelectionKeySelector(data => ((VariableHeightListTestData)data).Id);
    // 最后提交数据，保证第一次渲染时所有回调已准备好；列表未 Start 时会延后创建实例。
    virtualList.RefreshData(_variableHeightListTestDatas);
  }

  /// <summary>
  /// 获取 Inspector 中配置的第一个有效备用模板。
  /// Launcher 的测试配置中该模板为 render2；未配置时列表会自动回退到默认 render1。
  /// </summary>
  /// <returns>第一个有效备用模板；全部未配置时为 null。</returns>
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
  /// 根据数据完整恢复复用实例的高度与外观；前三项直接改根节点，第 4 项仅设置布局首选高度。
  /// 本例由代码决定高度，关闭实例根节点的垂直 Fitter，避免同一尺寸同时由两个控制者写入。
  /// </summary>
  /// <param name="info">虚拟列表提供的当前数据、索引、选中状态与 Item 实例。</param>
  private void OnRenderVariableHeightListItem(VirtualListRenderInfo info)
  {
    if (info.itemTransform == null || info.data is not VariableHeightListTestData data)
      return;

    ContentSizeFitter fitter = info.itemTransform.GetComponent<ContentSizeFitter>();
    if (fitter != null)
      fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

    if (data.Id % 4 == 3)
    {
      // LayoutElement 只声明期望高度，不负责调整根节点；后续计算器读取它，再由列表应用尺寸。
      // 组件添加在运行时克隆上，同一实例再次使用时直接复用已有组件。
      LayoutElement layoutElement = info.itemTransform.GetComponent<LayoutElement>();
      if (layoutElement == null)
        layoutElement = info.itemTransform.gameObject.AddComponent<LayoutElement>();
      layoutElement.preferredHeight = data.Height;
    }
    else
    {
      // 即使根节点有 Image、LayoutElement 或子 TMP，也以这里设置的完整 Item 高度为准。
      info.itemTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, data.Height);
    }

    // 文本只是内容示意，不参与本例的高度计算；真实业务可在此刷新图片、按钮或展开区域。
    TMP_Text itemText = info.itemTransform.GetComponentInChildren<TMP_Text>(true);
    if (itemText != null)
      itemText.text = data.Content;

    // 复用时同时写选中/未选中两种状态，防止上一条数据的高亮残留。
    Image background = info.itemTransform.GetComponent<Image>();
    if (background != null)
      background.color = info.selectedIndex == info.index
        ? new Color(0.3f, 0.65f, 1f, 1f)
        : Color.white;
  }

  /// <summary>混合模板的高度计算示例：LayoutElement 项读首选高度，其余项读 render 设置后的实际高度。</summary>
  /// <param name="info">已完成内容绑定和布局更新的运行时 Item。</param>
  /// <returns>完整 Item 高度，不是某个子控件的高度。</returns>
  private float MeasureVariableHeightListItem(VirtualListRenderInfo info)
  {
    return info.data is VariableHeightListTestData data && data.Id % 4 == 3
      ? LayoutUtility.GetPreferredHeight(info.itemTransform)
      : info.itemTransform.rect.height;
  }

  /// <summary>
  /// 切换数据的展开状态；列表在点击回调结束后自动重渲染，验证增高、缩短与后续项重排。
  /// </summary>
  /// <param name="info">虚拟列表提供的点击数据；点击回调中的 Item 实例固定为 null。</param>
  private void OnClickVariableHeightListItem(VirtualListRenderInfo info)
  {
    if (info.data is VariableHeightListTestData data)
    {
      // 修改数据即可，不依赖点击时的 Item 引用；滚出视口再滚回来仍可恢复展开状态。
      data.Expanded = !data.Expanded;
      Debug.Log($"[LoadingBehaviour] 点击异高列表：索引={info.index}，Id={data.Id}，高度={data.Height:0}", this);
    }
  }

  /// <summary>
  /// 两秒后通过 TMPOutline 将指定 TMP_Text 的描边改为测试颜色 #806f03。
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

  /// <summary>等待两秒后检查文本是否仍存在，再通过 TMPOutline 设置颜色并清空协程句柄。</summary>
  /// <returns>使用缩放时间等待的测试协程。</returns>
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

  /// <summary>解除按钮与列表回调，取消登录/描边任务；下次启用时重新初始化测试数据。</summary>
  private void OnDisable()
  {
    if (btnLogin != null)
      btnLogin.Clicked -= OnLogin;
    // 列表可能被独立缓存，界面停用时先清除对当前 LoadingBehaviour 的回调引用。
    if (virtualList != null)
    {
      virtualList.ClearHandlers();
      virtualList.SetItemTemplateSelector(null);
      virtualList.SetSelectionKeySelector(null);
    }
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
  /// 异高虚拟列表测试数据；展开状态留在数据中，不依赖可被回收的 Item 实例。
  /// </summary>
  private sealed class VariableHeightListTestData
  {
    /// <summary>稳定标识，同时决定使用哪一种模板与高度来源。</summary>
    public int Id { get; }
    /// <summary>显示文本。</summary>
    public string Content { get; }
    /// <summary>是否展开；点击时切换，滚动离屏后保留。</summary>
    public bool Expanded { get; set; }
    /// <summary>根据展开状态计算当前绝对高度。</summary>
    public float Height => _collapsedHeight + (Expanded ? 120f : 0f);
    // 构造时记录的收起高度；展开/收起都由此计算，避免反复点击造成高度累积误差。
    private readonly float _collapsedHeight;

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
      _collapsedHeight = height;
    }
  }
}
