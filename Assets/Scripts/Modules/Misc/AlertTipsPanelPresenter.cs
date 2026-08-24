using System;
using UnityEngine;
using UnityEngine.UI;

public struct AlertTipsPanelArgs
{
  public string title;
  public string desc;
  public Action confirmAction;

  public AlertTipsPanelArgs(string title, string desc, Action confirm)
  {
    this.title = title;
    this.desc = desc;
    this.confirmAction = confirm;
  }
}

public class AlertTipsPanelPresenter : BasePresenter<AlertTipsPanelView, AlertTipsPanelArgs>
{
  private Action _confirmCallback;

  public override void OnInit(UIView view)
  {
    base.OnInit(view);
    if (ViewT != null)
    {
      AddClickListener(ViewT.btn_confirm, OnConfirmClicked);
    }
  }

  public override void OnOpen(AlertTipsPanelArgs args)
  {
    base.OnOpen(args);
    ViewT.txt_title.text = args.title ?? "警告";
    ViewT.txt_desc.text = args.desc ?? "";
    _confirmCallback = args.confirmAction;
  }

  public override void OnClose()
  {
    base.OnClose();
  }

  public void OnConfirmClicked()
  {
    _confirmCallback?.Invoke();
    UIManager.Instance.CloseWindow(this);
  }

}
