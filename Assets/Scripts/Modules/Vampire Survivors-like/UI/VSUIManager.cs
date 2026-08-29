using UnityEngine;

namespace VampireSurvivorsLike
{
  public class VSUIManager : SingletonMono<VSUIManager>
  {
    private int _currentHealth;
    private int _maxHealth;
    private bool _hasHealthState;

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

    public void UpdateInventory()
    {
      SurvivorModule survivorModule = GetSurvivorModule();
      SurvivorMainPresenter presenter = survivorModule?.GetPresenter<SurvivorMainPresenter>();
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
      SurvivorModule survivorModule = GetSurvivorModule();
      SurvivorMainPresenter presenter = survivorModule?.GetPresenter<SurvivorMainPresenter>();
      if (presenter == null)
      {
        Debug.LogWarning("[VSUIManager] SurvivorMainPresenter is not open.");
        return;
      }

      presenter.UpdateExp(addExp, () => survivorModule.OpenSkillSelectPanel());
    }

    private static SurvivorModule GetSurvivorModule()
    {
      return ModuleManager.Instance.GetModule<SurvivorModule>(ModuleName.Survivor);
    }

    private static SurvivorMainPresenter GetSurvivorMainPresenter()
    {
      return GetSurvivorModule()?.GetPresenter<SurvivorMainPresenter>();
    }
  }
}
