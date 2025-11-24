using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FruitCollectManager
{
  private static FruitCollectManager _instance;
  public static FruitCollectManager Instance => _instance ??= new FruitCollectManager();

  public int Score = 0;

  public void ChangeScore(int scoreValue)
  {
    Score += scoreValue;
    EventBus.Dispatch("update_score");
  }
}