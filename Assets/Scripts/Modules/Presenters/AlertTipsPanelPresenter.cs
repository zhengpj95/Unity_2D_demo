using UnityEngine;
using UnityEngine.UI;

public struct AlertTipsPanelArgs
{
  public string title;
  public string desc;

  public AlertTipsPanelArgs(string title, string desc)
  {
    this.title = title;
    this.desc = desc;
  }
}

public class AlertTipsPanelPresenter : BasePresenter<AlertTipsPanelView, AlertTipsPanelArgs>
{

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
  }

  public override void OnClose()
  {
    base.OnClose();
  }

  public void OnConfirmClicked()
  {
    Debug.Log("AlertTipsPanelPresenter: Confirm clicked!");
    UIManager.Instance.CloseWindow(this);
  }

}
