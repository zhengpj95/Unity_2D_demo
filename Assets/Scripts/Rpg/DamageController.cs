using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamageController : MonoBehaviour
{
  public static DamageController Instance { get; private set; }
  public Transform damagePrefab;
  public Transform point;

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
      DontDestroyOnLoad(gameObject);
    }
    else
    {
      Destroy(gameObject);
    }
  }

  public void ShowDamage(int damageAmount, Vector3 position)
  {
    if (damagePrefab != null)
    {
      Transform dmgText = Instantiate(damagePrefab, position, Quaternion.identity, point);
      UI_Damage damage = dmgText.GetComponent<UI_Damage>();
      if (damage != null)
      {
        damage.SetDamageText(damageAmount);
      }
    }
  }
}
