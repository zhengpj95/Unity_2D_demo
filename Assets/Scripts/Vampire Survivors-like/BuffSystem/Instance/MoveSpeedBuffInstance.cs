using UnityEngine;

/**
 * 移动速度buff实例
 * 增加移动速度
 * 减少移动速度
 * 持续时间：data.duration
 * 最大叠加层数：data.maxStack
 */
public class MoveSpeedBuffInstance : BuffInstance
{
  private Hero hero;
  private float rate;

  public MoveSpeedBuffInstance(MoveSpeedBuffSO data, GameObject target) : base(data, target)
  {
    hero = target.GetComponent<Hero>();
    rate = data.speedRate;
  }

  public override void OnAdd()
  {
    hero.moveSpeedMultiplier += rate;
  }

  public override void OnRemove()
  {
    hero.moveSpeedMultiplier -= rate * stack;
  }
}