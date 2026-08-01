using UnityEngine;
using UnityEngine.UI;

public class ConfirmPanel2Presenter : UIPresenter
{
  private ConfirmPanel2View _view;

  public override void OnInit(UIView view)
  {
    base.OnInit(view);
    _view = view as ConfirmPanel2View;

    // 自动注册按钮事件绑定模板
    _view.btn_cancel?.onClick.AddListener(OnCancelClicked);
    _view.btn_confirm?.onClick.AddListener(OnConfirmClicked);
  }

  public override void OnOpen(object args = null)
  {
    base.OnOpen(args);
  }

  public override void OnClose()
  {
    base.OnClose();
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
