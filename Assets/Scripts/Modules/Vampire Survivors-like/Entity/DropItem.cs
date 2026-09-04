using UnityEngine;

namespace VampireSurvivorsLike {

  public class DropItem : MonoBehaviour, IPoolable
  {
    [SerializeField] private int score = 1;
    [SerializeField] private DropItemType dropItemType;

    private bool collected;

    private void OnTriggerEnter2D(Collider2D other)
    {
      if (collected || !other.CompareTag("Player"))
        return;

      collected = true;
      DropItemManager manager = DropItemManager.Instance;
      manager.AddScore(score);
      manager.AddDropItem(dropItemType, 1);

      // 只有 Gem 提供经验；Coin 仅计入背包数量。
      if (dropItemType == DropItemType.Gem)
        manager.AddExperience(score);

      manager.RecycleDropItem(gameObject);
    }

    public void OnAlloc()
    {
      collected = false;
    }

    public void OnFree()
    {
      collected = false;
    }
  }

}
