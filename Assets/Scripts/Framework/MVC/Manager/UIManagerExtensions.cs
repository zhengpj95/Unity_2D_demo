using System;
using UnityEngine;
using Framework.MVC;

/// <summary>
/// UIManager扩展 - MVC支持
/// 为UIManager添加MVC框架集成功能
/// 
/// 注意：此文件必须在 MVC 框架编译后才能正常工作
/// 如果遇到编译错误，请等待 Unity 完成 MVC 框架的编译
/// </summary>
public static class UIManagerExtensions
{
    /// <summary>
    /// 显示UI并绑定Controller（MVC模式）
    /// 自动创建Controller、绑定View、启动控制器
    /// </summary>
    public static TController ShowUIWithController<TView, TController, TData>(
        this UIManager uiManager,
        string prefabPath,
        UILayerIndex layer,
        TData data = null
    )
        where TView : UIView<TData>
        where TController : UIController<TData>, new()
        where TData : UIData, new()
    {
        // 显示UI
        uiManager.ShowUI(prefabPath, layer);

        // 获取View实例
        var view = uiManager.GetUIComponent<TView>(prefabPath);
        if (view == null)
        {
            Debug.LogError($"[UIManagerExtensions] View component not found: {typeof(TView).Name}");
            return null;
        }

        // 创建或获取Controller
        var controller = UIControllerFactory.GetOrCreate<TController, TData>(view);

        // 设置数据
        if (data != null)
        {
            controller.UpdateData(data);
        }

        // 启动Controller
        controller.Start();

        return controller;
    }

    /// <summary>
    /// 隐藏UI并清理Controller（MVC模式）
    /// </summary>
    public static void HideUIWithController<TController, TData>(
        this UIManager uiManager,
        string prefabPath,
        bool isDestroy = false
    )
        where TController : UIController<TData>
        where TData : UIData, new()
    {
        // 获取Controller并清理
        var controller = UIControllerManager.Instance.Get<TController>();
        if (controller != null)
        {
            controller.Cleanup();
            UIControllerManager.Instance.Unregister<TController>();
        }

        // 隐藏UI
        uiManager.HideUI(prefabPath, isDestroy);
    }

    /// <summary>
    /// 预加载UI（不显示）
    /// 用于性能优化，提前加载UI资源
    /// </summary>
    public static TController PreloadUI<TView, TController, TData>(
        this UIManager uiManager,
        string prefabPath,
        UILayerIndex layer
    )
        where TView : UIView<TData>
        where TController : UIController<TData>, new()
        where TData : UIData, new()
    {
        // 预加载UI（隐藏状态）
        if (!uiManager.HasUI(prefabPath))
        {
            uiManager.ShowUI(prefabPath, layer);
            uiManager.HideUI(prefabPath, false);
        }

        // 获取View实例
        var view = uiManager.GetUIComponent<TView>(prefabPath);
        if (view == null)
        {
            Debug.LogError($"[UIManagerExtensions] View component not found: {typeof(TView).Name}");
            return null;
        }

        // 创建Controller（不启动）
        var controller = UIControllerFactory.Create<TController, TData>(view, register: true);

        return controller;
    }
}
