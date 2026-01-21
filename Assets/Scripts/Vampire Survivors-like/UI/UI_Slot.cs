using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Slot : MonoBehaviour
{
  public Image Icon;
  public Text CountText;

  public void UpdateCount(int count)
  {
    CountText.text = count.ToString();
  }
}
