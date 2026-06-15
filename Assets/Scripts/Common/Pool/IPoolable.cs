using UnityEngine;

/// <summary>
/// 对象池接口，用于需要特殊处理出池入池逻辑的对象
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// 当对象从池中取出时调用
    /// </summary>
    void OnAlloc();

    /// <summary>
    /// 当对象返回池中时调用
    /// </summary>
    void OnFree();
}
