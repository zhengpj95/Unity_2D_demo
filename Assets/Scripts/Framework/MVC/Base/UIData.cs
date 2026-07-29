using System;

/// <summary>
/// UI数据基类
/// 所有UI数据模型都应继承此类
/// </summary>
[Serializable]
public abstract class UIData : IUIData
{
  /// <summary>
  /// 重置数据到默认状态
  /// 子类应重写此方法以重置特定数据
  /// </summary>
  public virtual void Reset()
  {
    // 子类实现具体重置逻辑
  }

  /// <summary>
  /// 克隆数据（浅拷贝）
  /// </summary>
  /// <returns>数据副本</returns>
  public virtual UIData Clone()
  {
    return (UIData)MemberwiseClone();
  }
}

/// <summary>
/// 简单数据类型基类（用于简单值类型数据）
/// </summary>
/// <typeparam name="T">值类型</typeparam>
[Serializable]
public class SimpleData<T> : UIData
{
  public T Value;

  public SimpleData() { }

  public SimpleData(T value)
  {
    Value = value;
  }

  public override void Reset()
  {
    Value = default;
  }

  public override UIData Clone()
  {
    return new SimpleData<T>(Value);
  }
}

/// <summary>
/// 弹窗数据示例
/// </summary>
[Serializable]
public class DialogData : UIData
{
  public string Title;
  public string Content;
  public string ConfirmButtonText = "确定";
  public string CancelButtonText = "取消";
  public bool ShowCancelButton;
  public Action OnConfirm;
  public Action OnCancel;

  public override void Reset()
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

/// <summary>
/// 提示消息数据
/// </summary>
[Serializable]
public class MessageData : UIData
{
  public string Message;
  public float Duration = 2f;
  public MessageType Type = MessageType.Info;

  public override void Reset()
  {
    Message = string.Empty;
    Duration = 2f;
    Type = MessageType.Info;
  }
}

/// <summary>
/// 消息类型
/// </summary>
public enum MessageType
{
  Info,
  Success,
  Warning,
  Error
}