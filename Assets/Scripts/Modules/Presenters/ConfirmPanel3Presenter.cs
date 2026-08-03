using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 确认面板参数（使用 struct 避免 GC）
/// </summary>
public struct ConfirmPanel3Args
{
  public string title;
  public string desc;

  public ConfirmPanel3Args(string title, string desc)
  {
    this.title = title;
    this.desc = desc;
  }
}

/// <summary>
/// 使用泛型基类的 Presenter 示例（类型安全）
/// </summary>
public class ConfirmPanel3Presenter : UIPresenter<ConfirmPanel3View, ConfirmPanel3Args>
{
  public override void OnInit(UIView view)
  {
    base.OnInit(view);

    // 使用工具方法添加监听器（自动管理生命周期）
    if (ViewT != null)
    {
      AddClickListener(ViewT.btn_confirm, OnConfirmClicked);
      AddClickListener(ViewT.btn_cancel, OnCancelClicked);
    }
  }

  public override void OnOpen(ConfirmPanel3Args args)
  {
    base.OnOpen(args);

    // 类型安全，无需反射
    if (ViewT != null)
    {
      ViewT.txt_title.text = args.title;
      ViewT.txt_desc.text = args.desc;
    }
  }

  public override void OnShow()
  {
    Debug.Log($"[{GetType().Name}] OnShow called");
  }

  public override void OnHide()
  {
    Debug.Log($"[{GetType().Name}] OnHide called");
  }

  private void OnConfirmClicked()
  {
    Debug.Log("ConfirmPanel3Presenter: Confirm clicked!");
    Close();
  }

  private void OnCancelClicked()
  {
    Debug.Log("ConfirmPanel3Presenter: Cancel clicked!");
    Close();
  }
}