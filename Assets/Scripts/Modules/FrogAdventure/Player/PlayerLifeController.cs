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
      EventBus.On(EventDefine.FROG_PLAYER_REVIVE, Revive, this);
    }

    private void OnDestroy()
    {
      EventBus.Off(EventDefine.FROG_PLAYER_REVIVE, Revive, this);
    }

    private void Revive(EventContext context)
    {
      Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);
    }
  }
}
