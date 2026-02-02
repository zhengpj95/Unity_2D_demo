using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/**
 * 进度条UI组件，是底图+进度条图片+tmp文本
 */
public class UIProgressBar : MonoBehaviour
{
  [Header("References")]
  [SerializeField] private RectTransform fill;    // imgProgress
  [SerializeField] private TMP_Text label;        // tmpTxt

  [Header("Settings")]
  [Range(0f, 1f)]
  [SerializeField] private float progress = 1f;   // 默认满格显示

  private float _originalWidth;

  private void Start()
  {
    if (fill != null)
    {
      _originalWidth = fill.sizeDelta.x;
    }
    UpdateFill();
  }

  /// <summary>
  /// 设置进度(0~1)
  /// </summary>
  public void SetProgress(float value)
  {
    progress = Mathf.Clamp01(value);
    UpdateFill();
  }

  /// <summary>
  /// 设置显示文本（任意字符串）
  /// </summary>
  public void SetText(string text)
  {
    if (label != null)
      label.text = text;
  }

  private void UpdateFill()
  {
    if (fill == null)
    {
      return;
    }

    Vector2 size = fill.sizeDelta;
    size.x = _originalWidth * progress;
    fill.sizeDelta = size;
  }
}
