using System;
using UnityEngine;

public class PlayerLifeController : MonoBehaviour
{
  public GameObject playerPrefab;
  public Transform spawnPoint;

  private void Awake()
  {
    DontDestroyOnLoad(gameObject);
  }

  private void Start()
  {
    EventBus.AddListener("PLAYER_REVIVE", Revive);
  }

  private void Revive()
  {
    Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);
  }
}