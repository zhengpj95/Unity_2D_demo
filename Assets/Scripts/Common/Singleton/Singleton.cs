using System;
using System.Threading;

/// <summary>
/// 可继承的通用单例基类（适用于纯 C# 类，非 MonoBehaviour）。
/// 派生类可以定义 protected 或 private 构造函数以防止外部实例化，
/// 并通过 <c>Singleton&lt;T&gt;.Instance</c> 获取唯一实例。
/// 
/// 特性：
/// - 使用 <see cref="Lazy{T}"/> 做线程安全的惰性初始化（ExecutionAndPublication）。
/// - 通过 <c>Activator.CreateInstance(..., nonPublic:true)</c> 支持创建带受保护/私有构造函数的派生类实例。
/// - 提供 <see cref="IsCreated"/> 和 <see cref="GetIfCreated"/> 来查询而不强制创建实例。
/// </summary>
public abstract class Singleton<T> where T : class
{
  private static readonly Lazy<T> IsLazyInstance = new Lazy<T>(
    () => (T)Activator.CreateInstance(typeof(T), nonPublic: true),
    LazyThreadSafetyMode.ExecutionAndPublication);

  /// <summary>
  /// 全局单例访问点（首次访问时会创建实例）。
  /// 在应用退出或异常期间访问可能抛出构造时异常（与 Lazy 行为一致）。
  /// </summary>
  public static T Instance => IsLazyInstance.Value;

  /// <summary>
  /// 指示实例是否已被创建（不会触发创建）。
  /// </summary>
  public static bool IsCreated => IsLazyInstance.IsValueCreated;

  /// <summary>
  /// 如果实例已经创建则返回实例，否则返回 null（不会触发创建）。
  /// </summary>
  public static T GetIfCreated() => IsLazyInstance.IsValueCreated ? IsLazyInstance.Value : null;

  /// <summary>
  /// 受保护构造函数，防止外部通过 new 创建。
  /// 派生类应声明 protected 或 private 构造函数。
  /// </summary>
  protected Singleton()
  {
    //
  }
}
