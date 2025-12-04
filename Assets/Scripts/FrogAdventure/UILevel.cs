using UnityEngine;
using UnityEngine.UI;

public class UILevel : MonoBehaviour
{
  public Text levelText;
  public Image[] img;

  private void Start()
  {
    SetLevelText();
    EventBus.AddListener("UPDATE_LEVEL", SetLevelText);
    EventBus.AddListener("UPDATE_HP", SetHeart);
  }

  private void SetLevelText()
  {
    levelText.text = "关卡：" + GameController.Instance.Level;
  }

  private void SetHeart()
  {
    int hp = GameController.Instance.MaxHp;
    for (int i = 0; i < img.Length; i++)
    {
      if (i < hp)
      {
        img[i].enabled = true;
      }
      else
      {
        img[i].enabled = false;
      }
    }
  }
}