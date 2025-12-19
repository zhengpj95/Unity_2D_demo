using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Damage : MonoBehaviour
{
  void Start()
  {
    Destroy(gameObject, 1.5f);
  }

  void Update()
  {
    // 让伤害数字缓慢上升
    transform.position += new Vector3(0.6f * Time.deltaTime, 0.8f * Time.deltaTime, 0);
  }

  public void SetDamageText(int damageAmount)
  {
    var text = GetComponent<TMP_Text>();
    if (text != null)
    {
      text.text = damageAmount.ToString();
    }
  }
}
