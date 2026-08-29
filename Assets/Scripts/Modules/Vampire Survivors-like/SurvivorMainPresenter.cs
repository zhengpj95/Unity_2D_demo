using UnityEngine;
using VampireSurvivorsLike;

public class SurvivorMainPresenter : BasePresenter
{
  private SurvivorMainView _view;
  private int _currentLevel;
  private int _currentExp;

  public override void OnInit(UIView view)
  {
    base.OnInit(view);
    _view = view as SurvivorMainView;
  }

  public override void OnOpen(object args = null)
  {
    base.OnOpen(args);

    UpdateExpView(GetNextLevelExp());

    if (VSUIManager.Instance.TryGetHp(out int currentHealth, out int maxHealth))
      UpdateHp(currentHealth, maxHealth);
  }

  public override void OnClose()
  {
    base.OnClose();
  }

  public void UpdateExp(int addExp, System.Action onLevelUp)
  {
    _currentExp += addExp;
    int nextLevelExp = GetNextLevelExp();

    if (_currentExp >= nextLevelExp)
    {
      UpdateExpView(nextLevelExp);
      _currentExp = 0;
      _currentLevel++;
      UpdateExpView(nextLevelExp);
      onLevelUp?.Invoke();
      return;
    }

    UpdateExpView(nextLevelExp);
  }

  public void UpdateHp(int currentHealth, int maxHealth)
  {
    if (_view == null)
    {
      Debug.LogWarning("[SurvivorMainPresenter] SurvivorMainView is not initialized.");
      return;
    }

    if (_view.hpSlider != null)
    {
      _view.hpSlider.maxValue = maxHealth;
      _view.hpSlider.value = currentHealth;
    }

    if (_view.hpValueText != null)
      _view.hpValueText.text = $"{Mathf.Max(0, currentHealth)} / {maxHealth}";
  }

  private int GetNextLevelExp()
  {
    const int baseValue = 20;
    const int growth = 5;
    int nextLevel = _currentLevel + 1;
    return baseValue * nextLevel + growth * nextLevel * nextLevel;
  }

  private void UpdateExpView(int nextLevelExp)
  {
    if (_view == null)
    {
      Debug.LogWarning("[SurvivorMainPresenter] SurvivorMainView is not initialized.");
      return;
    }

    if (_view.expSlider != null)
      _view.expSlider.value = Mathf.Min(1f, (float)_currentExp / nextLevelExp);

    if (_view.expLevelTxt != null)
      _view.expLevelTxt.text = $"Lv.{_currentLevel}";
  }
}
