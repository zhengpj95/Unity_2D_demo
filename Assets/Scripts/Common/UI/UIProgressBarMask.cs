using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIProgressBarMask : MonoBehaviour
{
  [Header("References")]
  [SerializeField] private RectTransform maskRect;
  [SerializeField] private RectTransform imgExpPro;
  [SerializeField] private TMP_Text tmpTxt;

  private float _fullWidth;

  void Start()
  {
    var containerSize = maskRect.parent.GetComponent<RectTransform>().sizeDelta;
    maskRect.sizeDelta = new Vector2(containerSize.x, containerSize.y);
    imgExpPro.sizeDelta = new Vector2(containerSize.x, containerSize.y);
    _fullWidth = containerSize.x;
  }

  public void SetProgress(float progress)
  {
    progress = Mathf.Clamp01(progress);
    float width = Mathf.Min(_fullWidth, _fullWidth * progress);
    maskRect.sizeDelta = new Vector2(width, maskRect.sizeDelta.y);
  }

  public void SetText(string text)
  {
    if (tmpTxt != null)
      tmpTxt.text = text;
  }

}

