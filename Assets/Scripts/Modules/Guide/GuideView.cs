using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>GuidePanel 预制体视图；负责定位目标、更新四片遮罩和展示手势说明。</summary>
public sealed class GuideView : UIView
{
  private static readonly string[] MaskNames = { "Top", "Right", "Bottom", "Left" };

  [SerializeField, Tooltip("目标区域外扩的遮罩镂空边距。")]
  private Vector2 highlightPadding = new Vector2(12f, 12f);
  [SerializeField, Tooltip("手势相对于目标中心的偏移。")]
  private Vector2 cursorOffset = new Vector2(48f, -36f);
  [SerializeField, Tooltip("说明气泡相对于手势的偏移。")]
  private Vector2 contentOffset = new Vector2(0f, 300f);
  [SerializeField, Min(80f), Tooltip("说明文本的最大宽度。")]
  private float contentMaxWidth = 320f;
  [SerializeField, Tooltip("说明文本区域的内边距。")]
  private Vector2 contentPadding = new Vector2(16f, 12f);
  [SerializeField, Tooltip("目标外的四片遮罩；按 Top、Right、Bottom、Left 顺序排列。")]
  private RectTransform[] maskRects;
  [SerializeField] private GameObject maskRoot;
  [SerializeField] private RectTransform cursorRect;
  [SerializeField] private RectTransform tipsRect;
  [SerializeField] private TMP_Text contentText;
  [SerializeField] private Image maskBackground;

  private readonly Vector3[] _targetCorners = new Vector3[4];
  private GuideCursor _target;
  private RectTransform _rootRect;
  private bool _initialized;
  private bool _showMask;
  private bool _showCursor;
  private Rect _canvasBounds;

  /// <summary>绑定预制体中的 UI 子节点并确保装饰层不拦截点击。</summary>
  public override void InitView()
  {
    if (_initialized)
      return;

    _rootRect = transform as RectTransform;
    maskRoot ??= transform.Find("GuideMask")?.gameObject;
    cursorRect ??= transform.Find("GuideCursor") as RectTransform;
    tipsRect ??= transform.Find("GuideTips") as RectTransform;
    contentText ??= transform.Find("GuideTips/tmp")?.GetComponent<TMP_Text>();

    if (maskRects == null || maskRects.Length != MaskNames.Length)
    {
      maskRects = new RectTransform[MaskNames.Length];
      for (int i = 0; i < MaskNames.Length; i++)
        maskRects[i] = transform.Find($"GuideMask/{MaskNames[i]}") as RectTransform;
    }

    if (maskRoot != null)
    {
      maskBackground = maskBackground != null ? maskBackground : maskRoot.GetComponent<Image>();
      if (maskBackground != null)
        maskBackground.enabled = false;
    }

    if (_rootRect == null)
      throw new MissingComponentException("[GuideView] GuidePanel root requires a RectTransform.");
    if (maskRoot == null)
      throw new MissingComponentException("[GuideView] GuidePanel is missing the GuideMask object.");
    if (cursorRect == null)
      throw new MissingComponentException("[GuideView] GuidePanel is missing the GuideCursor RectTransform.");
    if (tipsRect == null)
      throw new MissingComponentException("[GuideView] GuidePanel is missing the GuideTips RectTransform.");
    if (contentText == null)
      throw new MissingComponentException("[GuideView] GuidePanel is missing the TMP_Text at GuideTips/tmp.");

    for (int i = 0; i < maskRects.Length; i++)
    {
      if (maskRects[i] == null)
        throw new MissingComponentException($"[GuideView] GuidePanel prefab is missing GuideMask/{MaskNames[i]}.");

      Image maskImage = maskRects[i].GetComponent<Image>();
      if (maskImage == null)
        throw new MissingComponentException($"[GuideView] GuideMask/{MaskNames[i]} requires an Image component.");

      maskImage.raycastTarget = true;
    }

    foreach (Graphic graphic in cursorRect.GetComponentsInChildren<Graphic>(true))
      graphic.raycastTarget = false;
    foreach (Graphic graphic in tipsRect.GetComponentsInChildren<Graphic>(true))
      graphic.raycastTarget = false;

    contentText.enableWordWrapping = true;
    _initialized = true;
    HideStep();
  }

