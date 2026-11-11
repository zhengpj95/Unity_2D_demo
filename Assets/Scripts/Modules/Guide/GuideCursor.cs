using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>标记可点击的 UI 引导目标，不依赖特定按钮组件。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class GuideCursor : MonoBehaviour, IPointerClickHandler
{
  [SerializeField, Tooltip("与 GuideStepData.TargetId 一致的目标标识。")]
  private string targetId;
  [SerializeField, Tooltip("可选的精确点击/高亮矩形；留空时自动使用所有 Raycast Target Graphic 的矩形并集。")]
  private RectTransform targetArea;

  private readonly List<Graphic> _graphics = new List<Graphic>();

  /// <summary>此组件对应的引导目标标识。</summary>
  public string TargetId => targetId;

  /// <summary>目标区域收到有效 UI 点击时触发。</summary>
  public event Action Clicked;

  /// <summary>目标对象或其子节点是否包含可接收射线的 UI Graphic。</summary>
  public bool HasRaycastableGraphic
  {
    get
    {
      foreach (Graphic graphic in _graphics)
      {
        if (graphic.isActiveAndEnabled && graphic.raycastTarget)
          return true;
      }

      return false;
    }
  }

  /// <summary>
  /// 获取当前有效点击 Graphic（或显式区域）在指定 Canvas 坐标系中的包围矩形。
  /// 重新读取 RectTransform 四角，因此会跟随布局、缩放和动画变化。
  /// </summary>
  /// <param name="canvasRect">用于计算遮罩位置的根 Canvas RectTransform。</param>
  /// <param name="corners">复用的四角缓存，长度必须至少为 4。</param>
  /// <param name="bounds">成功时返回 Canvas 局部坐标系中的目标包围盒。</param>
  /// <returns>目标区域有效时返回 true。</returns>
  public bool TryGetTargetBounds(RectTransform canvasRect, Vector3[] corners, out Bounds bounds)
  {
    bounds = default;
    if (canvasRect == null || corners == null || corners.Length < 4)
      return false;

    if (targetArea != null)
    {
      if (!targetArea.gameObject.activeInHierarchy)
        return false;

      targetArea.GetWorldCorners(corners);
      bounds = CreateCanvasBounds(canvasRect, corners);
      return true;
    }

    bool hasBounds = false;
    foreach (Graphic graphic in _graphics)
    {
      if (!graphic.isActiveAndEnabled || !graphic.raycastTarget)
        continue;

      graphic.rectTransform.GetWorldCorners(corners);
      Bounds graphicBounds = CreateCanvasBounds(canvasRect, corners);
      if (!hasBounds)
      {
        bounds = graphicBounds;
        hasBounds = true;
      }
      else
      {
        bounds.Encapsulate(graphicBounds);
      }
    }

    return hasBounds;
  }

  /// <summary>设置运行时创建的引导目标标识。</summary>
  /// <param name="value">与引导步骤 TargetId 对应的非空标识。</param>
  public void SetTargetId(string value)
  {
    if (string.IsNullOrWhiteSpace(value))
      throw new ArgumentException("Guide target ID cannot be empty.", nameof(value));

    targetId = value;
  }

  /// <summary>响应落在目标 Graphic 区域内的指针点击。</summary>
  /// <param name="eventData">当前指针事件数据。</param>
  public void OnPointerClick(PointerEventData eventData)
  {
    if (eventData != null && eventData.button == PointerEventData.InputButton.Left)
      Clicked?.Invoke();
  }

  private void Awake()
  {
    RefreshGraphics();
  }

  private void OnTransformChildrenChanged()
  {
    RefreshGraphics();
  }

  private void RefreshGraphics()
  {
    _graphics.Clear();
    GetComponentsInChildren(true, _graphics);
  }

  private static Bounds CreateCanvasBounds(RectTransform canvasRect, Vector3[] corners)
  {
    Vector3 firstCorner = canvasRect.InverseTransformPoint(corners[0]);
    Bounds bounds = new Bounds(firstCorner, Vector3.zero);
    for (int i = 1; i < 4; i++)
      bounds.Encapsulate(canvasRect.InverseTransformPoint(corners[i]));

    return bounds;
  }

  private void OnDestroy()
  {
    Clicked = null;
  }
}
