using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trap_Saw : MonoBehaviour
{
  public GameObject[] waypoints;
  public float speed = 2f;
  public float rotationSpeed = 2f;
  public float waitTime = 1.5f;

  private int _currentWayPointIndex = 0;
  private bool _canMove = true;

  public void Update()
  {
    transform.Rotate(0, 0, 360 * rotationSpeed * Time.deltaTime); // 旋转

    if (_canMove == false)
    {
      return;
    }

    transform.position = Vector2.MoveTowards(transform.position, waypoints[_currentWayPointIndex].transform.position,
      speed * Time.deltaTime);

    if (Vector2.Distance(waypoints[_currentWayPointIndex].transform.position, transform.position) < .1f)
    {
      StartCoroutine(WaitAtWaypoint());

      _currentWayPointIndex++;
      if (_currentWayPointIndex >= waypoints.Length)
      {
        _currentWayPointIndex = 0;
      }
    }
  }

  private IEnumerator WaitAtWaypoint()
  {
    _canMove = false;
    yield return new WaitForSeconds(waitTime);
    _canMove = true;
  }
}