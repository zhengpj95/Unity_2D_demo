using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "RPG/Item")]
public class ItemSO : ScriptableObject
{
  public int id;
  public string itemName;
  public Sprite icon;
  [TextArea] public string itemDescription;
}
