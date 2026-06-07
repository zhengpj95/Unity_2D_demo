using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Melee
{
  public class CameraFollow : MonoBehaviour
  {
    [Header("Follow Target")] 
    public Transform target;
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Smooth Follow")] [Range(0f, 1f)]
    public float smoothTime = 0.1f;

    [Header("Boundaries")]
    public float minX = 0f;

    private Vector3 currentVelocity = Vector3.zero;

    private void Reset()
    {
      if (target == null)
      {
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
          target = player.transform;
        }
      }
    }

    private void LateUpdate()
    {
      if (target == null)
      {
        return;
      }

      // Only follow horizontally, keep Y position fixed
      Vector3 desiredPosition = new Vector3(target.position.x + offset.x,
        transform.position.y,
        offset.z);

      // Apply left boundary constraint
      desiredPosition.x = Mathf.Max(desiredPosition.x, minX);

      transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);
    }
  }
}