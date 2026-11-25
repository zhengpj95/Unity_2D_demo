using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 屏幕虚拟摇杆视图：把鼠标拖拽或触摸拖拽转换为归一化方向向量，并驱动摇杆柄显示。
/// 挂在 JoystickPanel 根节点；imgBg 接收指针事件，imgHandle 只做视觉跟随。
/// uGUI 的 Pointer/Drag 事件同时覆盖鼠标与触屏，不需要分别读取 Input 与 Input.touches。
/// </summary>
public sealed class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
  [SerializeField] private RectTransform _background;
  [SerializeField] private RectTransform _handle;
  [SerializeField, Range(0.05f, 0.5f)]
  [Tooltip("摇杆死区占可用行程的比例。拖拽距离小于 半径×该值 时视为无输入，避免轻微抖动导致角色移动；值越大越迟钝，越小越灵敏。")]
  private float _deadZoneRatio = 0.1f;

  private int _activePointerId;
  private bool _hasActivePointer;
  private Vector2 _direction;

  /// <summary>当前归一化移动方向；未拖拽或处于死区时为 Vector2.zero。</summary>
  public Vector2 Direction => _direction;

  /// <summary>方向变化时通知订阅者；参数为归一化向量或零向量。</summary>
  public event Action<Vector2> DirectionChanged;

  private void Awake()
  {
    // 运行时挂载脚本时序列化引用为空，因此按预制体固定节点名补查找。
    if (_background == null)
      _background = transform.Find("imgBg") as RectTransform;
    if (_handle == null && _background != null)
      _handle = _background.Find("imgHandle") as RectTransform;

    if (_background == null)
      Debug.LogError("[JoystickView] 未找到 imgBg 节点，摇杆无法接收指针事件。", this);
  }

  /// <summary>第一个按下的指针获得摇杆控制权，其余指针忽略，避免多指同时拉动。</summary>
  public void OnPointerDown(PointerEventData eventData)
  {
    if (_hasActivePointer) return;
    _hasActivePointer = true;
    _activePointerId = eventData.pointerId;
    UpdateFromPointer(eventData);
  }

  public void OnDrag(PointerEventData eventData)
  {
    if (!_hasActivePointer || eventData.pointerId != _activePointerId) return;
    UpdateFromPointer(eventData);
  }

  /// <summary>只有控制中的指针抬起才归中，其他手指离开不影响当前拖拽。</summary>
  public void OnPointerUp(PointerEventData eventData)
  {
    if (!_hasActivePointer || eventData.pointerId != _activePointerId) return;
    _hasActivePointer = false;
    ApplyDirection(Vector2.zero, Vector2.zero);
  }

  private void UpdateFromPointer(PointerEventData eventData)
  {
    if (_background == null) return;

    // 将屏幕坐标转成底图局部坐标，Screen Space Overlay 下 pressEventCamera 为 null 由 Unity 处理。
    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
          _background, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
      return;

    // 摇杆柄不能超出底盘，可用行程取底图与摇杆柄尺寸差的一半。
    float handleSize = _handle == null ? 0f : Mathf.Min(_handle.rect.width, _handle.rect.height);
    float radius = Mathf.Max(1f,
      (Mathf.Min(_background.rect.width, _background.rect.height) - handleSize) * 0.5f);

    Vector2 clamped = Vector2.ClampMagnitude(localPoint, radius);
    Vector2 direction = clamped.magnitude < radius * _deadZoneRatio ? Vector2.zero : clamped / radius;
    ApplyDirection(direction, clamped);
  }

  private void ApplyDirection(Vector2 direction, Vector2 handlePosition)
  {
    _direction = direction;
    if (_handle != null)
      _handle.anchoredPosition = handlePosition;
    DirectionChanged?.Invoke(_direction);
  }

  /// <summary>清零方向与归属指针；界面隐藏或状态切换时由 Presenter 调用。</summary>
  public void ResetInput()
  {
    _hasActivePointer = false;
    ApplyDirection(Vector2.zero, Vector2.zero);
  }

  private void OnDisable()
  {
    // 禁用时释放指针并归中，防止再次启用后沿用旧方向。
    ResetInput();
  }
}
