using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Inventory : MonoBehaviour
{
  public UI_Slot[] Slots;

  void Start()
  {
    UpdateSlot();
  }

  public void UpdateSlot()
  {
    for (int i = 0; i < Slots.Length; i++)
    {
      switch (i)
      {
        case 0:
          Slots[i].UpdateCount(DropItemManager.Instance.GemCount);
          break;
        case 1:
          Slots[i].UpdateCount(DropItemManager.Instance.CoinCount);
          break;
      }
    }
  }
}
