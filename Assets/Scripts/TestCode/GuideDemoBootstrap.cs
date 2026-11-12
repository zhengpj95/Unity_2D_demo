using UnityEngine;

/// <summary>GuideDemo 场景入口；通过 GuideModule 打开 Presenter 并启动样例配置。</summary>
public sealed class GuideDemoBootstrap : MonoBehaviour
{
  [SerializeField] private GuideConfig guideConfig;

  private async void Start()
  {
    if (guideConfig == null)
    {
      Debug.LogError("[GuideDemoBootstrap] Assign GuideConfig in the scene.", this);
      return;
    }

    if (!ModuleManager.IsCreated || !ModuleManager.Instance.IsInitialized)
    {
      Debug.LogError("[GuideDemoBootstrap] GameMgr must initialize ModuleManager before the demo starts.", this);
      return;
    }

    GuideModule guideModule = ModuleManager.Instance.GetModule<GuideModule>(ModuleName.Guide);
    if (guideModule == null)
    {
      Debug.LogError("[GuideDemoBootstrap] GuideModule is not registered in ModuleManager.", this);
      return;
    }

    if (!await guideModule.StartGuide(guideConfig))
      Debug.LogError("[GuideDemoBootstrap] Failed to start the sample guide.", this);
  }
}
