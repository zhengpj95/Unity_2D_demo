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

  [Tooltip("The horizontal spacing between cells in pixels")] [SerializeField]
  private int spaceX = 0;

  [Tooltip("The vertical spacing between cells in pixels")] [SerializeField]
  private int spaceY = 0;

  // [Tooltip("The number of cells displayed horizontally")] [SerializeField]
  // private int repeatX = 0;
  //
  // [Tooltip("The number of cells displayed vertically")] [SerializeField]
  // private int repeatY = 0;

  private readonly List<object> dataList = new();
  private readonly Queue<VirtualListItem> pool = new();
  private readonly List<VirtualListItem> visibleItems = new();

  private int visibleCount;
  private int startIndex = -1;

  private float viewportHeight = 0;
  private float viewportWidth = 0;

  private int _extraCount = 3; // 额外缓存数量
  private float _itemHeight = 0;
  private float _itemWidth = 0;

  private void Awake()
  {
    CreateItemPrefab();
    InitRect();
    scrollRect.onValueChanged.AddListener(OnScroll);
    scrollRect.vertical = scrollType == ScrollType.Vertical;
    scrollRect.horizontal = scrollType == ScrollType.Horizontal;
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
    visibleCount = scrollType == ScrollType.Vertical
      ? Mathf.CeilToInt(viewportHeight / _itemHeight) + _extraCount
      : Mathf.CeilToInt(viewportWidth / _itemWidth) + _extraCount;
    CreateItems();
    Debug.Log("InitRect: " + viewportHeight + ", " + viewportWidth + ", " + visibleCount + ", " + scrollType);
  }

  private void CreateItems()
  {
    for (int i = 0; i < visibleCount; i++)
    {
      VirtualListItem item = Instantiate(itemPrefab, content);
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
    if (scrollType == ScrollType.Vertical)
    {
      float height = dataList.Count * _itemHeight + ((dataList.Count - 1) * spaceY);
      content.sizeDelta = new Vector2(content.sizeDelta.x, height);
    }
    else if (scrollType == ScrollType.Horizontal)
    {
      float width = dataList.Count * _itemWidth + ((dataList.Count - 1) * spaceX);
      content.sizeDelta = new Vector2(width, content.sizeDelta.y);
    }

    RectTransform rt = content.GetComponent<RectTransform>();
    rt.anchorMin = new Vector2(0, 1);
    rt.anchorMax = new Vector2(0, 1);
    rt.pivot = new Vector2(0, 1);
  }

  private void OnScroll(Vector2 value)
  {
    RefreshVisible(false);
  }

  private void RefreshVisible(bool force)
  {
    int newStartIndex = scrollType == ScrollType.Vertical
      ? Mathf.FloorToInt(Math.Abs(content.anchoredPosition.y) / (_itemHeight + spaceY))
      : Mathf.FloorToInt(Mathf.Abs(content.anchoredPosition.x) / (_itemWidth + spaceX));
    newStartIndex = Mathf.Max(0, newStartIndex);

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

      item.gameObject.SetActive(true);
      item.name = "item" + dataIndex;

      RectTransform rt = item.transform as RectTransform;
      if (scrollType == ScrollType.Vertical)
      {
        rt.anchoredPosition = new Vector2(0, -dataIndex * _itemHeight - (spaceY * dataIndex));
      }
      else if (scrollType == ScrollType.Horizontal)
      {
        rt.anchoredPosition = new Vector2(dataIndex * _itemWidth + (spaceX * dataIndex), 0);
      }

      item.Refresh(dataIndex, dataList[dataIndex]);
    }
  }
}