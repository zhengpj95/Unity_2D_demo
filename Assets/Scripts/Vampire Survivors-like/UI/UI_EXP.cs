using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_EXP : MonoBehaviour
{
  [SerializeField] private Slider slider;
  [SerializeField] private Text LevelTxt;

  private int _curLevel;

  public void UpdateExp(int currentExp, int expToNextLevel)
  {
    slider.value = Mathf.Max(1, (float)currentExp / expToNextLevel);
  }

  public void UpdateLevel(int currentLevel)
  {
    _curLevel = currentLevel;
    LevelTxt.text = $"Lv.{currentLevel}";
  }
}
