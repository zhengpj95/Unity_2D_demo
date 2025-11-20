using UnityEngine;
using UnityEngine.SceneManagement;

public class CheckEndPoint : MonoBehaviour
{
  private void OnTriggerEnter2D(Collider2D other)
  {
    if (other.gameObject.CompareTag("Player"))
    {
      Debug.Log("到达终点，切换关卡");
      SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
  }
}