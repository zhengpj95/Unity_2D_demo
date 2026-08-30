
using UnityEngine;

public sealed class OpenAlertTipsCmd : BaseCommand
{
  public override void Execute(object args = null)
  {
    if (!(args is AlertTipsPanelArgs alertArgs))
    {
      Debug.LogWarning("[OpenAlertTipsCmd] Invalid AlertTipsPanelArgs.");
      return;
    }

    if (Module is MiscModule miscModule)
      miscModule.OpenAlert(alertArgs);
  }
}
