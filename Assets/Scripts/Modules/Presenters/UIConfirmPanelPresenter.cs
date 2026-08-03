using System;
using UnityEngine;
using UnityEngine.UI;

public class UIConfirmPanelPresenter : UIPresenter
{
  private ConfirmPanelView _panel;

  public override void OnInit(UIView view)
  {
    base.OnInit(view);
    _panel = view as ConfirmPanelView;

    if (_panel == null)
    {
      Debug.LogError($"[{GetType().Name}] View type mismatch: expected ConfirmPanelView, got {view?.GetType()}");
      return;
    }

    // 在 Init 中绑定事件（只执行一次）
    _panel.btn_confirm.onClick.AddListener(OnConfirmClicked);
    _panel.btn_cancel.onClick.AddListener(OnCancelClicked);
  }

  public override void OnOpen(object args = null)
  {
    base.OnOpen(args);

    if (_panel == null) return;

    // 设置文本内容
    if (args != null)
    {
      var type = args.GetType();
      var titleProp = type.GetProperty("title");
      var descProp = type.GetProperty("desc");

      if (titleProp != null)
        _panel.txt_title.text = titleProp.GetValue(args)?.ToString() ?? "";
      if (descProp != null)
        _panel.txt_desc.text = descProp.GetValue(args)?.ToString() ?? "";
    }
  }

  public override void OnClose()
  {
    base.OnClose();
    // 不再需要 RemoveAllListeners，事件在 OnDestroy 中解绑
  }

  public override void OnDestroy()
  {
    if (_panel != null)
    {
      _panel.btn_confirm.onClick.RemoveListener(OnConfirmClicked);
      _panel.btn_cancel.onClick.RemoveListener(OnCancelClicked);
    }
    base.OnDestroy();
  }

  private void OnConfirmClicked()
  {
    Debug.Log("OnConfirmClicked Triggered!");
    Close();
  }

  private void OnCancelClicked()
  {
    Debug.Log("OnCancelClicked Triggered!");
    Close();
  }
}