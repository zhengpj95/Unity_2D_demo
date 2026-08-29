using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VampireSurvivorsLike
{
  public class VSUIManager : SingletonMono<VSUIManager>
  {
    [SerializeField] private Image rectBg;
    [SerializeField] private Transform skillSelectPanel;

    private int _currentHealth;
    private int _maxHealth;
    private bool _hasHealthState;

    public void ShowRectBg(bool isVisible = true)
    {
      if (rectBg)
        rectBg.gameObject.SetActive(isVisible);
    }

    public void UpdateHp(int currentHealth, int maxHealth)
    {
      _currentHealth = currentHealth;
      _maxHealth = maxHealth;
      _hasHealthState = true;

      SurvivorMainPresenter presenter = GetSurvivorMainPresenter();
      if (presenter == null)
        return;

      presenter.UpdateHp(currentHealth, maxHealth);
    }

    public bool TryGetHp(out int currentHealth, out int maxHealth)
    {
      currentHealth = _currentHealth;
      maxHealth = _maxHealth;
      return _hasHealthState;
    }

    public void ShowSkillSelectPanel(bool isVisible = true)
    {
      Time.timeScale = isVisible ? 0 : 1;
      ShowRectBg(isVisible);
      if (skillSelectPanel)
        skillSelectPanel.gameObject.SetActive(isVisible);
    }

    public void UpdateInventory()
    {
      SurvivorMainPresenter presenter = GetSurvivorMainPresenter();
      if (presenter == null)
        return;

      presenter.UpdateInventory(DropItemManager.Instance.GemCount, DropItemManager.Instance.CoinCount);
    }

    public void UpdateEnemyKillCount()
    {
      SurvivorMainPresenter presenter = GetSurvivorMainPresenter();
      if (presenter == null)
        return;

      presenter.UpdateEnemyKillCount(EnemySpawnManager.Instance.KillEnemyCount);
    }

    public void UpdateExp(int addExp)
    {
      SurvivorMainPresenter presenter = GetSurvivorMainPresenter();
      if (presenter == null)
      {
        Debug.LogWarning("[VSUIManager] SurvivorMainPresenter is not open.");
        return;
      }

      presenter.UpdateExp(addExp, () => ShowSkillSelectPanel(true));
    }

    private static SurvivorMainPresenter GetSurvivorMainPresenter()
    {
      SurvivorModule survivorModule = ModuleManager.Instance.GetModule<SurvivorModule>(ModuleName.Survivor);
      return survivorModule?.GetPresenter<SurvivorMainPresenter>();
    }
  }
}
