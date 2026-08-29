public class SurvivorModule : BaseModule
{
  public override ModuleName ModuleName => ModuleName.Survivor;

  private const string SurvivorMainPrefabPath = "Prefabs/SurvivorMain";

  /// <summary>
  /// 打开幸存者主界面，并将 Presenter 注册、持有在当前模块中。
  /// </summary>
  public SurvivorMainPresenter OpenSurvivorMain()
  {
    SurvivorMainPresenter presenter = GetPresenter<SurvivorMainPresenter>();
    return presenter ?? OpenWindow<SurvivorMainPresenter>(SurvivorMainPrefabPath, UILayerIndex.Window);
  }
}
