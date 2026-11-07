
using UnityEngine;

public sealed class OpenAlertTipsCmd : BaseCommand
{
  public override async void Execute(EventContext context)
  {
    if (!context.TryGetData(out AlertTipsPanelArgs alertArgs))
    {
      Debug.LogWarning("[OpenAlertTipsCmd] Invalid AlertTipsPanelArgs.");
      return;
    }

    if (Module is MiscModule miscModule)
    {
      try
      {
        await miscModule.OpenAlert(alertArgs);
      }
      catch (System.Exception exception)
      {
        Debug.LogError($"[OpenAlertTipsCmd] Failed to open alert UI: {exception}");
      }
    }
  }
}
