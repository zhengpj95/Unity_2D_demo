using System;
using UnityEngine;

namespace VampireSurvivorsLike
{

  /// <summary>管理敌人的运行时生命、死亡结算、场景统一掉落与对象池回收。</summary>
  public class VSEnemyHealth : MonoBehaviour, IPoolable
  {
    private const string MoveAnimName = "Move";
    private const string HitAnimName = "Hit";

    [SerializeField] private int maxHealth;
    private int currentHealth;
    private AnimSprite _animSprite;
    private Action _resumeMoveAnimation;

    private void Awake()
    {
      _animSprite = GetComponentInChildren<AnimSprite>(true);
      _resumeMoveAnimation = ResumeMoveAnimation;
    }

    public void OnAlloc()
    {
      currentHealth = maxHealth;
      UpdateHpBar();
      _animSprite?.Play(MoveAnimName);
    }

    public void OnFree()
    {
      currentHealth = 0;
      _animSprite?.Stop(false);
    }

    public void TakeDamage(int damage)
    {
      currentHealth -= damage;
      UpdateHpBar();
      DamageController.Instance.ShowDamage(damage, transform.position);
      if (currentHealth <= 0)
      {
        EnemyDirector.Instance.KillEnemyCount++;
        ModuleManager.Instance.GetModule<SurvivorModule>(ModuleName.Survivor).UpdateEnemyKillCount();
        // 掉落由场景级权重统一决定，避免不同敌人 Prefab 固定产出同一种资源。
        DropItemManager.Instance.SpawnDropItem(transform.position);
        EnemyDirector.Instance.RecycleEnemy(gameObject);
        return;
      }

      // 受击动画为单次播放，结束后恢复统一的移动动作；连续受击会从头重播 Hit。
      if (_animSprite != null && _animSprite.HasAnimation(HitAnimName))
      {
        _animSprite.Play(HitAnimName, _resumeMoveAnimation);
      }
    }

    /// <summary>受击动画结束后恢复敌人的循环移动动画。</summary>
    private void ResumeMoveAnimation()
    {
      _animSprite?.Play(MoveAnimName);
    }

    private void UpdateHpBar()
    {
      UI_HpBar hpBarUI = gameObject.GetComponent<UI_HpBar>();
      if (hpBarUI)
      {
        hpBarUI.SetPercent(Mathf.Max(0f, currentHealth / (maxHealth * 1f)));
      }
    }
  }

}
