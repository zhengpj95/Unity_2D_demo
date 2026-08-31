using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FrogAdventure
{

  public class UIFruit : MonoBehaviour
  {
    private void Start()
    {
      UpdateScore();
      EventBus.On("update_score", UpdateScore, this); // 自定义事件监听
    }

    private void OnDestroy()
    {
      EventBus.Off("update_score", UpdateScore, this);
    }

    private void UpdateScore()
    {
      var text = gameObject?.GetComponent<Text>();
      if (text)
      {
        text.text = "FRUITS: " + FruitCollectManager.Instance.Score;
      }
    }
  }
}
