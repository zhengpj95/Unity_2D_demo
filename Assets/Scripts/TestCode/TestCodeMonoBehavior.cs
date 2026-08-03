using UnityEngine;

public class TestCodeMonoBehavior : MonoBehaviour
{
  public void OnClickConfirm()
  {
    // UIManager.Instance.OpenWindow<ConfirmPanel2Presenter>("Prefabs/ConfirmPanel2", UILayerIndex.Model, new { title = "提示", desc = "Are you sure? \nAre you close?" });

    UIManager.Instance.OpenWindow<AlertTipsPanelPresenter>("Prefabs/AlertTipsPanel", UILayerIndex.Model, new AlertTipsPanelArgs("警告标题", "警告信息！不允许随便修改！"));
  }
}