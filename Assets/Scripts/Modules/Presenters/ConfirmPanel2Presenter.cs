using UnityEngine;
using UnityEngine.UI;

public class ConfirmPanel2Presenter : UIPresenter
{
  private ConfirmPanel2View _view;

  public override void OnInit(UIView view)
  {
    base.OnInit(view);
    _view = view as ConfirmPanel2View;

    if (_view == null)
    {
      Debug.LogError($"[{GetType().Name}] View type mismatch: expected ConfirmPanel2View, got {view?.GetType()}");
      return;
    }

    // 在 Init 中绑定事件（只执行一次）
    if (_view.btn_cancel != null)
      _view.btn_cancel.onClick.AddListener(OnCancelClicked);
    if (_view.btn_confirm != null)
      _view.btn_confirm.onClick.AddListener(OnConfirmClicked);
  }

  public override void OnOpen(object args = null)
  {
    base.OnOpen(args);
  }

  public override void OnClose()
  {
    base.OnClose();
    // 不再需要移除监听器，事件在 OnDestroy 中解绑
  }

  public override void OnDestroy()
  {
    if (_view != null)
    {
      if (_view.btn_cancel != null)
        _view.btn_cancel.onClick.RemoveListener(OnCancelClicked);
      if (_view.btn_confirm != null)
        _view.btn_confirm.onClick.RemoveListener(OnConfirmClicked);
    }
    base.OnDestroy();
  }

  private void OnCancelClicked()
  {
    Debug.Log("OnCancelClicked Triggered!");
    Close(); // 关闭当前界面
  }

  private void OnConfirmClicked()
  {
    Debug.Log("OnConfirmClicked Triggered!");
    Close(); // 关闭当前界面
  }

}
