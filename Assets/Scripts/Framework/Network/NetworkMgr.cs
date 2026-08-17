using UnityEngine;
using System.Threading.Tasks;
using NativeWebSocket;
using System;
using Google.Protobuf;
using Msg;

public class NetworkMgr : Singleton<NetworkMgr>
{
  private SocketMgr _socketMgr;
  private MessageDispatcher _dispatcher;

  public void s2cUserLogin(s2c_user_login data)
  {
    Debug.Log($"s2c_user_login: {data}");
  }

  public async Task Connect(string url)
  {
    ProtoRegister.RegisterAll(); // 注册解析 TODO 暂时
    _socketMgr = new SocketMgr();
    _dispatcher = new MessageDispatcher();
    _dispatcher.Register<s2c_user_login>(MessageId.S2C_USER_LOGIN, s2cUserLogin);
    _socketMgr.OnMessage += ReceiveMessage;
    await _socketMgr.Connect(url);
  }

  public async Task Send<T>(uint cmd, T message) where T : IMessage
  {
    if (_socketMgr == null)
    {
      return;
    }
    byte[] body = ProtoMgr.Encode(message);
    byte[] packet = PacketCodec.Encode(cmd, body);
    Debug.Log("Send -- " + cmd + " -- " + message);
    await _socketMgr.Send(packet);
  }

  public void ReceiveMessage(byte[] data)
  {
    Packet packet = PacketCodec.Decode(data);
    Debug.Log($"Received Packet: Cmd={packet.Cmd}, BodyLength={packet.Body.Length}");
    IMessage message = ProtoMgr.Decode(packet.Cmd, packet.Body);
    Debug.Log($"Received message: {message}");
    _dispatcher.Dispatch(packet.Cmd, message);
  }

  public async Task Close()
  {
    await _socketMgr.Close();
  }

  public void Dispose()
  {
    _socketMgr.Dispose();
    _socketMgr = null;
  }
}