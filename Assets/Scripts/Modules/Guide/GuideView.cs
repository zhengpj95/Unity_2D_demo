using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>展示 GuideRunner 当前步骤，并将目标点击交互转交给 GuideModule。</summary>
public sealed class GuideView : MonoBehaviour
{
  [SerializeField] private GameObject panel;
  [SerializeField] private TMP_Text titleText;
  [SerializeField] private TMP_Text contentText;
  [SerializeField] private TMP_Text stepText;
  [SerializeField, Tooltip("GuideCursor.TargetId 对应的点击手势 Prefab。")]
  private GameObject cursorPrefab;
  [SerializeField, Tooltip("高亮按钮以外区域的遮罩颜色。")]
  private Color maskColor = new Color(0f, 0f, 0f, 0.65f);
  [SerializeField, Tooltip("高亮按钮区域向外扩展的边距。")]
  private Vector2 highlightPadding = new Vector2(12f, 12f);
  [SerializeField, Tooltip("点击手势相对于按钮中心的偏移。")]
  private Vector2 cursorOffset = new Vector2(48f, -36f);
  [SerializeField, Tooltip("引导正文相对于目标或手势的位置偏移。")]
  private Vector2 contentOffset = new Vector2(0f, 104f);
  [SerializeField, Min(120f), Tooltip("引导正文气泡的最大宽度。")]
  private float contentMaxWidth = 420f;
  [SerializeField, Tooltip("引导正文气泡的内边距。")]
  private Vector2 contentPadding = new Vector2(16f, 12f);

  private GuideModule _guideModule;
  private GuideRunner _runner;
  private CanvasGroup _panelCanvasGroup;
  private Canvas _rootCanvas;
  private RectTransform _canvasRect;
  private RectTransform _overlayRect;
  private Image[] _maskImages;
  private readonly Vector3[] _targetCorners = new Vector3[4];
  private GameObject _overlayObject;
  private GameObject _cursorObject;
  private GameObject _contentBubbleObject;
  private RectTransform _contentBubbleRect;
  private RectTransform _contentTextRect;
  private Transform _contentOriginalParent;
  private int _contentOriginalSiblingIndex;
  private Vector2 _contentOriginalAnchorMin;
  private Vector2 _contentOriginalAnchorMax;
  private Vector2 _contentOriginalPivot;
  private Vector2 _contentOriginalAnchoredPosition;
  private Vector2 _contentOriginalSizeDelta;
  private bool _contentOriginalRaycastTarget;
  private bool _contentOriginalWordWrapping;
  private TextOverflowModes _contentOriginalOverflowMode;
  private string _contentOriginalText;
  private GuideCursor _activeTarget;
  private bool _showMask;
  private bool _showCursor;
  private bool _showContent;
  private bool _originalBlocksRaycasts;
  private bool _originalInteractable;
  private float _originalAlpha;
  private bool _referencesValid;
  private bool _wasPaused;

  private void Awake()
  {
    _referencesValid = ValidateReferences();
    if (!_referencesValid)
      return;

    _contentTextRect = contentText.rectTransform;
    _contentOriginalParent = _contentTextRect.parent;
    _contentOriginalSiblingIndex = _contentTextRect.GetSiblingIndex();
    _contentOriginalAnchorMin = _contentTextRect.anchorMin;
    _contentOriginalAnchorMax = _contentTextRect.anchorMax;
    _contentOriginalPivot = _contentTextRect.pivot;
    _contentOriginalAnchoredPosition = _contentTextRect.anchoredPosition;
    _contentOriginalSizeDelta = _contentTextRect.sizeDelta;
    _contentOriginalRaycastTarget = contentText.raycastTarget;
    _contentOriginalWordWrapping = contentText.enableWordWrapping;
    _contentOriginalOverflowMode = contentText.overflowMode;
    _contentOriginalText = contentText.text;

    _panelCanvasGroup = panel.GetComponent<CanvasGroup>();
    if (_panelCanvasGroup == null)
      _panelCanvasGroup = panel.AddComponent<CanvasGroup>();
    _originalAlpha = _panelCanvasGroup.alpha;
    _originalBlocksRaycasts = _panelCanvasGroup.blocksRaycasts;
    _originalInteractable = _panelCanvasGroup.interactable;
    _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
    if (_rootCanvas != null)
      _canvasRect = _rootCanvas.transform as RectTransform;
    panel.SetActive(false);
  }

