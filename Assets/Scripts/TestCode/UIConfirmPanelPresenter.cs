using System;
using UnityEngine;
using UnityEngine.UI;

public class UIConfirmPanelPresenter : UIPresenter
{

  public override void OnOpen(object args = null)
  {
    Debug.Log("UIConfirmPanelPresenter OnOpen called with args: " + args);
    base.OnOpen(args);
    UIConfirmPanel panel = View as UIConfirmPanel;
    if (panel != null && args != null)
    {
      var type = args.GetType();
      var titleProp = type.GetProperty("title");
      var descProp = type.GetProperty("desc");
      panel.txt_title.text = titleProp.GetValue(args)?.ToString();
      panel.txt_desc.text = descProp.GetValue(args)?.ToString();
    }

    panel.btn_confirm.onClick.AddListener(OnConfirmClicked);
    panel.btn_cancel.onClick.AddListener(OnConfirmClicked);
  }

  public override void OnClose()
  {
    base.OnClose();
    UIConfirmPanel panel = View as UIConfirmPanel;
    if (panel != null)
    {
      panel.btn_confirm.onClick.RemoveAllListeners();
      panel.btn_cancel.onClick.RemoveAllListeners();
    }
  }

  private void OnConfirmClicked()
  {
    UIManager.Instance.CloseWindow<UIConfirmPanelPresenter>();
  }
}