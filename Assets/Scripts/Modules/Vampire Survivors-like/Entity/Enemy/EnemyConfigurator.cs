using UnityEngine;

namespace VampireSurvivorsLike
{
  /// <summary>
  /// 把 EnemyTypeConfig 的配置应用到通用敌人实例。
  /// 运行时出池与 Editor 场景实例化共用同一入口，避免两处维护字段映射。
  /// </summary>
  public static class EnemyConfigurator
  {
    /// <summary>
    /// 将配置写入通用敌人实例；组件缺失时记录错误并返回 false，调用方应放弃该实例。
    /// </summary>
    /// <param name="enemyObject">EnemyCommon Prefab 的实例。</param>
    /// <param name="config">要应用的敌人类型配置。</param>
    /// <returns>组件齐全且配置有效时返回 true。</returns>
    public static bool Apply(GameObject enemyObject, EnemyTypeConfig config)
    {
      if (enemyObject == null || config == null)
      {
        Debug.LogError("[EnemyConfigurator] 缺少敌人实例或 EnemyTypeConfig，未应用配置。");
        return false;
      }

      EnemyChasing chasing = enemyObject.GetComponent<EnemyChasing>();
      VSEnemyHealth health = enemyObject.GetComponent<VSEnemyHealth>();
      AnimFrame anim = enemyObject.GetComponentInChildren<AnimFrame>(true);
      CapsuleCollider2D collider = enemyObject.GetComponent<CapsuleCollider2D>();
      if (chasing == null || health == null || anim == null || collider == null)
      {
        Debug.LogError($"[EnemyConfigurator] 敌人实例 '{enemyObject.name}' 缺少追击/血量/动画/碰撞体组件，未应用配置。", enemyObject);
        return false;
      }

      chasing.Configure(config.ChaseSpeed, config.Damage);
      health.Configure(config.MaxHealth);
      if (config.AnimConfig != null)
      {
        // 配置切换后重新播放默认动作，覆盖对象池上一轮遗留的动画状态。
        anim.SetConfig(config.AnimConfig, true);
      }
      else
      {
        // 新建的 EnemyTypeConfig 可能还没指定动画；此时保留 Prefab 默认配置，避免生成无贴图的隐形敌人。
        Debug.LogWarning($"[EnemyConfigurator] 敌人类型配置 '{config.name}' 未指定 AnimConfig，沿用 Prefab 默认动画。", config);
        anim.PlayDefault();
      }

      collider.offset = config.ColliderOffset;
      collider.size = config.ColliderSize;
      collider.direction = config.ColliderDirection;

      Transform hpBar = enemyObject.transform.Find("HpBar");
      if (hpBar != null)
      {
        // 零位置与单位缩放表示沿用 Prefab 默认值，避免覆盖美术调好的血条位置。
        if (config.HpBarLocalPosition != Vector3.zero) hpBar.localPosition = config.HpBarLocalPosition;
        if (config.HpBarLocalScale != Vector3.one) hpBar.localScale = config.HpBarLocalScale;
      }

      // spriteSize 在 Simple 绘制模式下不参与显示，保留在配置中供后续绘制模式使用，此处不写 m_Size 制造视觉错觉。
      return true;
    }
  }
}
