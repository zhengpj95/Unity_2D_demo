using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIFruit : MonoBehaviour
{
  private void Start()
  {
    UpdateScore();
    EventBus.AddListener("update_score", UpdateScore); // 自定义事件监听
  }

  private void OnDestroy()
  {
    EventBus.RemoveListener("update_score", UpdateScore);
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