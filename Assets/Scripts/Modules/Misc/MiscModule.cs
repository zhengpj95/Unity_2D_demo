
public sealed class MiscModule : BaseModule
{
  public override ModuleName ModuleName => ModuleName.Misc;

  protected override void OnInit()
  {
    RegCmd<OpenAlertTipsCmd>(UIEventDefine.MISC_OPEN_ALERT);
  }
}
