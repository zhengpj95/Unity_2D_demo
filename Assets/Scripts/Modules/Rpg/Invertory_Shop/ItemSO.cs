using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rpg
{
  [CreateAssetMenu(fileName = "NewItem", menuName = "Game Data/RPG/Inventory/Item")]
  public class ItemSO : ScriptableObject
  {
    public int id;
    public string itemName;
    public Sprite icon;
    [TextArea] public string itemDescription;
  }
}
