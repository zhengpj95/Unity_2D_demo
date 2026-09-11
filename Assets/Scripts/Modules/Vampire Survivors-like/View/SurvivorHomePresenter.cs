using UnityEngine;
using System;

/// <summary>Survivor 主界面的打开参数；场景切换由 GameplayController 编排。</summary>
public readonly struct SurvivorHomeArgs
{
  /// <summary>将开始按钮输入交回 SurvivorGameplayController 的流程回调。</summary>
  public Action OnStartBattle { get; }

  /// <summary>创建主界面参数。</summary>
  /// <param name="onStartBattle">玩家点击开始战斗后的请求回调。</param>
  public SurvivorHomeArgs(Action onStartBattle)
  {
    OnStartBattle = onStartBattle;
  }
}

/// <summary>
/// Survivor 局外主界面；只负责展示与转发输入，不直接加载场景或修改战斗时间。
/// </summary>
public sealed class SurvivorHomePresenter : BasePresenter<SurvivorHomeView, SurvivorHomeArgs>
{
  private Action _onStartBattle;

  /// <summary>Home 使用主界面层，避免占用局内弹窗层级。</summary>
  public override UILayerIndex Layer => UILayerIndex.Main;
  /// <summary>Resources 下的 SurvivorHome Prefab 路径。</summary>
  public override string PrefabPath => "Prefabs/SurvivorHome";

  public override void OnInit(UIView view)
  {
    base.OnInit(view);
    if (ViewT == null)
      return;

    // 自动生成的 View 同时支持 Inspector 绑定和运行时路径解析，初始化后再注册按钮。
    ViewT.InitView();
    AddClickListener(ViewT.btnStart, OnStartClicked);
  }

  public override void OnOpen(SurvivorHomeArgs args)
  {
    _onStartBattle = args.OnStartBattle;
    base.OnOpen(args);
  }

  public override void OnClose()
  {
    // 隐藏 Home 时清除本次回调；再次打开会注入新的流程入口。
    _onStartBattle = null;
    base.OnClose();
  }

  private void OnStartClicked()
  {
    Debug.Log("[SurvivorHomePresenter] 玩家点击开始战斗按钮，转发回调。", ViewT);
    if (_onStartBattle == null)
    {
      Debug.LogWarning("[SurvivorHomePresenter] 开始战斗回调为空，已忽略点击。", ViewT);
      return;
    }

    _onStartBattle.Invoke();
  }

}
