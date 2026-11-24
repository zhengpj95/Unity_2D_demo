using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有业务模块的唯一入口。ModuleName 是模块唯一键；模块实例只由此类注册、初始化和释放。
/// </summary>
public sealed class ModuleManager : Singleton<ModuleManager>
{
  private readonly Dictionary<ModuleName, BaseModule> _modules = new();
  private readonly List<Type> _pendingModuleTypes = new();
  // Update 复用快照，避免每帧分配；异常只上报一次，避免持续失败时刷屏。
  private readonly List<BaseModule> _updateSnapshot = new();
  private readonly HashSet<ModuleName> _updateErrorReported = new();

  public bool IsInitialized { get; private set; }
  public int ModuleCount => _modules.Count;

  private ModuleManager() { }

  /// <summary>注册模块实例。管理器已初始化时，模块会立即完成初始化。</summary>
  private void RegisterModule(BaseModule module)
  {
    if (module == null)
    {
      Debug.LogError("[ModuleManager] Cannot register a null Module.");
      return;
    }
    if (module.ModuleName == ModuleName.None)
    {
      Debug.LogError($"[ModuleManager] Module {module.GetType().Name} must define a valid ModuleName.");
      return;
    }
    if (_modules.ContainsKey(module.ModuleName))
    {
      Debug.LogError($"[ModuleManager] Module already registered: {module.ModuleName}.");
      return;
    }

    _modules.Add(module.ModuleName, module);
    if (IsInitialized)
    {
      module.Initialize(this);
      if (!module.IsInitialized)
        Debug.LogError($"[ModuleManager] Failed to initialize registered Module {module.ModuleName}.");
    }
  }

  // private void RegisterModules(params BaseModule[] modules)
  // {
  //   if (modules == null)
  //   {
  //     Debug.LogError("[ModuleManager] Cannot register a null Module array.");
  //     return;
  //   }

  //   foreach (BaseModule module in modules) RegisterModule(module);
  // }

  /// <summary>延迟登记模块类型；将在 InitializeAll 时创建，适合游戏启动阶段集中配置。</summary>
  public void PushModules<T>() where T : BaseModule, new() => PushModules(typeof(T));

  private void PushModules(params Type[] moduleTypes)
  {
    if (moduleTypes == null)
    {
      Debug.LogError("[ModuleManager] Cannot queue a null Module type array.");
      return;
    }

    foreach (Type moduleType in moduleTypes)
    {
      if (!ValidateModuleType(moduleType)) continue;
      if (ContainsModuleType(moduleType) || _pendingModuleTypes.Contains(moduleType)) continue;
      _pendingModuleTypes.Add(moduleType);
    }
  }

  public bool TryGetModule(ModuleName moduleName, out BaseModule module) => _modules.TryGetValue(moduleName, out module);
  public T GetModule<T>(ModuleName moduleName) where T : BaseModule => TryGetModule(moduleName, out BaseModule module) ? module as T : null;

  // private void InitializeModule(ModuleName moduleName)
  // {
  //   if (TryGetModule(moduleName, out BaseModule module))
  //   {
  //     module.Initialize(this);
  //     if (!module.IsInitialized)
  //       Debug.LogError($"[ModuleManager] Failed to initialize Module {moduleName}.");
  //   }
  //   else Debug.LogWarning($"[ModuleManager] Module not found: {moduleName}");
  // }

  /// <summary>创建所有延迟模块，并初始化尚未初始化的模块；可安全重复调用。</summary>
  public void InitializeAll()
  {
    bool allModulesInitialized = true;
    foreach (Type moduleType in _pendingModuleTypes)
    {
      BaseModule module;
      try
      {
        module = Activator.CreateInstance(moduleType) as BaseModule;
      }
      catch (Exception exception)
      {
        Debug.LogError($"[ModuleManager] Failed to create Module {moduleType.Name}. Error={exception}");
        allModulesInitialized = false;
        continue;
      }

      if (module == null)
      {
        Debug.LogError($"[ModuleManager] Failed to create Module {moduleType.Name}.");
        allModulesInitialized = false;
        continue;
      }

      RegisterModule(module);
      if (!_modules.TryGetValue(module.ModuleName, out BaseModule registeredModule)
          || !ReferenceEquals(registeredModule, module))
        allModulesInitialized = false;
    }
    _pendingModuleTypes.Clear();

    foreach (BaseModule module in _modules.Values)
    {
      module.Initialize(this);
      if (module.IsInitialized) continue;

      Debug.LogError($"[ModuleManager] Failed to initialize Module {module.ModuleName}.");
      allModulesInitialized = false;
    }

    IsInitialized = allModulesInitialized;
  }

  /// <summary>
  /// 逐模块驱动 Update。单个模块异常只记录并跳过，避免中断后续模块；
  /// 使用复用快照，避免模块更新期间增删注册表导致遍历失效。
  /// </summary>
  public void Update()
  {
    _updateSnapshot.Clear();
    foreach (BaseModule module in _modules.Values)
      _updateSnapshot.Add(module);

    for (int i = 0; i < _updateSnapshot.Count; i++)
    {
      BaseModule module = _updateSnapshot[i];
      if (module == null) continue;

      try
      {
        module.UpdateModule();
        _updateErrorReported.Remove(module.ModuleName);
      }
      catch (Exception exception)
      {
        if (_updateErrorReported.Add(module.ModuleName))
          Debug.LogError($"[ModuleManager] Failed to update Module {module.ModuleName}. Error={exception}");
      }
    }
  }

  // private void ReleaseModule(ModuleName moduleName)
  // {
  //   if (!_modules.TryGetValue(moduleName, out BaseModule module)) return;
  //   module.Release();
  //   _modules.Remove(moduleName);
  // }

  /// <summary>
  /// 释放所有模块。先快照再逐个释放并隔离异常，保证一个模块失败不会阻断后续模块；
  /// 无论释放是否抛错，最终都会清空注册表并复位状态，避免影响 GameMgr 后续清理链路。
  /// </summary>
  public void ReleaseAll()
  {
    List<BaseModule> modules = null;
    try
    {
      // 释放期间可能通过事件间接改动注册表，先快照再遍历。
      modules = new List<BaseModule>(_modules.Values);
      foreach (BaseModule module in modules)
      {
        if (module == null) continue;
        try
        {
          module.Release();
        }
        catch (Exception exception)
        {
          Debug.LogError($"[ModuleManager] Failed to release Module {module.ModuleName}. Error={exception}");
        }
      }
    }
    finally
    {
      _modules.Clear();
      _pendingModuleTypes.Clear();
      _updateSnapshot.Clear();
      _updateErrorReported.Clear();
      IsInitialized = false;
    }
  }

  private bool ContainsModuleType(Type moduleType)
  {
    foreach (BaseModule module in _modules.Values)
      if (module.GetType() == moduleType) return true;
    return false;
  }

  private static bool ValidateModuleType(Type moduleType)
  {
    if (moduleType == null || !typeof(BaseModule).IsAssignableFrom(moduleType) || moduleType.IsAbstract || moduleType.GetConstructor(Type.EmptyTypes) == null)
    {
      Debug.LogError($"[ModuleManager] Module type must be a concrete BaseModule with a parameterless constructor: {moduleType}.");
      return false;
    }

    return true;
  }
}
