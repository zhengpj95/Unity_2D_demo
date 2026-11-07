using UnityEngine;

public sealed class OpenConfirmCmd : BaseCommand
{
  public override async void Execute(EventContext args)
  {
    if (!args.TryGetData(out ConfirmPanelArgs confirmArgs))
      return;

    if (Module is MiscModule miscModule)
    {
      await miscModule.OpenConfirm(confirmArgs);
    }
  }
}