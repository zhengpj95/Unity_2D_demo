using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIRoot : MonoBehaviour
{
  public Text scoreText;

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

  private void Start()
  {
    UpdateScore();
  }

  public void UpdateScore(int score = 0)
  {
    scoreText.text = "FRUITS: " + score;
  }
}