  /// <summary>显示步骤说明，并将预制体内的手势和遮罩绑定到当前目标。</summary>
  /// <param name="step">当前引导步骤。</param>
  /// <param name="target">步骤对应的 UI 点击目标。</param>
  /// <returns>目标区域有效且成功开始展示时返回 true。</returns>
  public bool ShowStep(GuideStepConfig step, GuideCursor target)
  {
    if (!_initialized)
      InitView();

    if (step == null || target == null)
      return false;

    _target = target;
    _showMask = step.ShowMask;
    _showCursor = step.ShowCursor;
    _canvasBounds = _rootRect.rect;

    string content = step.Content ?? string.Empty;
    if (!string.IsNullOrWhiteSpace(content) && !string.IsNullOrWhiteSpace(step.Title))
      content = $"{step.Title}\n{content}";
    contentText.text = content;

    bool showContent = !string.IsNullOrWhiteSpace(content);
    cursorRect.gameObject.SetActive(_showCursor);
    tipsRect.gameObject.SetActive(showContent);
    maskRoot.SetActive(_showMask);

    if (showContent)
    {
      float textWidth = Mathf.Max(80f, Mathf.Min(contentMaxWidth, _canvasBounds.width - 32f));
      Vector2 preferredSize = contentText.GetPreferredValues(content, textWidth, 0f);
      contentText.rectTransform.sizeDelta = new Vector2(textWidth, preferredSize.y);
      tipsRect.sizeDelta = preferredSize + contentPadding * 2f;
    }

    return UpdateTargetLayout();
  }

  /// <summary>隐藏当前步骤的目标、遮罩、手势和说明。</summary>
  public void HideStep()
  {
    _target = null;
    _showMask = false;
    _showCursor = false;

    if (maskRoot != null)
      maskRoot.SetActive(false);
    if (cursorRect != null)
      cursorRect.gameObject.SetActive(false);
    if (tipsRect != null)
      tipsRect.gameObject.SetActive(false);
  }

  private void LateUpdate()
  {
    if (object.ReferenceEquals(_target, null))
      return;

    if (_target == null || !_target.isActiveAndEnabled)
    {
      Debug.LogError("[GuideView] The active guide target became unavailable.", _target);
      HideStep();
      return;
    }

    _canvasBounds = _rootRect.rect;
    UpdateTargetLayout();
  }

  private bool UpdateTargetLayout()
  {
    if (_canvasBounds.width <= 0f || _canvasBounds.height <= 0f)
      return true;

    if (!_target.TryGetTargetBounds(_rootRect, _targetCorners, out Bounds targetBounds))
    {
      Debug.LogError("[GuideView] The active guide target has no valid RectTransform display area.", _target);
      HideStep();
      return false;
    }

    float minX = Mathf.Max(_canvasBounds.xMin, targetBounds.min.x - highlightPadding.x);
    float maxX = Mathf.Min(_canvasBounds.xMax, targetBounds.max.x + highlightPadding.x);
    float minY = Mathf.Max(_canvasBounds.yMin, targetBounds.min.y - highlightPadding.y);
    float maxY = Mathf.Min(_canvasBounds.yMax, targetBounds.max.y + highlightPadding.y);
    if (minX >= maxX || minY >= maxY)
    {
      Debug.LogError("[GuideView] The active guide target is outside the visible GuidePanel bounds.", _target);
      HideStep();
      return false;
    }

    if (_showMask)
    {
      SetMaskRect(maskRects[0], _canvasBounds.xMin, _canvasBounds.xMax, maxY, _canvasBounds.yMax);
      SetMaskRect(maskRects[1], maxX, _canvasBounds.xMax, minY, maxY);
      SetMaskRect(maskRects[2], _canvasBounds.xMin, _canvasBounds.xMax, _canvasBounds.yMin, minY);
      SetMaskRect(maskRects[3], _canvasBounds.xMin, minX, minY, maxY);
    }

    Vector2 cursorPosition = (Vector2)targetBounds.center + cursorOffset;
    if (_showCursor)
      cursorRect.anchoredPosition = cursorPosition;

    if (tipsRect.gameObject.activeSelf)
    {
      Vector2 tipsPosition = cursorPosition + contentOffset;
      Vector2 halfSize = tipsRect.rect.size * 0.5f;
      tipsPosition.x = Mathf.Clamp(tipsPosition.x, _canvasBounds.xMin + halfSize.x, _canvasBounds.xMax - halfSize.x);
      tipsPosition.y = Mathf.Clamp(tipsPosition.y, _canvasBounds.yMin + halfSize.y, _canvasBounds.yMax - halfSize.y);
      tipsRect.anchoredPosition = tipsPosition;
    }

    return true;
  }

  private static void SetMaskRect(RectTransform rect, float minX, float maxX, float minY, float maxY)
  {
    rect.anchoredPosition = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
    rect.sizeDelta = new Vector2(Mathf.Max(0f, maxX - minX), Mathf.Max(0f, maxY - minY));
  }
}
