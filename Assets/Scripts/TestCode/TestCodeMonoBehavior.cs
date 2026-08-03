using UnityEngine;

public class TestCodeMonoBehavior : MonoBehaviour
{
  public void OnClickConfirm()
  {
    // UIManager.Instance.OpenWindow<ConfirmPanel2Presenter>("Prefabs/ConfirmPanel2", UILayerIndex.Model, new { title = "提示", desc = "Are you sure? \nAre you close?" });

    UIManager.Instance.OpenWindow<ConfirmPanel3Presenter>("Prefabs/ConfirmPanel3", UILayerIndex.Model, new ConfirmPanel3Args("提示3", "Are you sure? \nAre you close 333?"));
  }
}