  /// <summary>绑定引导模块并根据其当前状态恢复界面。</summary>
  /// <param name="module">由 ModuleManager 管理且已初始化的 GuideModule。</param>
  public void Bind(GuideModule module)
  {
    if (module == null)
      throw new ArgumentNullException(nameof(module));
    if (!_referencesValid)
      throw new InvalidOperationException("[GuideView] Required UI references are missing.");
    if (module.Runner == null)
      throw new InvalidOperationException("[GuideView] GuideModule must be initialized before binding.");

    Unbind();
    _guideModule = module;
    _runner = module.Runner;
    _runner.OnStepChanged += OnStepChanged;
    _runner.OnStateChanged += OnStateChanged;

    if ((_runner.State == GuideState.Running || _runner.State == GuideState.Paused)
        && _runner.CurrentStep != null)
    {
      OnStepChanged(_runner.CurrentStep, _runner.CurrentIndex, _runner.Config.Steps.Count);
    }

    OnStateChanged(_runner.State);
  }

  /// <summary>解除步骤与状态事件订阅，并清除模块引用。</summary>
  public void Unbind()
  {
    ClearTarget();
    if (_runner != null)
    {
      _runner.OnStepChanged -= OnStepChanged;
      _runner.OnStateChanged -= OnStateChanged;
    }

    _runner = null;
    _guideModule = null;
    if (panel != null)
      panel.SetActive(false);
  }

  private void OnDestroy()
  {
    Unbind();
    RestoreContentText();
    if (_overlayObject != null)
      Destroy(_overlayObject);
  }

  private bool ValidateReferences()
  {
    if (panel != null
        && titleText != null
        && contentText != null
        && stepText != null)
    {
      return true;
    }

    Debug.LogError("[GuideView] Assign panel, title/content/step text in the Inspector.", this);
    return false;
  }

  private void OnStepChanged(GuideStepConfig step, int index, int total)
  {
    if (step == null || index < 0 || total <= index)
    {
      Debug.LogError("[GuideView] Received invalid step data from GuideRunner.", this);
      return;
    }

    titleText.text = step.Title ?? string.Empty;
    stepText.text = $"{index + 1} / {total}";
    ClearTarget();

    if (!ShowTarget(step))
    {
      ClearTarget();
      SetPanelVisible(true);
      return;
    }

    SetPanelVisible(IsTargetWithinPanel());
  }

  private void OnStateChanged(GuideState state)
  {
    if (state == GuideState.Paused)
    {
      _wasPaused = true;
      ClearTarget();
      SetPanelVisible(true);
      return;
    }

    if (state != GuideState.Running)
    {
      _wasPaused = false;
      ClearTarget();
      panel.SetActive(false);
      return;
    }

    if (_wasPaused && _runner != null && _runner.CurrentStep != null)
    {
      _wasPaused = false;
      OnStepChanged(_runner.CurrentStep, _runner.CurrentIndex, _runner.Config.Steps.Count);
    }
    else
    {
      panel.SetActive(true);
    }
  }

  private bool ShowTarget(GuideStepConfig step)
  {
    if (_canvasRect == null)
    {
      Debug.LogError("[GuideView] A parent Canvas is required for targeted guide steps.", this);
      return false;
    }

    // 面板内的目标原本随面板隐藏；先激活但保持透明，才能查找到目标组件。
    SetPanelVisible(false);
    GuideCursor target = FindTarget(step.TargetId);
    if (target == null)
      return false;

    if (!target.HasRaycastableGraphic)
    {
      Debug.LogError($"[GuideView] Guide target '{step.TargetId}' has no enabled, raycastable UI Graphic on itself or a child.", target);
      return false;
    }

    ClearTarget();
    _activeTarget = target;
    _activeTarget.Clicked += OnTargetClicked;
    return ShowStepVisuals(step);
  }

