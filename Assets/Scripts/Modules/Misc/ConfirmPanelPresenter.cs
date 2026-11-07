using System;
using UnityEngine;
using UnityEngine.UI;

public struct ConfirmPanelArgs
{
  public string title;
  public string desc;
}

public class ConfirmPanelPresenter : BasePresenter<ConfirmPanelView, ConfirmPanelArgs>
{
  private ConfirmPanelView _panel;

  public override UILayerIndex Layer => UILayerIndex.Model;
  public override string PrefabPath => "Prefabs/CommonView/ConfirmPanel";

  public override void OnInit(UIView view)
  {
    base.OnInit(view);
    _panel = ViewT;
    _panel.InitView();
    if (_panel.btn_confirm == null
        || _panel.btn_cancel == null
        || _panel.btn_close == null
        || _panel.txt_title == null
        || _panel.txt_desc == null)
    {
      throw new InvalidOperationException(
        "[ConfirmPanelPresenter] ConfirmPanelView is missing a required button or text reference.");
    }

    // 在 Init 中绑定事件（只执行一次）
    _panel.btn_confirm.onClick.AddListener(OnConfirmClicked);
    _panel.btn_cancel.onClick.AddListener(OnCancelClicked);
    _panel.btn_close.Clicked += OnCloseClicked;
  }

  public override void OnOpen(ConfirmPanelArgs args = default)
  {
    base.OnOpen(args);

    // 设置文本内容
    _panel.txt_title.text = args.title ?? "";
    _panel.txt_desc.text = args.desc ?? "";
  }

  public override void OnClose()
  {
    base.OnClose();
    // 不再需要 RemoveAllListeners，事件在 OnDestroy 中解绑
  }

  public override void OnDestroy()
  {
    // 先解绑自定义监听；基类随后销毁 View 并清空强类型访问所依赖的引用。
    if (_panel != null)
    {
      if (_panel.btn_confirm != null)
        _panel.btn_confirm.onClick.RemoveListener(OnConfirmClicked);
      if (_panel.btn_cancel != null)
        _panel.btn_cancel.onClick.RemoveListener(OnCancelClicked);
      if (_panel.btn_close != null)
        _panel.btn_close.Clicked -= OnCloseClicked;
    }

    _panel = null;
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

  private void OnCloseClicked()
  {
    Debug.Log("OnCloseClicked Triggered!");
    Close();
  }
}