using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 为 UI 按钮提供按下缩放反馈；使用非缩放时间，游戏暂停时仍可正常播放。
/// </summary>
public class UIButtonScale : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
  private const float ScaleSnapSqrDistance = 0.000001f;

  [SerializeField, Tooltip("执行缩放的目标；为空时使用当前节点。")]
  private Transform target;

  [SerializeField, Range(0.5f, 1f), Tooltip("按下状态相对于初始缩放的倍率。")]
  private float pressedScale = 0.95f;

  [SerializeField, Min(0f), Tooltip("缩放平滑时间；设为 0 时立即切换。")]
  private float smoothTime = 0.02f;

  private Vector3 _normalScale;
  private Vector3 _targetScale;
  private Vector3 _velocity;
  private bool _isAnimating;

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
  }

  private void Update()
  {
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
    AnimateTo(_normalScale * pressedScale);
  }

  /// <summary>释放时恢复初始缩放。</summary>
  /// <param name="eventData">当前指针事件数据。</param>
  public void OnPointerUp(PointerEventData eventData)
  {
    AnimateTo(_normalScale);
  }

  /// <summary>指针离开按钮区域时恢复初始缩放。</summary>
  /// <param name="eventData">当前指针事件数据。</param>
  public void OnPointerExit(PointerEventData eventData)
  {
    AnimateTo(_normalScale);
  }

  private void OnDisable()
  {
    if (target != null)
    {
      target.localScale = _normalScale;
    }

    _velocity = Vector3.zero;
    _isAnimating = false;
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
