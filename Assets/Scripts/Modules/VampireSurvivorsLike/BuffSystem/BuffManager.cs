using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike {

  public class BuffManager : SingletonMono<BuffManager>
  {
    // BuffManager 持有当前战斗场景的 Hero 引用，必须随场景销毁并在下一局重新绑定。
    protected override bool PersistAcrossScenes => false;

    [Header("玩家buff")]
    public BuffSO playerSpeedSO;
    public BuffSO playerAttackRangeSO;

    public Hero hero;
  }

}
