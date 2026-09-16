using UnityEngine;

/// <summary>
/// 虚拟列表单元格的渲染与点击回调参数。
/// </summary>
/// <remarks>
/// 点击回调只传递数据与索引，<see cref="itemTransform"/> 可能为 null。
/// </remarks>
public struct VirtualListRenderInfo
{
  public int index;
  public object data;
  public int selectedIndex;
  public RectTransform itemTransform;
}
