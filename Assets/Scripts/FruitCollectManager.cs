using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FruitCollectManager : MonoBehaviour
{
  public int score = 0;
  public UIRoot uiRoot;

  private void Awake()
  {
    DontDestroyOnLoad(gameObject);
    SceneManager.sceneLoaded += OnSceneLoaded;
  }

  private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
  {
    if (scene.name == "EndGame")
    {
      Destroy(gameObject);
    }
  }

  public void ChangeScore(int scoreValue)
  {
    score += scoreValue;
    uiRoot.UpdateScore(score);
  }
}