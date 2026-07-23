using UnityEngine;

public class DestroyEvent : MonoBehaviour
{
  // 销毁自身
  public void DestroySelf()
  {
    Destroy(gameObject);
  }
}