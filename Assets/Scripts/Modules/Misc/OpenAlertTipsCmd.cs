
using UnityEngine;

public sealed class OpenAlertTipsCmd : BaseCommand
{
  public override void Execute(EventContext context)
  {
    if (!context.TryGetData(out AlertTipsPanelArgs alertArgs))
    {
      Debug.LogWarning("[OpenAlertTipsCmd] Invalid AlertTipsPanelArgs.");
      return;
    }

    if (Module is MiscModule miscModule)
      miscModule.OpenAlert(alertArgs);
  }
}
