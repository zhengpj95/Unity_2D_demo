using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 统一管理所有业务模块，并以 ModuleName 保证模块唯一。
/// </summary>
public sealed class ModuleManager : Singleton<ModuleManager>
{
  private readonly Dictionary<ModuleName, BaseModule> _modules = new();
  private readonly List<Type> _moduleTypes = new();

  private ModuleManager()
  {
  }

  public void RegisterModule(BaseModule module)
  {
    if (module == null)
    {
      throw new ArgumentNullException(nameof(module));
    }

    if (module.ModuleName == ModuleName.None)
    {
      throw new ArgumentException("A module must define a valid ModuleName.", nameof(module));
    }

    if (_modules.ContainsKey(module.ModuleName))
    {
      throw new InvalidOperationException($"Module already registered: {module.ModuleName}");
    }

    _modules.Add(module.ModuleName, module);
  }

  /// <summary>
  /// 批量注册业务模块。模块的初始化由 InitializeAll 统一执行。
  /// </summary>
  public void RegisterModules(params BaseModule[] modules)
  {
    if (modules == null)
    {
      throw new ArgumentNullException(nameof(modules));
    }

    foreach (BaseModule module in modules)
    {
      RegisterModule(module);
    }
  }

  /// <summary>
  /// 延迟加入模块类型。调用时不会创建模块实例。
  /// </summary>
  public void PushModules<T>() where T : BaseModule, new()
  {
    PushModules(typeof(T));
  }

  /// <summary>
  /// 延迟加入多个模块类型。调用时不会创建模块实例。
  /// </summary>
  public void PushModules(params Type[] moduleTypes)
  {
    if (moduleTypes == null)
    {
      throw new ArgumentNullException(nameof(moduleTypes));
    }

    foreach (Type moduleType in moduleTypes)
    {
      if (moduleType == null || !typeof(BaseModule).IsAssignableFrom(moduleType)
          || moduleType.IsAbstract || moduleType.GetConstructor(Type.EmptyTypes) == null)
      {
        throw new ArgumentException(
          $"Module type must be a concrete BaseModule with a parameterless constructor: {moduleType}",
          nameof(moduleTypes));
      }

      bool isCreated = false;
      foreach (BaseModule module in _modules.Values)
      {
        if (module.GetType() == moduleType)
        {
          isCreated = true;
          break;
        }
      }

      if (!isCreated && !_moduleTypes.Contains(moduleType))
      {
        _moduleTypes.Add(moduleType);
      }
    }
  }

  public bool TryGetModule(ModuleName moduleName, out BaseModule module)
  {
    return _modules.TryGetValue(moduleName, out module);
  }

  public T GetModule<T>(ModuleName moduleName) where T : BaseModule
  {
    return TryGetModule(moduleName, out BaseModule module) ? module as T : null;
  }

  public void InitializeModule(ModuleName moduleName)
  {
    if (TryGetModule(moduleName, out BaseModule module))
    {
      module.Initialize(this);
    }
    else
    {
      Debug.LogWarning($"[ModuleManager] Module not found: {moduleName}");
    }
  }

  public void InitializeAll()
  {
    foreach (Type moduleType in _moduleTypes)
    {
      BaseModule module = (BaseModule)Activator.CreateInstance(moduleType);
      RegisterModule(module);
    }
    _moduleTypes.Clear();

    foreach (BaseModule module in _modules.Values)
    {
      module.Initialize(this);
    }
  }

  public void Update()
  {
    foreach (BaseModule module in _modules.Values)
    {
      module.UpdateModule();
    }
  }

  public void ReleaseModule(ModuleName moduleName)
  {
    if (_modules.TryGetValue(moduleName, out BaseModule module))
    {
      module.Release();
      _modules.Remove(moduleName);
    }
  }

  public void ReleaseAll()
  {
    foreach (BaseModule module in _modules.Values)
    {
      module.Release();
    }
    _modules.Clear();
    _moduleTypes.Clear();
  }
}