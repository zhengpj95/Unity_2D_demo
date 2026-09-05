using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike {

  public class VSPlayerHealth : MonoBehaviour
  {
    private bool _isDead;

    void Start()
    {
      _isDead = false;
    }

    public void TakeDamage(int damage)
    {
      if (_isDead || damage <= 0)
        return;

      SurvivorModule survivorModule = ModuleManager.Instance.GetModule<SurvivorModule>(ModuleName.Survivor);
      SurvivorModel model = survivorModule?.ApplyPlayerDamage(damage);
      DamageController.Instance.ShowDamage(damage, transform.position);

      // 死亡只上报一次，具体暂停、结算与重开流程由 GameplayController 编排。
      if (model == null || model.CurrentHealth > 0)
        return;

      _isDead = true;
      survivorModule?.OnPlayerDied();
    }

    /// <summary>转发最大生命升级到 SurvivorModule；运行时生命数据不保存在本组件。</summary>
    public void ApplyMaxHealthUpgrade(float value, bool isPercent)
    {
      ModuleManager.Instance.GetModule<SurvivorModule>(ModuleName.Survivor)?.ApplyPlayerMaxHealthUpgrade(value, isPercent);
    }
  }

}
