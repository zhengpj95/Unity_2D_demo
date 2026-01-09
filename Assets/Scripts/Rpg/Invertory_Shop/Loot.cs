using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using TMPro;

public class Loot : MonoBehaviour
{
  public ItemSO itemSO;
  public SpriteRenderer sr;
  public TMP_Text text;
  public Animator animator;

  public int quantity;

  // 仅限编辑器运行，不要在里面写游戏逻辑（玩家永远看不到）。
  private void OnValidate()
  {
    if (itemSO != null && sr != null)
    {
      sr.sprite = itemSO.icon;
      // text.text = quantity.ToString();
      this.name = itemSO.itemName;
    }
  }

  void OnTriggerEnter2D(Collider2D collision)
  {
    if (collision.gameObject.CompareTag("Player"))
    {
      animator.Play("LootPickup");
    }
  }

  // Animation Event
  public void OnLootPickupComplete()
  {
    // 播放动画后，销毁 loot  GameObject
    Destroy(gameObject);
  }
}
