using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>显示一个 ListTab 页签单元格的名称、图标和选中状态。</summary>
public sealed class ListTabItemView : MonoBehaviour
{
  [SerializeField, Tooltip("页签名称文本。")]
  private TMP_Text nameText;
  [SerializeField, Tooltip("可选页签图标。")]
  private Image iconImage;
  [SerializeField, Tooltip("选中时显示的背景或装饰节点。")]
  private GameObject selectedIndicator;

  /// <summary>根据数据和已加载图标刷新单元格，虚拟列表复用单元格时也会重新调用。</summary>
  /// <param name="data">当前绑定的页签数据。</param>
  /// <param name="icon">根据 IconPath 加载的图标；路径为空或加载失败时为 null。</param>
  /// <param name="isSelected">当前页签是否选中。</param>
  public void Bind(ListTabItemData data, Sprite icon, bool isSelected)
  {
    if (data == null) return;

    if (nameText != null) nameText.text = data.Name;
    if (iconImage != null)
    {
      iconImage.sprite = icon;
      iconImage.enabled = icon != null;
    }
    if (selectedIndicator != null) selectedIndicator.SetActive(isSelected);
  }
}
