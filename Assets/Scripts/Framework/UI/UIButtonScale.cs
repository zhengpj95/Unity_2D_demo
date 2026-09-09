using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonScale : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
  [SerializeField]
  private Transform target;

  [SerializeField, Range(0.5f, 1f)]
  private float pressedScale = 0.95f;

  [SerializeField]
  private float smoothTime = 0.02f;

  private Vector3 _normalScale;
  private Vector3 _targetScale;
  private Vector3 _velocity;

  private void Awake()
  {
    if (target == null)
      target = transform;

    _normalScale = target.localScale;
    _targetScale = _normalScale;
  }

  private void Update()
  {
    target.localScale = Vector3.SmoothDamp(
        target.localScale,
        _targetScale,
        ref _velocity,
        smoothTime
    );
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    _targetScale = _normalScale * pressedScale;
  }

  public void OnPointerUp(PointerEventData eventData)
  {
    Restore();
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    Restore();
  }

  private void Restore()
  {
    _targetScale = _normalScale;
  }

  private void OnDisable()
  {
    if (target == null)
      return;

    _velocity = Vector3.zero;
    _targetScale = _normalScale;
    target.localScale = _normalScale;
  }
}