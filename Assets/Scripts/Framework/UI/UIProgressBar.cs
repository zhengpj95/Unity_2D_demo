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
  Value,
  Percent,
  Custom
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
  private float _progress = 0f; // 进度(0~1)

  private void Start()
  {
    if (barMode == ProgressBarMode.Fill)
    {
      _fillImg = img.GetComponent<Image>();
    }
    else if (barMode == ProgressBarMode.Mask)
    {
      _mask2D = img.GetComponent<RectMask2D>();
    }
    Reset();
  }

  /// <summary>
  /// 设置进度(0~1)
  /// </summary>
  public void SetProgress(float value)
  {
    _progress = Mathf.Clamp01(value);
    RefreshView();
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
    SetProgress(0);
  }

  private void RefreshView()
  {
    if (img == null)
    {
      return;
    }

    if (barMode == ProgressBarMode.Fill)
    {
      _fillImg.fillAmount = _progress;
    }
    else if (barMode == ProgressBarMode.Mask)
    {
      Vector4 padding = _mask2D.padding;
      padding.z = img.rect.width * (1 - _progress);
      _mask2D.padding = padding;
    }
  }
}
