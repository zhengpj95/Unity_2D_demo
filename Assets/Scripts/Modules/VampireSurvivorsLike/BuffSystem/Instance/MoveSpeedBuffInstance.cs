using UnityEngine;

namespace VampireSurvivorsLike {

  /**
   * 移动速度buff实例
   * 增加移动速度
   * 减少移动速度
   * 持续时间：data.duration
   * 最大叠加层数：data.maxStack
   */
  public class MoveSpeedBuffInstance : BuffInstance
  {
    private float rate;
    public float moveSpeedMultiplier = 0f;

    public MoveSpeedBuffInstance(MoveSpeedBuffSO data) : base(data)
    {
      rate = data.speedRate;
    }

    public override void OnAdd()
    {
      moveSpeedMultiplier += rate;
    }

    public override void OnRemove()
    {
      moveSpeedMultiplier -= rate * stack;
    }

    /// <summary>将当前叠加后的移速效果暴露为统一玩家属性临时修正。</summary>
    public override PlayerStatModifier GetStatModifier(PlayerStat stat)
    {
      return stat == PlayerStat.MoveSpeed
        ? new PlayerStatModifier(0f, moveSpeedMultiplier)
        : default;
    }
  }
}
