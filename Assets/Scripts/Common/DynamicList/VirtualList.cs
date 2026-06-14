using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/**
 * 虚拟列表，支持 vertical, horizontal, grid
 */
public class VirtualList : MonoBehaviour
{
  [Header("引用")] [SerializeField] private ScrollRect scrollRect;
  [SerializeField] private RectTransform content;
  [SerializeField] private VirtualListItem itemPrefab;

  [Header("设置")] [SerializeField] private ScrollType scrollType;

  [Tooltip("水平方向显示的单元格之间的间距")] [SerializeField]
  private int spaceX = 0;

  [Tooltip("垂直方向显示的单元格之间的间距")] [SerializeField]
  private int spaceY = 0;

  [Tooltip("水平方向显示的单元格数量")] [SerializeField]
  private int repeatX = 0;

  [Tooltip("垂直方向显示的单元格数量")] [SerializeField]
  private int repeatY = 0;

  private readonly List<object> dataList = new();
  private readonly Queue<VirtualListItem> pool = new();
  private readonly List<VirtualListItem> visibleItems = new();

  private int visibleCount;
  private int startIndex = -1;
  private int columns = 1;
  private int rows = 1;

  private float viewportHeight = 0;
  private float viewportWidth = 0;

  private int _extraCount = 3; // 额外缓存数量
  private float _itemHeight = 0;
  private float _itemWidth = 0;

  private void Awake()
  {
    CreateItemPrefab();
    scrollRect.onValueChanged.AddListener(OnScroll);
    scrollRect.vertical = scrollType == ScrollType.Vertical;
    scrollRect.horizontal = scrollType == ScrollType.Horizontal;
  }

  private void Start()
  {
    // Ensure layout has been calculated so viewport sizes are valid
    Canvas.ForceUpdateCanvases();

    // set content anchors/pivot once
    if (content != null)
    {
      content.anchorMin = new Vector2(0, 1);
      content.anchorMax = new Vector2(0, 1);
      content.pivot = new Vector2(0, 1);
    }

    InitRect();
  }

  private void CreateItemPrefab()
  {
    if (itemPrefab == null)
    {
      Debug.LogError("VirtualList itemPrefab is null, Please check your list.");
      return;
    }

    RectTransform rt = itemPrefab.GetComponent<RectTransform>();
    _itemHeight = rt.sizeDelta.y;
    _itemWidth = rt.sizeDelta.x;
    itemPrefab.gameObject.SetActive(false);
  }

  private void InitRect()
  {
    if (itemPrefab == null) return;

    viewportHeight = scrollRect.viewport.rect.height;
    viewportWidth = scrollRect.viewport.rect.width;

    // determine grid columns/rows based on scroll type
    if (scrollType == ScrollType.Vertical)
    {
      // Vertical: columns fixed (repeatX), rows auto-calculated
      columns = repeatX > 0 ? repeatX : 1;
      rows = Mathf.Max(1, Mathf.FloorToInt(viewportHeight / (_itemHeight + spaceY)));
    }
    else
    {
      // Horizontal: rows fixed (repeatY), columns auto-calculated
      rows = repeatY > 0 ? repeatY : 1;
      columns = Mathf.Max(1, Mathf.FloorToInt(viewportWidth / (_itemWidth + spaceX)));
    }

    if (scrollType == ScrollType.Vertical)
    {
      int visibleRows = Mathf.CeilToInt(viewportHeight / (_itemHeight + spaceY)) + _extraCount;
      visibleCount = visibleRows * columns;
    }
    else
    {
      int visibleCols = Mathf.CeilToInt(viewportWidth / (_itemWidth + spaceX)) + _extraCount;
      visibleCount = visibleCols * rows;
    }

    CreateItems();
  }

  private void CreateItems()
  {
    for (int i = 0; i < visibleCount; i++)
    {
      VirtualListItem item = Instantiate(itemPrefab, content, false);
      // ensure instantiated clones are inactive until used
      if (item != null && item.gameObject.activeSelf)
      {
        item.gameObject.SetActive(false);
      }

      visibleItems.Add(item);
    }
  }

  public void RefreshData<T>(List<T> datas)
  {
    dataList.Clear();

    foreach (var data in datas)
    {
      dataList.Add(data);
    }

    UpdateContentLayout();

    RefreshVisible(true);
  }

  private void UpdateContentLayout()
  {
    // compute content size based on grid columns/rows
    int totalItems = dataList.Count;
    int totalRows = Mathf.CeilToInt((float)totalItems / columns);
    int totalCols = Mathf.CeilToInt((float)totalItems / rows);

    if (scrollType == ScrollType.Vertical)
    {
      // 垂直方向
      float height = 0f;
      if (totalRows > 0)
      {
        height = totalRows * _itemHeight + ((totalRows - 1) * spaceY);
      }

      content.sizeDelta = new Vector2(columns * _itemWidth + ((columns - 1) * spaceX), height);
    }
    else
    {
      // 水平方向
      float width = 0f;
      if (totalCols > 0)
      {
        width = totalCols * _itemWidth + ((totalCols - 1) * spaceX);
      }

      content.sizeDelta = new Vector2(width, rows * _itemHeight + ((rows - 1) * spaceY));
    }
  }

  private void OnScroll(Vector2 value)
  {
    RefreshVisible(false);
  }

  private void RefreshVisible(bool force)
  {
    int newStartIndex = 0;
    if (scrollType == ScrollType.Vertical)
    {
      int startRow = Mathf.FloorToInt(Mathf.Abs(content.anchoredPosition.y) / (_itemHeight + spaceY));
      startRow = Mathf.Max(0, startRow);
      newStartIndex = startRow * columns;
    }
    else
    {
      int startCol = Mathf.FloorToInt(Mathf.Abs(content.anchoredPosition.x) / (_itemWidth + spaceX));
      startCol = Mathf.Max(0, startCol);
      newStartIndex = startCol * rows;
    }

    if (!force && newStartIndex == startIndex)
    {
      return;
    }

    startIndex = newStartIndex;

    for (int i = 0; i < visibleItems.Count; i++)
    {
      int dataIndex = startIndex + i;

      VirtualListItem item = visibleItems[i];
      if (item == null) continue;

      if (dataIndex >= dataList.Count)
      {
        item.gameObject.SetActive(false);
        continue;
      }

      RefreshItem(item, dataIndex);
    }
  }

  private void RefreshItem(VirtualListItem item, int dataIndex)
  {
    if (item == null) return;

    item.gameObject.SetActive(true);
    item.name = "item" + dataIndex;

    RectTransform rt = item.transform as RectTransform;
    if (rt != null)
    {
      float x, y;

      if (scrollType == ScrollType.Vertical)
      {
        // row-major: row = index / columns, col = index % columns
        int row = dataIndex / columns;
        int col = dataIndex % columns;
        x = col * (_itemWidth + spaceX);
        y = -row * (_itemHeight + spaceY);
      }
      else
      {
        // column-major: col = index / rows, row = index % rows
        int col = dataIndex / rows;
        int row = dataIndex % rows;
        x = col * (_itemWidth + spaceX);
        y = -row * (_itemHeight + spaceY);
      }

      rt.anchoredPosition = new Vector2(x, y);
    }

    item.Refresh(dataIndex, dataList[dataIndex]);
  }
}