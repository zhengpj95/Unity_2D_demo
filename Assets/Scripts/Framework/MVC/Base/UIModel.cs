using System;

/// <summary>
/// UI Model基类
/// 负责数据管理和业务逻辑处理
/// </summary>
/// <typeparam name="TData">数据类型</typeparam>
public abstract class UIModel<TData> : IModel<TData> where TData : new()
{
  /// <summary>
  /// 数据实例
  /// </summary>
  protected TData _data;

  /// <summary>
  /// 数据变化事件
  /// </summary>
  public event Action<TData> OnDataChanged;

  /// <summary>
  /// 获取数据
  /// </summary>
  public TData Data => _data;

  /// <summary>
  /// 是否已初始化
  /// </summary>
  public bool IsInitialized { get; protected set; }

  /// <summary>
  /// 构造函数
  /// </summary>
  protected UIModel()
  {
    _data = new TData();
  }

  /// <summary>
  /// 初始化模型
  /// </summary>
  public virtual void Initialize()
  {
    if (IsInitialized) return;
    IsInitialized = true;
    OnInit();
  }

  /// <summary>
  /// 子类可重写的初始化方法
  /// </summary>
  protected virtual void OnInit() { }

  /// <summary>
  /// 更新数据并触发事件
  /// </summary>
  /// <param name="data">新数据</param>
  public virtual void UpdateData(TData data)
  {
    _data = data;
    OnDataChanged?.Invoke(_data);
  }

  /// <summary>
  /// 更新数据（不触发事件）
  /// </summary>
  /// <param name="data">新数据</param>
  public void SetDataSilent(TData data)
  {
    _data = data;
  }

  /// <summary>
  /// 手动触发数据变化事件
  /// </summary>
  protected void NotifyDataChanged()
  {
    OnDataChanged?.Invoke(_data);
  }

  /// <summary>
  /// 修改数据（通过Action）
  /// </summary>
  /// <param name="modifier">修改函数</param>
  public void ModifyData(Action<TData> modifier)
  {
    modifier?.Invoke(_data);
    OnDataChanged?.Invoke(_data);
  }

  /// <summary>
  /// 重置数据（如果数据实现了IUIData接口）
  /// </summary>
  public void ResetData()
  {
    if (_data is IUIData uidata)
    {
      uidata.Reset();
    }
    else
    {
      _data = new TData();
    }
    OnDataChanged?.Invoke(_data);
  }

  /// <summary>
  /// 清理模型
  /// </summary>
  public virtual void Cleanup()
  {
    OnDataChanged = null;
    if (_data is IUIData uidata)
    {
      uidata.Reset();
    }
    IsInitialized = false;
    OnCleanup();
  }

  /// <summary>
  /// 子类可重写的清理方法
  /// </summary>
  protected virtual void OnCleanup() { }
}

/// <summary>
/// 简单Model实现
/// 可直接使用，无需继承
/// </summary>
/// <typeparam name="TData">数据类型</typeparam>
public class SimpleModel<TData> : UIModel<TData> where TData : new()
{
  // 使用基类的所有实现，无需额外代码
}
