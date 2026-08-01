using UnityEngine;

public class TestCodeMonoBehavior : MonoBehaviour
{
  public void OnClickConfirm()
  {
    UIManager.Instance.OpenWindow<UIConfirmPanelPresenter>("Prefabs/ConfirmPanel", UILayerIndex.Model, new { title = "提示", desc = "Are you sure?" });
  }
}