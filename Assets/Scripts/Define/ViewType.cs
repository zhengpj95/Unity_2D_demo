using System;

/// <summary>Misc 模块的界面类型。</summary>
public enum MiscViewType
{
  AlertTips
}

/// <summary>Survivor 模块的界面类型。</summary>
public enum SurvivorViewType
{
  Main,
  SkillSelect,
  GameOver,
}

/// <summary>
/// UI 实例的全局身份，由所属模块和模块内 ViewType 组成。
/// </summary>
public readonly struct ModuleViewKey : IEquatable<ModuleViewKey>
{
  public ModuleName ModuleName { get; }
  public Enum ViewType { get; }

  public ModuleViewKey(ModuleName moduleName, Enum viewType)
  {
    if (moduleName == ModuleName.None)
      throw new ArgumentException("ModuleName cannot be None.", nameof(moduleName));

    ModuleName = moduleName;
    ViewType = viewType ?? throw new ArgumentNullException(nameof(viewType));
  }

  public bool Equals(ModuleViewKey other)
  {
    return ModuleName == other.ModuleName && Equals(ViewType, other.ViewType);
  }

  public override bool Equals(object obj)
  {
    return obj is ModuleViewKey other && Equals(other);
  }

  public override int GetHashCode()
  {
    unchecked
    {
      return ((int)ModuleName * 397) ^ (ViewType?.GetHashCode() ?? 0);
    }
  }

  public override string ToString()
  {
    return $"{ModuleName}.{ViewType}";
  }
}