  private bool ShowStepVisuals(GuideStepConfig step)
  {
    _showMask = step.ShowMask;
    _showCursor = step.ShowCursor;
    _showContent = !string.IsNullOrWhiteSpace(step.Content);
    if (!_showMask && !_showCursor && !_showContent)
      return true;

    if (_showCursor && cursorPrefab == null)
    {
      Debug.LogError("[GuideView] Assign the GuideCursor gesture Prefab when ShowCursor is enabled.", this);
      return false;
    }

    if (_canvasRect == null || !EnsureOverlay())
      return false;

    for (int i = 0; i < _maskImages.Length; i++)
      _maskImages[i].gameObject.SetActive(_showMask);

    ConfigureContentBubble(step.Content);

    if (_showCursor)
    {
      if (_cursorObject == null)
      {
        _cursorObject = Instantiate(cursorPrefab, _overlayRect, false);
        _cursorObject.name = "GuideCursorGesture";
        foreach (Graphic graphic in _cursorObject.GetComponentsInChildren<Graphic>(true))
          graphic.raycastTarget = false;

        if (_cursorObject.transform is not RectTransform)
        {
          Debug.LogError("[GuideView] The GuideCursor gesture Prefab must have a RectTransform root.", this);
          Destroy(_cursorObject);
          _cursorObject = null;
          return false;
        }
      }

      _cursorObject.SetActive(true);
    }
    else if (_cursorObject != null)
    {
      _cursorObject.SetActive(false);
    }

    _overlayObject.SetActive(true);
    _overlayObject.transform.SetAsLastSibling();
    return UpdateTargetOverlay();
  }

  private GuideCursor FindTarget(string targetId)
  {
    GuideCursor found = null;
    bool hasDuplicate = false;
    GuideCursor[] targets = FindObjectsOfType<GuideCursor>(true);
    foreach (GuideCursor target in targets)
    {
      if (!target.isActiveAndEnabled || target.TargetId != targetId)
        continue;

      if (found != null)
      {
        Debug.LogError($"[GuideView] More than one active GuideCursor uses target ID '{targetId}'.", this);
        hasDuplicate = true;
        break;
      }

      found = target;
    }

    if (hasDuplicate)
      return null;

    if (found == null)
      Debug.LogError($"[GuideView] No active GuideCursor matches target ID '{targetId}'.", this);

    return found;
  }

  /// <summary>面板内的目标需要保留父面板显示，避免目标本身随说明面板一起隐藏。</summary>
  private bool IsTargetWithinPanel()
  {
    Transform targetTransform = _activeTarget.transform;
    Transform panelTransform = panel.transform;
    return targetTransform == panelTransform || targetTransform.IsChildOf(panelTransform);
  }

  private bool EnsureOverlay()
  {
    if (_overlayObject != null)
      return true;

    _overlayObject = new GameObject("GuideInteractionOverlay", typeof(RectTransform));
    _overlayObject.transform.SetParent(_rootCanvas.transform, false);
    _overlayRect = (RectTransform)_overlayObject.transform;
    _overlayRect.anchorMin = Vector2.zero;
    _overlayRect.anchorMax = Vector2.one;
    _overlayRect.offsetMin = Vector2.zero;
    _overlayRect.offsetMax = Vector2.zero;

    _maskImages = new Image[4];
    for (int i = 0; i < _maskImages.Length; i++)
    {
      GameObject maskObject = new GameObject($"Mask{i}", typeof(RectTransform), typeof(Image));
      maskObject.transform.SetParent(_overlayRect, false);
      Image maskImage = maskObject.GetComponent<Image>();
      maskImage.color = maskColor;
      maskImage.raycastTarget = true;
      RectTransform maskRect = (RectTransform)maskObject.transform;
      maskRect.anchorMin = new Vector2(0.5f, 0.5f);
      maskRect.anchorMax = new Vector2(0.5f, 0.5f);
      maskRect.pivot = new Vector2(0.5f, 0.5f);
      _maskImages[i] = maskImage;
    }

    _overlayObject.SetActive(false);
    return true;
  }

