using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UILevel : MonoBehaviour
{
  public Text levelText;
  public int level;

  private void Awake()
  {
    DontDestroyOnLoad(gameObject.transform.parent.gameObject);
  }

  private void Start()
  {
    SetLevelText();
  }

  private void SetLevelText()
  {
    levelText.text = "关卡：" + level;
  }
}