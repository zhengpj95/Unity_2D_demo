using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class VSUIManager : SingletonMono<VSUIManager>
{
  [SerializeField] private Slider uiHp;
  [SerializeField] private Image rectBg;
  [SerializeField] private Transform skillSelectPanel;
  [SerializeField] private UI_Inventory uiInventory;
  [SerializeField] private Transform uiEnemyCount;
  [SerializeField] private UI_EXP uI_EXP;

  public void ShowRectBg(bool isVisible = true)
  {
    if (rectBg)
    {
      rectBg.gameObject.SetActive(isVisible);
    }
  }

  public void UpdateHp(int currentHealth, int maxHealth)
  {
    if (uiHp == null) return;

    uiHp.maxValue = maxHealth;
    uiHp.value = currentHealth;

    var text = uiHp.transform.Find("HpValue")?.GetComponent<Text>();
    if (text != null)
    {
      text.text = Mathf.Max(0, currentHealth) + " / " + maxHealth;
    }
  }

  public void ShowSkillSelectPanel(bool isVisible = true)
  {
    Time.timeScale = isVisible ? 0 : 1;
    ShowRectBg(isVisible);
    if (skillSelectPanel)
    {
      skillSelectPanel.gameObject.SetActive(isVisible);
    }
  }

  public void UpdateInventory()
  {
    if (uiInventory)
    {
      uiInventory.UpdateSlot();
    }
  }

  public void UpdateEnemyKillCount()
  {
    if (uiEnemyCount)
    {
      TMP_Text killCount = uiEnemyCount.Find("KillCount")?.GetComponent<TMP_Text>();
      if (killCount)
      {
        killCount.text = EnemySpawnManager.Instance.KillEnemyCount.ToString();
      }
    }
  }


  #region ==== exp ====

  private int _curLevel = 0;
  private int _curExp = 0;

  private int GetNextLevelExp()
  {
    int baseVal = 20, grow = 5;
    int nextLevel = _curLevel + 1;
    int exp = baseVal * nextLevel + grow * nextLevel * nextLevel;
    return exp;
  }

  // 临时使用，添加经验
  public void UpdateExp(int addExp)
  {
    _curExp += addExp;
    int nextLevelExp = GetNextLevelExp();
    if (_curExp >= nextLevelExp)
    {
      _curExp = 0;
      _curLevel++;
      ShowSkillSelectPanel(true);
      UpdateExpBar(_curExp, nextLevelExp, _curLevel - 1);
    }
    else
    {
      UpdateExpBar(_curExp, nextLevelExp, _curLevel);
    }
    // Debug.Log($"AddExp: {addExp} CurExp: {_curExp} NextLevelExp: {nextLevelExp} CurLevel: {_curLevel}");
  }

  public void UpdateExpBar(int currentExp, int expToNextLevel, int currentLevel)
  {
    if (uI_EXP)
    {
      uI_EXP.UpdateExp(currentExp, expToNextLevel);
      uI_EXP.UpdateLevel(currentLevel);
    }
  }
  #endregion
}
