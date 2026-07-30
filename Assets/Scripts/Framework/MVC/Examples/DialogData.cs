using System;

/// <summary>
/// 示例：弹窗数据
/// 这是一个自定义数据类的示例
/// 实现 IUIData 接口以支持 Reset 功能
/// </summary>
[Serializable]
public class DialogData : IUIData
{
  public string Title;
  public string Content;
  public string ConfirmButtonText = "确定";
  public string CancelButtonText = "取消";
  public bool ShowCancelButton;
  public Action OnConfirm;
  public Action OnCancel;

  /// <summary>
  /// 重置数据到默认状态
  /// </summary>
  public void Reset()
  {
    Title = string.Empty;
    Content = string.Empty;
    ConfirmButtonText = "确定";
    CancelButtonText = "取消";
    ShowCancelButton = false;
    OnConfirm = null;
    OnCancel = null;
  }
}