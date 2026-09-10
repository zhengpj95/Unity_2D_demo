using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>
/// 为 UI 按钮提供按下缩放反馈；使用非缩放时间，游戏暂停时仍可正常播放。
/// </summary>
public class UIButtonScale : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
  private const float ScaleSnapSqrDistance = 0.000001f;

  [SerializeField, Tooltip("执行缩放的目标；为空时使用当前节点。")]
  private Transform target;

  [SerializeField, Range(0.5f, 1f), Tooltip("按下状态相对于初始缩放的倍率。")]
  private float pressedScale = 0.95f;

  [SerializeField, Min(0f), Tooltip("缩放平滑时间；设为 0 时立即切换。")]
  private float smoothTime = 0.02f;

  [SerializeField, Tooltip("普通短按松开时触发。")]
  private UnityEvent onClick = new UnityEvent();

  [SerializeField, Tooltip("是否启用长按回调。")]
  private bool enableLongPress;

  [SerializeField, Min(0.1f), Tooltip("持续按住多少秒后触发长按回调。")]
  private float longPressDuration = 0.5f;

  [SerializeField, Tooltip("达到长按时长时触发；每次按下最多调用一次，并取消本次普通点击。")]
  private UnityEvent onLongPress = new UnityEvent();

  private Vector3 _normalScale;
  private Vector3 _targetScale;
  private Vector3 _velocity;
  private bool _isAnimating;
  private bool _isPointerDown;
  private float _pressedDuration;
  private PointerEventData _pressedEventData;

  private void OnEnable()
  {
    if (target == null)
    {
      target = transform;
    }

    _normalScale = target.localScale;
    _targetScale = _normalScale;
    _velocity = Vector3.zero;
    _isAnimating = false;
    ResetLongPress();
  }

  private void Update()
  {
    UpdateLongPress();

    if (!_isAnimating)
    {
      return;
    }

    if (target == null)
    {
      _isAnimating = false;
      return;
    }

    if (smoothTime <= 0f)
    {
      ApplyScale(_targetScale);
      return;
    }

    target.localScale = Vector3.SmoothDamp(
        target.localScale,
        _targetScale,
        ref _velocity,
        smoothTime,
        Mathf.Infinity,
        Time.unscaledDeltaTime);

    if ((target.localScale - _targetScale).sqrMagnitude <= ScaleSnapSqrDistance)
    {
      ApplyScale(_targetScale);
    }
  }

  /// <summary>按下时切换到按压缩放。</summary>
  /// <param name="eventData">当前指针事件数据。</param>
  public void OnPointerDown(PointerEventData eventData)
  {
    if (enableLongPress)
    {
      _isPointerDown = true;
      _pressedDuration = 0f;
      _pressedEventData = eventData;
    }

    AnimateTo(_normalScale * pressedScale);
  }

  /// <summary>释放时恢复初始缩放。</summary>
  /// <param name="eventData">当前指针事件数据。</param>
  public void OnPointerUp(PointerEventData eventData)
  {
    ResetLongPress();
    AnimateTo(_normalScale);
  }

  /// <summary>指针离开按钮区域时恢复初始缩放。</summary>
  /// <param name="eventData">当前指针事件数据。</param>
  public void OnPointerExit(PointerEventData eventData)
  {
    ResetLongPress();
    AnimateTo(_normalScale);
  }

  /// <summary>未被长按消费的短按在松开后触发普通点击回调。</summary>
  /// <param name="eventData">当前指针事件数据。</param>
  public void OnPointerClick(PointerEventData eventData)
  {
    if (eventData == null || eventData.eligibleForClick)
    {
      onClick?.Invoke();
    }
  }

  private void OnDisable()
  {
    if (target != null)
    {
      target.localScale = _normalScale;
    }

    _velocity = Vector3.zero;
    _isAnimating = false;
    ResetLongPress();
  }

  /// <summary>累计长按时间，并在达到阈值时触发一次 Inspector 回调。</summary>
  private void UpdateLongPress()
  {
    if (!enableLongPress || !_isPointerDown)
    {
      return;
    }

    _pressedDuration += Time.unscaledDeltaTime;
    if (_pressedDuration < longPressDuration)
    {
      return;
    }

    PointerEventData pressedEventData = _pressedEventData;
    ResetLongPress();

    // 长按已经执行，本次抬起不再派发 Button.onClick 或其他点击回调。
    if (pressedEventData != null)
    {
      pressedEventData.eligibleForClick = false;
    }

    onLongPress?.Invoke();
  }

  /// <summary>取消当前长按计时。</summary>
  private void ResetLongPress()
  {
    _isPointerDown = false;
    _pressedDuration = 0f;
    _pressedEventData = null;
  }

  /// <summary>开始向目标缩放平滑过渡。</summary>
  private void AnimateTo(Vector3 scale)
  {
    if (target == null)
    {
      return;
    }

    _targetScale = scale;
    _velocity = Vector3.zero;
    _isAnimating = true;
  }

  /// <summary>应用最终缩放并结束动画，避免 SmoothDamp 无限逼近。</summary>
  private void ApplyScale(Vector3 scale)
  {
    target.localScale = scale;
    _velocity = Vector3.zero;
    _isAnimating = false;
  }
}
