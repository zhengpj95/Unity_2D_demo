using UnityEngine;

namespace VampireSurvivorsLike
{
  /// <summary>
  /// Survivor 场景的无边界 2D 相机跟随。
  /// 不做地图范围裁剪；目标可以在无限地表上任意移动。
  /// </summary>
  [DisallowMultipleComponent]
  public sealed class SurvivorCameraFollow : MonoBehaviour
  {
    [SerializeField] private Transform _target;
    [SerializeField] private Vector2 _offset;
    [Min(0f)]
    [SerializeField] private float _smoothTime = 0.12f;

    private Vector3 _velocity;

    private void Awake()
    {
      ResolveTarget();
    }

    private void LateUpdate()
    {
      if (_target == null)
        ResolveTarget();

      if (_target == null)
        return;

      Vector3 desiredPosition = new Vector3(
        _target.position.x + _offset.x,
        _target.position.y + _offset.y,
        transform.position.z);

      transform.position = _smoothTime <= 0f
        ? desiredPosition
        : Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, _smoothTime);
    }

    private void ResolveTarget()
    {
      GameObject player = GameObject.FindWithTag("Player");
      if (player != null)
        _target = player.transform;
    }
  }
}
