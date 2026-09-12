using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum ProgressBarMode
{
  Fill,
  Mask
}
public enum ProgressBarTextType
{
  None,
  Value,
  Percent
}

/**
 * 进度条UI组件，是底图+进度条图片+tmp文本
 */
public class UIProgressBar : MonoBehaviour
{
  [Header("References")]
  [SerializeField] private ProgressBarMode barMode = ProgressBarMode.Fill;
  [SerializeField] private RectTransform img; // imgProgress
  [SerializeField] private TMP_Text tmpTxt;   // tmpTxt

  private Image _fillImg;
  private RectMask2D _mask2D;
  private float _normalizedValue = 0f; // 归一化的进度值 (0 到 1)

  // 初始化只刷新已有值，避免首次激活时覆盖调用方预先设置的进度。
  private void Awake()
  {
    SetValue(_normalizedValue);
  }

  /// <summary>
  /// 设置进度值
  /// </summary>
  public void SetValue(float value, ProgressBarTextType textType = ProgressBarTextType.Percent)
  {
    _normalizedValue = Mathf.Clamp01(value);
    RefreshView();
    if (textType == ProgressBarTextType.Value)
    {
      SetText($"{value}");
    }
    else if (textType == ProgressBarTextType.Percent)
    {
      SetText($"{_normalizedValue * 100:F2}%");
    }
    else if (textType == ProgressBarTextType.None)
    {
      SetText("");
    }
  }
  public void SetValue(float value, float maxValue, ProgressBarTextType textType = ProgressBarTextType.Value)
  {
    _normalizedValue = Mathf.Clamp01(value / maxValue);
    RefreshView();
    if (textType == ProgressBarTextType.Value)
    {
      SetText($"{value}/{maxValue}");
    }
    else if (textType == ProgressBarTextType.Percent)
    {
      SetText($"{_normalizedValue * 100:F2}%");
    }
    else if (textType == ProgressBarTextType.None)
    {
      SetText("");
    }
  }
  public void SetValue(float value, float minValue, float maxValue, ProgressBarTextType textType = ProgressBarTextType.Value)
  {
    _normalizedValue = Mathf.Clamp01((value - minValue) / (maxValue - minValue));
    RefreshView();
    if (textType == ProgressBarTextType.Value)
    {
      SetText($"{value}/{maxValue}");
    }
    else if (textType == ProgressBarTextType.Percent)
    {
      SetText($"{_normalizedValue * 100:F2}%");
    }
    else if (textType == ProgressBarTextType.None)
    {
      SetText("");
    }
  }

  /// <summary>
  /// 设置显示文本（任意字符串）
  /// </summary>
  public void SetText(string text)
  {
    if (tmpTxt != null)
    {
      tmpTxt.SetText(text);
    }
  }

  private void Reset()
  {
    SetText("");
    SetValue(0);
  }

  private void RefreshView()
  {
    if (img == null)
    {
      return;
    }

    if (barMode == ProgressBarMode.Fill)
    {
      // 允许调用方在进度条首次激活前设置 0%，引用按需缓存。
      if (_fillImg == null) _fillImg = img.GetComponent<Image>();
      if (_fillImg == null) return;
      _fillImg.fillAmount = _normalizedValue;
    }
    else if (barMode == ProgressBarMode.Mask)
    {
      if (_mask2D == null) _mask2D = img.GetComponent<RectMask2D>();
      if (_mask2D == null) return;
      Vector4 padding = _mask2D.padding;
      padding.z = img.rect.width * (1 - _normalizedValue);
      _mask2D.padding = padding;
    }
  }
}
