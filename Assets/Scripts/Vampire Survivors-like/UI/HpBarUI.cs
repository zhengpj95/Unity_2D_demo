using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HpBarUI : MonoBehaviour
{
  [SerializeField] private Transform fill;

  public void SetPercent(float p)
  {
    float scaleX = p - (0.002f * 2);
    fill.localScale = new Vector3(scaleX, fill.localScale.y, fill.localScale.z);
  }
}
