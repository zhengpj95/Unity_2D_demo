using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 统一管理所有业务模块，并以 ModuleName 保证模块唯一。
/// </summary>
public sealed class ModuleManager : Singleton<ModuleManager>
{
  private readonly Dictionary<ModuleName, BaseModule> _modules = new();

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
  }
}