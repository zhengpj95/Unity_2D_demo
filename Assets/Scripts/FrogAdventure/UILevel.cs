using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UILevel : MonoBehaviour
{
  public Text levelText;

  private void Start()
  {
    SetLevelText();
  }

  private void SetLevelText()
  {
    levelText.text = "关卡：" + SceneManager.GetActiveScene().buildIndex;
  }
}