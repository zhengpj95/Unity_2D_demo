using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_EXP : MonoBehaviour
{
  [SerializeField] private Slider slider;
  [SerializeField] private Text LevelTxt;

  public void UpdateExp(int currentExp, int expToNextLevel)
  {
    Debug.Log($"UpdateExp: {currentExp} / {expToNextLevel}");
    slider.value = Mathf.Min(1, (float)currentExp / expToNextLevel);
  }

  public void UpdateLevel(int currentLevel)
  {
    LevelTxt.text = $"Lv.{currentLevel}";
  }
}
