using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UISizeFitter 是一个自定义的 UI 组件，用于根据目标 RectTransform 的尺寸变化自动调整自身的尺寸。
/// 使用 DrivenRectTransformTracker 来控制 RectTransform 的尺寸变化，避免手动修改尺寸时被覆盖。
/// 方法 SetDirty() 会在目标尺寸变化时被调用，计算新的尺寸并应用到自身的 RectTransform 上。
/// 功能包括：
/// 1. 跟随目标的宽度和高度变化。
/// 2. 支持宽度和高度的偏移量和缩放比例。
/// 3. 支持最小和最大尺寸约束。
/// 4. 在编辑器中实时更新尺寸变化。
/// 5. 实现 ILayoutSelfController 接口，确保在布局系统中正确更新自身尺寸。
/// 6. 在 OnValidate() 中处理编辑器中的属性变化，确保在属性修改后立即更新尺寸。
/// 7. 使用 ExecuteAlways 属性，使组件在编辑器和运行时都能生效。
/// 8. 使用 RequireComponent 属性，确保组件附加在具有 RectTransform 的 GameObject 上。
/// 9. 提供公共属性和方法，方便在代码中动态设置目标和调整尺寸。
/// 10. 提供 OnEnable() 和 OnDisable() 方法，确保在启用和禁用组件时正确管理 DrivenRectTransformTracker。
/// 11. 提供 Update() 方法，实时检测目标尺寸变化并更新自身尺寸。
/// </summary>

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class UISizeFitter : MonoBehaviour, ILayoutSelfController
{
  [Header("目标设置")]
  [Tooltip("需要跟随尺寸变化的目标 RectTransform")]
  public RectTransform target;

  [Header("Tracker设置")]
  [Tooltip("是否使用 DrivenRectTransformTracker 来控制 RectTransform 的尺寸变化，避免手动修改尺寸时被覆盖")]
  [HideInInspector]
  public bool useTracker = true;

  [Header("跟随设置")]
  [Tooltip("是否跟随目标的宽度变化")]
  public bool followWidth = true;
  [Tooltip("是否跟随目标的高度变化")]
  public bool followHeight = true;

  [Header("尺寸设置")]
  [Tooltip("宽度偏移量")]
  public float widthOffset = 0f;
  [Tooltip("高度偏移量")]
  public float heightOffset = 0f;
  [Tooltip("宽度缩放比例")]
  public float widthScale = 1f;
  [Tooltip("高度缩放比例")]
  public float heightScale = 1f;

  [Header("约束设置")]
  [Tooltip("最小宽度，默认 0")]
  public float minWidth = 0f;
  [Tooltip("最小高度，默认 0")]
  public float minHeight = 0f;
  [Tooltip("最大宽度，默认 float.MaxValue")]
  public float maxWidth = float.MaxValue;
  [Tooltip("最大高度，默认 float.MaxValue")]
  public float maxHeight = float.MaxValue;


  private RectTransform m_rectTransform;
  private Vector2 m_lastTargetRectSize = Vector2.zero;
  private DrivenRectTransformTracker m_tracker = new DrivenRectTransformTracker();
  private LayoutGroup m_parentLayoutGroup;

  public bool IsActive()
  {
    return gameObject.activeInHierarchy && target != null;
  }

  private void Awake()
  {
    m_rectTransform = GetComponent<RectTransform>();
    RefreshParentLayoutGroup();
  }

  private void OnEnable()
  {
    UpdateTracker();
    SetDirty();
  }

  private void OnDisable()
  {
    m_tracker.Clear();
    NotifyLayoutRebuild();
  }

  private void OnTransformParentChanged()
  {
    RefreshParentLayoutGroup();
    NotifyLayoutRebuild();
  }

  private void Update()
  {
    if (target == null)
      return;

    // 检查目标的尺寸是否发生变化，如果发生变化则更新自身尺寸
    // 坑点修正：使用 target.rect.size 检测，完美兼容所有锚点(Anchor)类型
    Vector2 targetRectSize = target.rect.size;
    if (targetRectSize != m_lastTargetRectSize)
    {
      SetDirty();
      m_lastTargetRectSize = targetRectSize;
    }
  }

  private void SetDirty()
  {
    if (!gameObject.activeInHierarchy || target == null)
    {
      return;
    }

    // 计算新尺寸：基于目标的真实 rect 尺寸计算
    Vector2 targetSize = target.rect.size;
    bool sizeChanged = false;

    if (followWidth)
    {
      float finalWidth = Mathf.Clamp(targetSize.x * widthScale + widthOffset, minWidth, maxWidth);
      if (!Mathf.Approximately(m_rectTransform.rect.width, finalWidth))
      {
        m_rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, finalWidth);
        sizeChanged = true;
      }
    }
    if (followHeight)
    {
      float finalHeight = Mathf.Clamp(targetSize.y * heightScale + heightOffset, minHeight, maxHeight);
      if (!Mathf.Approximately(m_rectTransform.rect.height, finalHeight))
      {
        m_rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight);
        sizeChanged = true;
      }
    }

    if (sizeChanged)
    {
      NotifyLayoutRebuild();
    }
  }

  private void RefreshParentLayoutGroup()
  {
    m_parentLayoutGroup = GetComponentInParent<LayoutGroup>();
  }

  private void NotifyLayoutRebuild()
  {
    if (m_rectTransform != null)
    {
      LayoutRebuilder.MarkLayoutForRebuild(m_rectTransform);
    }

    if (m_parentLayoutGroup != null)
    {
      RectTransform parentRectTransform = m_parentLayoutGroup.transform as RectTransform;
      if (parentRectTransform != null)
      {
        LayoutRebuilder.MarkLayoutForRebuild(parentRectTransform);
      }
    }
  }

  private void UpdateTracker()
  {
    m_tracker.Clear();
    if (!useTracker || !enabled || target == null)
    {
      return;
    }
    DrivenTransformProperties drivenProperties = DrivenTransformProperties.None;
    if (followWidth)
    {
      drivenProperties |= DrivenTransformProperties.SizeDeltaX;
    }
    if (followHeight)
    {
      drivenProperties |= DrivenTransformProperties.SizeDeltaY;
    }
    m_tracker.Add(this, m_rectTransform, drivenProperties);
  }

  // ILayoutSelfController 接口实现
  public void SetLayoutHorizontal() => SetDirty();
  public void SetLayoutVertical() => SetDirty();

#if UNITY_EDITOR
  private void OnValidate()
  {
    UnityEditor.EditorApplication.delayCall += () =>
    {
      if (this == null) return;
      UpdateTracker();
      SetDirty();
    };
  }
#endif
}
