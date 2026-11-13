using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

/// <summary>根据 PresenterId 与相对层级路径解析可点击的 UI 按钮。</summary>
public static class GuidePathFinder
{
  /// <summary>按 Presenter 可见性、路径和按钮组件报告目标解析结果。</summary>
  public enum FindResult
  {
    Found,
    InvalidPresenterId,
    PresenterUnavailable,
    InvalidTargetPath,
    TargetNotFound,
    TargetInactive,
    MissingRectTransform,
    MissingButton,
    ButtonUnavailable
  }

  /// <summary>
  /// 在指定 Presenter 的 View 下查找带 UIButton 或 Unity Button 的目标节点。
  /// PresenterId 格式为 ModuleName 四位数字、下划线、ViewType 一位或两位数字。
  /// </summary>
  /// <param name="presenterId">Presenter 标识，例如 Misc/Confirm 对应 1002_2。</param>
  /// <param name="targetPath">相对于 Presenter View 根节点的 Transform 路径。</param>
  /// <param name="target">成功时返回可点击按钮组件，否则为 null。</param>
  /// <returns>目标解析状态。</returns>
  public static FindResult FindTarget(string presenterId, string targetPath, out Component target)
  {
    target = null;
    if (!TryParsePresenterId(presenterId, out _, out BaseModule module, out Enum viewType))
    {
      return FindResult.InvalidPresenterId;
    }

    BasePresenter presenter = module.GetPresenter(viewType);
    if (presenter == null
        || !presenter.IsVisible
        || presenter.View == null
        || !presenter.View.gameObject.activeInHierarchy)
    {
      return FindResult.PresenterUnavailable;
    }

    if (string.IsNullOrWhiteSpace(targetPath))
      return FindResult.InvalidTargetPath;

    Transform targetTransform = presenter.View.transform.Find(targetPath);
    if (targetTransform == null)
      return FindResult.TargetNotFound;
    if (!targetTransform.gameObject.activeInHierarchy)
      return FindResult.TargetInactive;
    if (!(targetTransform is RectTransform))
      return FindResult.MissingRectTransform;

    UIButton uiButton = targetTransform.GetComponent<UIButton>();
    if (uiButton != null && uiButton.isActiveAndEnabled)
    {
      target = uiButton;
      return FindResult.Found;
    }

    Button button = targetTransform.GetComponent<Button>();
    if (button != null && button.isActiveAndEnabled && button.IsInteractable())
    {
      target = button;
      return FindResult.Found;
    }

    return uiButton != null || button != null
      ? FindResult.ButtonUnavailable
      : FindResult.MissingButton;
  }

  /// <summary>判断 Presenter 生命周期事件是否对应指定的 PresenterId。</summary>
  /// <param name="presenterId">待比较的四位模块码与一位或两位界面码。</param>
  /// <param name="moduleName">事件中的模块标识。</param>
  /// <param name="viewType">事件中的模块界面类型。</param>
  /// <returns>标识格式有效且模块、界面类型均匹配时返回 true。</returns>
  public static bool MatchesPresenter(string presenterId, ModuleName moduleName, Enum viewType)
  {
    return TryParsePresenterId(presenterId, out ModuleName parsedModule, out _, out Enum parsedView)
      && parsedModule == moduleName
      && Equals(parsedView, viewType);
  }

  private static bool TryParsePresenterId(
    string presenterId,
    out ModuleName moduleName,
    out BaseModule module,
    out Enum viewType)
  {
    moduleName = ModuleName.None;
    module = null;
    viewType = null;
    if (presenterId == null
        || (presenterId.Length != 6 && presenterId.Length != 7)
        || presenterId[4] != '_'
        || !int.TryParse(presenterId.Substring(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out int moduleCode)
        || !int.TryParse(presenterId.Substring(5), NumberStyles.None, CultureInfo.InvariantCulture, out int viewCode)
        || !Enum.IsDefined(typeof(ModuleName), moduleCode))
    {
      return false;
    }

    moduleName = (ModuleName)moduleCode;
    if (!ModuleManager.Instance.TryGetModule(moduleName, out module)
        || !module.TryGetRegisteredViewType(viewCode, out viewType))
    {
      module = null;
      return false;
    }

    return true;
  }
}
