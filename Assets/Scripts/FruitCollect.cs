using UnityEngine;

public class FruitCollect : MonoBehaviour
{
  private void OnTriggerEnter2D(Collider2D other)
  {
    if (other.CompareTag("Fruits"))
    {
      Destroy(other.gameObject);
      FrogManager.Instance.Score++;
      Debug.Log("Fruit collected: " + FrogManager.Instance.Score);
    }
  }
}