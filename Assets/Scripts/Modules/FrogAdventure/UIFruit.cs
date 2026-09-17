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
      RefreshScore();
      EventBus.On(EventDefine.FROG_SCORE_CHANGED, UpdateScore, this);
    }

    private void OnDestroy()
    {
      EventBus.Off(EventDefine.FROG_SCORE_CHANGED, UpdateScore, this);
    }

    private void UpdateScore(EventContext context)
    {
      RefreshScore();
    }

    private void RefreshScore()
    {
      var text = gameObject?.GetComponent<Text>();
      if (text)
      {
        text.text = "FRUITS: " + FruitCollectManager.Instance.Score;
      }
    }
  }
}