  private void LateUpdate()
  {
    if (ReferenceEquals(_activeTarget, null))
      return;

    if (_activeTarget == null || !_activeTarget.isActiveAndEnabled)
    {
      Debug.LogError("[GuideView] The active guide target became unavailable before it was clicked.", this);
      ClearTarget();
      SetPanelVisible(true);
      return;
    }

    UpdateTargetOverlay();
  }

  private bool UpdateTargetOverlay()
  {
    if (!_activeTarget.TryGetTargetBounds(_canvasRect, _targetCorners, out Bounds targetBounds))
    {
      Debug.LogError("[GuideView] The active guide target has no valid RectTransform display area.", _activeTarget);
      ClearTarget();
      SetPanelVisible(true);
      return false;
    }

    Rect canvasBounds = _canvasRect.rect;
    float minX = Mathf.Max(canvasBounds.xMin, targetBounds.min.x - highlightPadding.x);
    float maxX = Mathf.Min(canvasBounds.xMax, targetBounds.max.x + highlightPadding.x);
    float minY = Mathf.Max(canvasBounds.yMin, targetBounds.min.y - highlightPadding.y);
    float maxY = Mathf.Min(canvasBounds.yMax, targetBounds.max.y + highlightPadding.y);

    if ((_showMask || _showCursor || _showContent) && (minX >= maxX || minY >= maxY))
    {
      Debug.LogError("[GuideView] The active guide target is outside the visible Canvas bounds.", _activeTarget);
      ClearTarget();
      SetPanelVisible(true);
      return false;
    }

    if (_showMask)
    {
      SetMaskRect(_maskImages[0].rectTransform, canvasBounds.xMin, minX, canvasBounds.yMin, canvasBounds.yMax);
      SetMaskRect(_maskImages[1].rectTransform, maxX, canvasBounds.xMax, canvasBounds.yMin, canvasBounds.yMax);
      SetMaskRect(_maskImages[2].rectTransform, minX, maxX, canvasBounds.yMin, minY);
      SetMaskRect(_maskImages[3].rectTransform, minX, maxX, maxY, canvasBounds.yMax);
    }

    if (_showCursor && _cursorObject != null)
    {
      RectTransform cursorRect = (RectTransform)_cursorObject.transform;
      cursorRect.anchoredPosition = (Vector2)targetBounds.center + cursorOffset;
    }

    if (_showContent && _contentBubbleObject != null)
    {
      Vector2 anchorOffset = _showCursor ? cursorOffset : Vector2.zero;
      Vector2 bubblePosition = (Vector2)targetBounds.center + anchorOffset + contentOffset;
      Vector2 halfSize = _contentBubbleRect.sizeDelta * 0.5f;
      bubblePosition.x = Mathf.Clamp(
        bubblePosition.x,
        canvasBounds.xMin + halfSize.x,
        canvasBounds.xMax - halfSize.x);
      bubblePosition.y = Mathf.Clamp(
        bubblePosition.y,
        canvasBounds.yMin + halfSize.y,
        canvasBounds.yMax - halfSize.y);
      _contentBubbleRect.anchoredPosition = bubblePosition;
    }

    return true;
  }

