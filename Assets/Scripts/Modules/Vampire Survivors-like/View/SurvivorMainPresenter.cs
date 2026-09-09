using UnityEngine;

public class SurvivorMainPresenter : BasePresenter
{
  public override string PrefabPath => "Prefabs/SurvivorMain";

  private SurvivorMainView _view;

  public override void OnInit(UIView view)
  {
    base.OnInit(view);
    _view = view as SurvivorMainView;
  }

  /// <summary>只展示 SurvivorModel 快照，不持有游戏运行时数据。</summary>
  public void Refresh(SurvivorModel model)
  {
    if (_view == null || model == null)
      return;

    UpdateHp(model.CurrentHealth, model.MaxHealth);
    UpdateExp(model.CurrentExp, GetRequiredExp(model.Level), model.Level);
    UpdateEnemyKillCount(model.KillCount);
    UpdateInventory(model.GemCount, model.CoinCount);
  }

  private void UpdateHp(int currentHealth, int maxHealth)
  {
    if (_view.hpSlider != null)
    {
      _view.hpSlider.maxValue = maxHealth;
      _view.hpSlider.value = currentHealth;
    }

    if (_view.hpValueText != null)
      _view.hpValueText.text = $"{Mathf.Max(0, currentHealth)} / {maxHealth}";
  }

  private void UpdateExp(int currentExp, int requiredExp, int level)
  {
    if (_view.expSlider != null)
      _view.expSlider.value = requiredExp <= 0 ? 0f : Mathf.Clamp01((float)currentExp / requiredExp);

    if (_view.expLevelTxt != null)
      _view.expLevelTxt.text = $"Lv.{level}";
  }

  private void UpdateEnemyKillCount(int killCount)
  {
    if (_view.killCountText != null)
      _view.killCountText.text = killCount.ToString();
  }

  private void UpdateInventory(int gemCount, int coinCount)
  {
    if (_view.gemCountText != null)
      _view.gemCountText.text = gemCount.ToString();

    if (_view.coinCountText != null)
      _view.coinCountText.text = coinCount.ToString();
  }

  private static int GetRequiredExp(int level)
  {
    const int baseValue = 20;
    const int growth = 5;
    return baseValue * level + growth * level * level;
  }
}
