using System;
/// <summary>描述一个虚拟页签及其要打开的 Module Presenter。</summary>
public sealed class ListTabItemData
{
  /// <summary>页签稳定唯一 ID，用于列表选中状态恢复。</summary>
  public int Id { get; }
  /// <summary>显示名称。</summary>
  public string Name { get; }
  /// <summary>目标业务模块。</summary>
  public ModuleName ModuleName { get; }
  /// <summary>目标模块登记的 ViewType。</summary>
  public Enum ViewType { get; }
  /// <summary>可选图标资源键，由 ListTab 通过 AssetLoader 加载。</summary>
  public string IconPath { get; }
  /// <summary>留给上层业务分发的功能编号。</summary>
  public int FuncId { get; }
  /// <summary>传递给目标 Presenter.OnOpen 的扩展参数。</summary>
  public object ExtraData { get; }

  /// <summary>创建页签定义；目标 ViewType 必须是对应模块已登记的枚举值。</summary>
  /// <param name="id">页签稳定唯一 ID。</param>
  /// <param name="name">显示名称。</param>
  /// <param name="moduleName">目标模块名称，不能为 None。</param>
  /// <param name="viewType">目标模块的 ViewType。</param>
  /// <param name="iconPath">可选图标资源键；为空时不显示图标。</param>
  /// <param name="funcId">可选功能编号。</param>
  /// <param name="extraData">传给目标 Presenter 的运行时参数。</param>
  public ListTabItemData(
    int id,
    string name,
    ModuleName moduleName,
    Enum viewType,
    string iconPath = null,
    int funcId = 0,
    object extraData = null)
  {
    if (moduleName == ModuleName.None) throw new ArgumentException("ModuleName cannot be None.", nameof(moduleName));
    if (viewType == null) throw new ArgumentNullException(nameof(viewType));

    Id = id;
    Name = name ?? string.Empty;
    ModuleName = moduleName;
    ViewType = viewType;
    IconPath = iconPath;
    FuncId = funcId;
    ExtraData = extraData;
  }
}
