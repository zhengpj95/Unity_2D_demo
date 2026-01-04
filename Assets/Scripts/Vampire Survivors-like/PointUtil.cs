using UnityEngine;

/// <summary>
/// 坐标点工具类（2D）
/// 用于刷怪 / 位移 / 技能 / AI
/// </summary>
public static class PointUtil
{
  /* ===============================
   * 基础方向
   * =============================== */

  /// 横向随机点（左右）
  public static Vector2 RandomHorizontal(Vector2 center, float range)
  {
    return new Vector2(
      center.x + Random.Range(-range, range),
      center.y
    );
  }

  /// 竖向随机点（上下）
  public static Vector2 RandomVertical(Vector2 center, float range)
  {
    return new Vector2(
      center.x,
      center.y + Random.Range(-range, range)
    );
  }

  /// 指定方向 + 距离
  public static Vector2 Offset(Vector2 center, Vector2 direction, float distance)
  {
    return center + direction.normalized * distance;
  }

  /* ===============================
   * 圆形 / 环形
   * =============================== */

  /// 圆形范围内随机点（刷怪最常用）
  public static Vector2 RandomInCircle(Vector2 center, float radius)
  {
    return center + Random.insideUnitCircle * radius;
  }

  /// 圆周上的随机点（环形刷怪）
  public static Vector2 RandomOnCircle(Vector2 center, float radius)
  {
    float angle = Random.Range(0f, 360f);
    Vector2 dir = AngleToDirection(angle);
    return center + dir * radius;
  }

  /* ===============================
   * 扇形
   * =============================== */

  /// 扇形范围随机点（技能锥形）
  /// forwardAngle：朝向角度（度）
  /// halfAngle：扇形一半角度
  public static Vector2 RandomInSector(
    Vector2 center,
    float radius,
    float forwardAngle,
    float halfAngle
  )
  {
    float angle = Random.Range(
      forwardAngle - halfAngle,
      forwardAngle + halfAngle
    );

    float distance = Random.Range(0f, radius);
    Vector2 dir = AngleToDirection(angle);
    return center + dir * distance;
  }

  /* ===============================
   * 工具方法
   * =============================== */

  /// 角度转方向
  public static Vector2 AngleToDirection(float angle)
  {
    float rad = angle * Mathf.Deg2Rad;
    return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
  }

  /// 两点方向
  public static Vector2 Direction(Vector2 from, Vector2 to)
  {
    return (to - from).normalized;
  }

  /// 限制距离（常用于最远攻击距离）
  public static Vector2 ClampDistance(Vector2 center, Vector2 target, float maxDistance)
  {
    Vector2 dir = target - center;
    if (dir.magnitude > maxDistance)
    {
      return center + dir.normalized * maxDistance;
    }
    return target;
  }
}
