using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ShooterBullet : MonoBehaviour
{
  public GameObject effectPrefab;
  private void Start()
  {
    Destroy(gameObject, 5f);
  }

  private void OnTriggerEnter2D(Collider2D other)
  {
    if (other.gameObject.CompareTag("Enemy"))
    {
      var enemyHealth = other.gameObject.GetComponent<RpgEnemyHealth>();
      if (enemyHealth != null)
      {
        enemyHealth.ChangeHealth(-StatsManager.Instance.damage);
      }
      var effect = Instantiate(effectPrefab, transform.position, Quaternion.identity);
      Destroy(effect, 0.6f);
      Destroy(gameObject);
    }
  }
}
