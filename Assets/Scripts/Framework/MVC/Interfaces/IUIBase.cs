using System;

/// <summary>
/// UI数据基类接口
/// 所有UI数据模型都应继承此接口
/// </summary>
public interface IUIData
{
  /// <summary>
  /// 重置数据到默认状态
  /// </summary>
  void Reset();
}

/// <summary>
/// Model接口 - 负责数据管理
/// </summary>
/// <typeparam name="TData">数据类型</typeparam>
public interface IModel<TData> where TData : IUIData
{
  /// <summary>
  /// 获取数据
  /// </summary>
  TData Data { get; }

  /// <summary>
  /// 数据变化事件
  /// </summary>
  event Action<TData> OnDataChanged;

  /// <summary>
  /// 初始化模型
  /// </summary>
  void Initialize();

  /// <summary>
  /// 更新数据
  /// </summary>
  /// <param name="data">新数据</param>
  void UpdateData(TData data);

  /// <summary>
  /// 清理模型
  /// </summary>
  void Cleanup();
}

/// <summary>
/// View接口 - 负责UI显示和用户交互
/// </summary>
/// <typeparam name="TData">数据类型</typeparam>
public interface IView<TData> where TData : IUIData
{
  /// <summary>
  /// 获取对应的GameObject
  /// </summary>
  UnityEngine.GameObject GameObject { get; }

  /// <summary>
  /// UI显示状态变化事件
  /// </summary>
  event Action<bool> OnVisibilityChanged;

  /// <summary>
  /// 初始化视图
  /// </summary>
  void Initialize();

  /// <summary>
  /// 更新视图显示
  /// </summary>
  /// <param name="data">数据</param>
  void UpdateView(TData data);

  /// <summary>
  /// 显示视图
  /// </summary>
  void Show();

  /// <summary>
  /// 隐藏视图
  /// </summary>
  void Hide();

  /// <summary>
  /// 清理视图
  /// </summary>
  void Cleanup();
}

/// <summary>
/// Controller接口 - 负责连接Model和View，处理业务逻辑
/// </summary>
/// <typeparam name="TData">数据类型</typeparam>
public interface IController<TData> where TData : IUIData
{
  /// <summary>
  /// 关联的Model
  /// </summary>
  IModel<TData> Model { get; }

  /// <summary>
  /// 关联的View
  /// </summary>
  IView<TData> View { get; }

  /// <summary>
  /// 初始化控制器
  /// </summary>
  /// <param name="view">视图实例</param>
  void Initialize(IView<TData> view);

  /// <summary>
  /// 启动控制器（开始业务逻辑）
  /// </summary>
  void Start();

  /// <summary>
  /// 停止控制器
  /// </summary>
  void Stop();

  /// <summary>
  /// 清理控制器
  /// </summary>
  void Cleanup();
}