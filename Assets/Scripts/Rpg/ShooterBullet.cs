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

  private void OnCollisionEnter2D(Collision2D collision)
  {
    var effect = Instantiate(effectPrefab, transform.position, Quaternion.identity);
    Destroy(effect, 0.6f);
    Destroy(gameObject);
  }
}