  /// <summary>将非空正文放入遮罩层中的气泡，避免依赖说明面板的显隐状态。</summary>
  private void ConfigureContentBubble(string content)
  {
    if (!_showContent)
    {
      if (_contentBubbleObject != null)
        _contentBubbleObject.SetActive(false);
      return;
    }

    if (_contentBubbleObject == null)
    {
      _contentBubbleObject = new GameObject("GuideContentBubble", typeof(RectTransform), typeof(Image));
      _contentBubbleRect = (RectTransform)_contentBubbleObject.transform;
      _contentBubbleRect.SetParent(_overlayRect, false);
      _contentBubbleRect.anchorMin = new Vector2(0.5f, 0.5f);
      _contentBubbleRect.anchorMax = new Vector2(0.5f, 0.5f);
      _contentBubbleRect.pivot = new Vector2(0.5f, 0.5f);

      Image background = _contentBubbleObject.GetComponent<Image>();
      background.color = new Color(0.04f, 0.06f, 0.1f, 0.92f);
      background.raycastTarget = false;

      _contentTextRect.SetParent(_contentBubbleRect, false);
      _contentTextRect.anchorMin = Vector2.zero;
      _contentTextRect.anchorMax = Vector2.one;
      _contentTextRect.pivot = new Vector2(0.5f, 0.5f);
      _contentTextRect.offsetMin = contentPadding;
      _contentTextRect.offsetMax = -contentPadding;
      contentText.raycastTarget = false;
      contentText.enableWordWrapping = true;
      contentText.overflowMode = TextOverflowModes.Truncate;
    }

    contentText.text = content;
    float maxWidth = Mathf.Max(120f, Mathf.Min(contentMaxWidth, _canvasRect.rect.width - 32f));
    float maxHeight = Mathf.Max(80f, _canvasRect.rect.height - 32f);
    float textWidth = Mathf.Max(80f, maxWidth - contentPadding.x * 2f);
    Vector2 preferredSize = contentText.GetPreferredValues(content, textWidth, 0f);
    float bubbleHeight = Mathf.Min(preferredSize.y + contentPadding.y * 2f, maxHeight);
    _contentBubbleRect.sizeDelta = new Vector2(maxWidth, bubbleHeight);
    _contentBubbleObject.SetActive(true);
  }

  /// <summary>销毁引导视图前恢复正文控件的原始父级与布局。</summary>
  private void RestoreContentText()
  {
    if (_contentTextRect == null || _contentOriginalParent == null)
      return;

    _contentTextRect.SetParent(_contentOriginalParent, false);
    _contentTextRect.SetSiblingIndex(
      Mathf.Clamp(_contentOriginalSiblingIndex, 0, _contentOriginalParent.childCount - 1));
    _contentTextRect.anchorMin = _contentOriginalAnchorMin;
    _contentTextRect.anchorMax = _contentOriginalAnchorMax;
    _contentTextRect.pivot = _contentOriginalPivot;
    _contentTextRect.anchoredPosition = _contentOriginalAnchoredPosition;
    _contentTextRect.sizeDelta = _contentOriginalSizeDelta;
    contentText.raycastTarget = _contentOriginalRaycastTarget;
    contentText.enableWordWrapping = _contentOriginalWordWrapping;
    contentText.overflowMode = _contentOriginalOverflowMode;
    contentText.text = _contentOriginalText;
  }

  private void SetMaskRect(RectTransform rect, float minX, float maxX, float minY, float maxY)
  {
    rect.anchoredPosition = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
    rect.sizeDelta = new Vector2(Mathf.Max(0f, maxX - minX), Mathf.Max(0f, maxY - minY));
  }

  private void ClearTarget()
  {
    if (_activeTarget != null)
      _activeTarget.Clicked -= OnTargetClicked;

    _activeTarget = null;
    _showMask = false;
    _showCursor = false;
    _showContent = false;
    if (_contentBubbleObject != null)
      _contentBubbleObject.SetActive(false);
    if (_overlayObject != null)
      _overlayObject.SetActive(false);
  }

  private void SetPanelVisible(bool visible)
  {
    panel.SetActive(true);
    _panelCanvasGroup.alpha = visible ? _originalAlpha : 0f;
    _panelCanvasGroup.blocksRaycasts = visible && _originalBlocksRaycasts;
    _panelCanvasGroup.interactable = visible && _originalInteractable;
  }

  private void OnTargetClicked()
  {
    if (_runner != null && _runner.State == GuideState.Running && _activeTarget != null)
      _guideModule?.Next();
  }
}
