using System;
using UnityEngine;

namespace FrogAdventure
{

  public class PlayerLifeController : MonoBehaviour
  {
    public GameObject playerPrefab;
    public Transform spawnPoint;

    private void Start()
    {
      EventBus.On("PLAYER_REVIVE", Revive, this);
    }

    private void OnDestroy()
    {
      EventBus.Off("PLAYER_REVIVE", Revive, this);
    }

    private void Revive()
    {
      Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);
    }
  }
}
