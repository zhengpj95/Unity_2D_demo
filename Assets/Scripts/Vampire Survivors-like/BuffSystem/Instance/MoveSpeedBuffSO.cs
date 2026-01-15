using UnityEngine;

[CreateAssetMenu(menuName = "Buff/MoveSpeedUpSO")]
public class MoveSpeedBuffSO : BuffSO
{
  [Tooltip("移动速度增加率【0.2f 表示增加20%】"), Range(0.05f, 1f)]
  public float speedRate = 0.2f;

  public override BuffInstance CreateInstance()
  {
    return new MoveSpeedBuffInstance(this);
  }
}