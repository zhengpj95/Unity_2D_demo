using UnityEngine;
using UnityEngine.UI;

namespace FrogAdventure
{

  public class UILevel : MonoBehaviour
  {
    public Text levelText;
    public Image[] img;

    private void Start()
    {
      RefreshLevelText();
      EventBus.On(EventDefine.FROG_LEVEL_CHANGED, SetLevelText, this);
      EventBus.On(EventDefine.FROG_HEALTH_CHANGED, SetHeart, this);
    }

    private void SetLevelText(EventContext context)
    {
      RefreshLevelText();
    }

    private void RefreshLevelText()
    {
      levelText.text = "关卡：" + GameController.Instance.Level;
    }

    private void SetHeart(EventContext context)
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
}
