using UnityEngine;
using Msg;
using Google.Protobuf;

public class TestCodeMonoBehavior : MonoBehaviour
{
  public void OnClickConfirm()
  {
    // UIManager.Instance.OpenWindow<ConfirmPanel2Presenter>("Prefabs/ConfirmPanel2", UILayerIndex.Model, new { title = "提示", desc = "Are you sure? \nAre you close?" });

    UIManager.Instance.OpenWindow<AlertTipsPanelPresenter>("Prefabs/AlertTipsPanel", UILayerIndex.Model, new AlertTipsPanelArgs("警告标题", "警告信息！不允许随便修改！"));
  }

  // 测试 Protobuf 序列化和反序列化
  public void OnTestProtobuf()
  {
    // Example code to test Protobuf serialization and deserialization
    c2s_user_register message = new c2s_user_register
    {
      UserName = "Test Message"
    };

    // Serialize the message to a byte array
    byte[] serializedMessage = message.ToByteArray();

    // Deserialize the byte array back to a message
    c2s_user_register deserializedMessage = c2s_user_register.Parser.ParseFrom(serializedMessage);

    Debug.Log($"Deserialized Message: UserName={deserializedMessage.UserName}");
  }
}