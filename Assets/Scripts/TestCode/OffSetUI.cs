using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// RectTransform - 矩形锚的 offsetMin / offsetMax 使用示例
/// 挂载到一个 UI 元素（例如 Image）上，并且该元素的锚点已经设置为矩形模式（如全屏拉伸）
/// </summary>
[RequireComponent(typeof(RectTransform), typeof(Image))]
public class OffSetUI : MonoBehaviour
{
  [Header("边距设置（像素）")] [SerializeField] private float leftMargin = 20f;
  [SerializeField] private float rightMargin = 20f;
  [SerializeField] private float topMargin = 20f;
  [SerializeField] private float bottomMargin = 20f;

  [Header("动态缩放参数")] [SerializeField] private float animationSpeed = 1f;
  [SerializeField] private float maxExtraMargin = 40f;

  private RectTransform rectTransform;
  private float time;

  private void Awake()
  {
    rectTransform = GetComponent<RectTransform>();

    // 确保锚点为矩形模式（这里演示将锚点设为父级全范围拉伸）
    // 注意：如果已经在 Inspector 中手动设置好，可以注释掉下面两行
    rectTransform.anchorMin = Vector2.zero; // 左下角锚点 (0,0)
    rectTransform.anchorMax = Vector2.one; // 右上角锚点 (1,1)

    // 轴心点保持中心（通常不需要修改）
    rectTransform.pivot = new Vector2(0.5f, 0.5f);
  }

  private void Start()
  {
    // 示例1：根据公开变量设置固定边距
    SetMargins(leftMargin, rightMargin, topMargin, bottomMargin);

    // 输出初始偏移用于观察
    Debug.Log($"offsetMin = {rectTransform.offsetMin} , offsetMax = {rectTransform.offsetMax}");
    // 例如输出: offsetMin = (50.0, 50.0) , offsetMax = (-50.0, -50.0)
  }

  private void Update()
  {
    // 示例2：动态改变右边距和上边距（呼吸灯效果）
    if (animationSpeed > 0)
    {
      time += Time.deltaTime * animationSpeed;
      float extra = Mathf.Sin(time) * maxExtraMargin;

      // 只修改右边距和上边距
      Vector2 newOffsetMax = rectTransform.offsetMax;
      newOffsetMax.x = -rightMargin - extra; // 右边距增加（负值减小 → 矩形变窄）
      newOffsetMax.y = -topMargin - extra; // 上边距增加
      rectTransform.offsetMax = newOffsetMax;
    }
  }

  /// <summary>
  /// 设置四个方向的边距（单位：像素）
  /// </summary>
  /// <param name="left">左边距</param>
  /// <param name="right">右边距</param>
  /// <param name="top">上边距</param>
  /// <param name="bottom">下边距</param>
  private void SetMargins(float left, float right, float top, float bottom)
  {
    // offsetMin: 控制左下角相对于锚点左下角的偏移 → 影响 left 和 bottom
    rectTransform.offsetMin = new Vector2(left, bottom);

    // offsetMax: 控制右上角相对于锚点右上角的偏移 → 影响 right 和 top
    // 注意：正值会向外扩展，负值向内收缩。通常我们想让元素距离右边界 right 像素，所以传入 -right
    rectTransform.offsetMax = new Vector2(-right, -top);
  }

  /// <summary>
  /// 获取当前边距（从 offsetMin/offsetMax 反算）
  /// </summary>
  public (float left, float right, float top, float bottom) GetCurrentMargins()
  {
    float left = rectTransform.offsetMin.x;
    float bottom = rectTransform.offsetMin.y;
    float right = -rectTransform.offsetMax.x;
    float top = -rectTransform.offsetMax.y;
    return (left, right, top, bottom);
  }

  // 可选：在 Inspector 面板上显示当前边距
  private void OnValidate()
  {
    if (rectTransform != null)
    {
      // SetMargins(leftMargin, rightMargin, topMargin, bottomMargin);
    }
  }
}