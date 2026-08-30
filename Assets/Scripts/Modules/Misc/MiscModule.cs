
public sealed class MiscModule : BaseModule
{
  public override ModuleName ModuleName => ModuleName.Misc;

  protected override void OnInit()
  {
    RegPresenter<AlertTipsPanelPresenter>(MiscViewType.AlertTips);
    RegCmd<OpenAlertTipsCmd>(UIEventDefine.MISC_OPEN_ALERT);
  }

  public AlertTipsPanelPresenter OpenAlert(AlertTipsPanelArgs args)
  {
    return OpenWindow<AlertTipsPanelPresenter>(MiscViewType.AlertTips, args);
  }
}
