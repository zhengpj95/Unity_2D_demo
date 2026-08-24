
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

    if (!UIManager.IsCreated || !UIManager.Instance.IsInitialized)
    {
      Debug.LogWarning("[OpenAlertTipsCmd] UIManager is not initialized.");
      return;
    }

    UIManager.Instance.OpenWindow<AlertTipsPanelPresenter>(
      "Prefabs/AlertTipsPanel",
      UILayerIndex.Model,
      alertArgs);
  }
}
