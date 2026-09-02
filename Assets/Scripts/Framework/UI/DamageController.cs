using UnityEngine;

public class DamageController : MonoBehaviour
{
  public static DamageController Instance { get; private set; }
  public RectTransform damagePrefab;
  public RectTransform point;

  private Canvas _canvas;

  private void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject);
  }

  public void ShowDamage(int damageAmount, Vector3 worldPosition)
  {
    if (damagePrefab == null || !TryGetCanvas(out Canvas canvas))
    {
      return;
    }

    Camera worldCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
    if (worldCamera == null)
    {
      Debug.LogError("[DamageController] No camera is available to project damage positions.", this);
      return;
    }

    Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
    if (screenPosition.z < 0f)
    {
      return;
    }

    Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : worldCamera;
    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
          point, screenPosition, eventCamera, out Vector2 localPosition))
    {
      return;
    }

    RectTransform damageTransform = Instantiate(damagePrefab, point);
    damageTransform.anchoredPosition = localPosition;

    UI_Damage damage = damageTransform.GetComponent<UI_Damage>();
    if (damage != null)
    {
      damage.SetDamageText(damageAmount);
    }
  }

  private bool TryGetCanvas(out Canvas canvas)
  {
    if (point == null)
    {
      Canvas targetCanvas = FindObjectOfType<Canvas>();
      if (targetCanvas == null)
      {
        Debug.LogError("[DamageController] No Canvas is available for damage text.", this);
        canvas = null;
        return false;
      }

      GameObject pointObject = new GameObject("DamagePoint", typeof(RectTransform));
      point = pointObject.GetComponent<RectTransform>();
      point.SetParent(targetCanvas.transform, false);
    }

    if (_canvas == null)
    {
      _canvas = point.GetComponentInParent<Canvas>();
    }

    canvas = _canvas;
    if (canvas != null)
    {
      return true;
    }

    Debug.LogError("[DamageController] The damage point must be under a Canvas.", this);
    return false;
  }
}
