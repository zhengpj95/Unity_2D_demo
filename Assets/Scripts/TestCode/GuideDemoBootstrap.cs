using UnityEngine;

/// <summary>GuideDemo 场景入口；绑定 GuideView 并启动 Inspector 指定的样例配置。</summary>
public sealed class GuideDemoBootstrap : MonoBehaviour
{
  [SerializeField] private GuideView guideView;
  [SerializeField] private GuideConfig guideConfig;

  private void Start()
  {
    if (guideView == null || guideConfig == null)
    {
      Debug.LogError("[GuideDemoBootstrap] Assign GuideView and GuideConfig in the scene.", this);
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

    guideView.Bind(guideModule);
    if (!guideModule.StartGuide(guideConfig))
    {
      guideView.Unbind();
      Debug.LogError("[GuideDemoBootstrap] The sample GuideConfig is invalid.", this);
    }
  }

  private void OnDestroy()
  {
    if (guideView != null)
      guideView.Unbind();
  }
}
