using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffManager : SingletonMono<BuffManager>
{
  [Header("玩家buff")]
  public BuffSO playerSpeedSO;
  public BuffSO playerAttackRangeSO;

  public Hero hero;
}
