using UnityEngine;
using UnityEngine.UI;

public class UILevel : MonoBehaviour
{
  public Text levelText;

  private void Start()
  {
    SetLevelText();
    EventBus.AddListener("UPDATE_LEVEL", SetLevelText);
  }

  private void SetLevelText()
  {
    levelText.text = "关卡：" + GameController.Instance.Level;
  }
}