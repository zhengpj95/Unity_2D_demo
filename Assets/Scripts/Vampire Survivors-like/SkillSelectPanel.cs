using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillSelectPanel : MonoBehaviour
{
  public Button skill0;
  public Button skill1;
  public Button skill2;
  public Text timerText; // UI 文本显示倒计时
  public WeaponSO[] weapons; // 武器SO

  private float countdownDuration = 10f;
  private float remainingTime;
  private bool hidden = false;

  void OnEnable()
  {
    remainingTime = countdownDuration;
    hidden = false;

    skill0.onClick.AddListener(() => SelectSkill(0));
    skill1.onClick.AddListener(() => SelectSkill(1));
    skill2.onClick.AddListener(() => SelectSkill(2));
  }

  void OnDisable()
  {
    skill0.onClick.RemoveAllListeners();
    skill1.onClick.RemoveAllListeners();
    skill2.onClick.RemoveAllListeners();
  }

  void Start()
  {
    for (int i = 0; i < 3; i++)
    {
      var skillItem = gameObject.transform.Find($"SkillItem{i}");
      if (skillItem && weapons[i])
      {
        Debug.Log(skillItem);
        Debug.Log(weapons[i]);
        UpdateSkillItem(skillItem, weapons[i]);
      }
    }
  }

  void UpdateSkillItem(Transform skillItem, WeaponSO weapon)
  {
    var icon = skillItem.Find("Image");
    if (icon != null)
    {
      icon.GetComponent<Image>().sprite = weapon.icon;
    }
    var text = skillItem.Find("Text");
    if (text != null)
    {
      text.GetComponent<TextMeshProUGUI>().text = weapon.weaponName;
    }
  }

  void SelectSkill(int skillIndex)
  {
    switch (skillIndex)
    {
      case 0:
        Debug.Log("选择了技能0");
        WeaponManager.Instance.AddOrUpgrade(weapons[0]);
        break;
      case 1:
        Debug.Log("选择了技能1");
        WeaponManager.Instance.AddOrUpgrade(weapons[1]);
        break;
      case 2:
        Debug.Log("选择了技能2");
        WeaponManager.Instance.AddOrUpgrade(weapons[2]);
        break;
      default:
        Debug.Log("未知技能索引");
        break;
    }
    HideUI();
  }

  void Update()
  {
    if (!hidden && remainingTime > 0)
    {
      remainingTime -= Time.deltaTime;
      // timerText.text = "倒计时关闭：" + Mathf.Max(0, remainingTime).ToString("F0"); // 字符串拼接
      // timerText.text = string.Format("倒计时关闭：{0:F0}秒", Mathf.Max(0, remainingTime)); // string.Format()
      timerText.text = $"倒计时关闭：{Mathf.Max(0, remainingTime):F0}秒"; // 字符串插值
    }
    else if (!hidden && remainingTime <= 0)
    {
      SelectSkill(0);
    }
  }

  void HideUI()
  {
    hidden = true;
    VSUIManager.Instance.ShowSkillSelectPanel(false);
  }
}
