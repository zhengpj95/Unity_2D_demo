using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VSUIManager : SingletonMono<VSUIManager>
{
  [SerializeField] private Slider uiHp;
  [SerializeField] private Image rectBg;
  [SerializeField] private Transform skillSelectPanel;

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
}
