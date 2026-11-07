using System;
using UnityEngine;

public struct ConfirmPanelArgs
{
  public string title;
  public string desc;
  /// <summary>可选的确定回调；为空时点击确定只关闭面板。</summary>
  public Action confirmAction;
  /// <summary>可选的取消回调；为空时点击取消只关闭面板。</summary>
  public Action cancelAction;
}

public class ConfirmPanelPresenter : BasePresenter<ConfirmPanelView, ConfirmPanelArgs>
{
  private Action _confirmCallback;
  private Action _cancelCallback;

  public override UILayerIndex Layer => UILayerIndex.Model;
  public override string PrefabPath => "Prefabs/Misc/ConfirmPanel";

  public override void OnInit(UIView view)
  {
    base.OnInit(view);
    ViewT.InitView();
    if (ViewT.btn_confirm == null
        || ViewT.btn_cancel == null
        || ViewT.btn_close == null
        || ViewT.txt_title == null
        || ViewT.txt_desc == null)
    {
      throw new InvalidOperationException(
        "[ConfirmPanelPresenter] ConfirmPanelView is missing a required button or text reference.");
    }

    AddClickListener(ViewT.btn_confirm, OnConfirmClicked);
    AddClickListener(ViewT.btn_cancel, OnCancelClicked);
    ViewT.btn_close.Clicked += OnCloseClicked;
  }

  public override void OnOpen(ConfirmPanelArgs args = default)
  {
    base.OnOpen(args);

    ViewT.txt_title.text = args.title ?? "";
    ViewT.txt_desc.text = args.desc ?? "";
    _confirmCallback = args.confirmAction;
    _cancelCallback = args.cancelAction;
  }

  public override void OnDestroy()
  {
    // UIButton 使用 C# 事件，需在基类销毁 View 前解绑。
    if (View is ConfirmPanelView view && view.btn_close != null)
      view.btn_close.Clicked -= OnCloseClicked;
    base.OnDestroy();
  }

  public override void OnClose()
  {
    _confirmCallback = null;
    _cancelCallback = null;
    base.OnClose();
  }

  private void OnConfirmClicked()
  {
    Debug.Log("OnConfirmClicked Triggered!");
    Action confirmCallback = _confirmCallback;
    _confirmCallback = null;
    _cancelCallback = null;
    try
    {
      confirmCallback?.Invoke();
    }
    finally
    {
      Close();
    }
  }

  private void OnCancelClicked()
  {
    Debug.Log("OnCancelClicked Triggered!");
    Action cancelCallback = _cancelCallback;
    _confirmCallback = null;
    _cancelCallback = null;
    try
    {
      cancelCallback?.Invoke();
    }
    finally
    {
      Close();
    }
  }

  private void OnCloseClicked()
  {
    Debug.Log("OnCloseClicked Triggered!");
    _confirmCallback = null;
    _cancelCallback = null;
    Close();
  }
}