using UnityEngine;
using System.Threading.Tasks;
using System;
using Google.Protobuf;
using System.Collections.Generic;

public class NetworkMgr : Singleton<NetworkMgr>
{
  private SocketMgr _socketMgr;
  private MessageDispatcher _dispatcher;
  private readonly List<Action> _pendingRegistrations = new();
  private readonly Dictionary<uint, int> _commandVersions = new();

  private void FlushPendingRegistrations()
  {
    if (_dispatcher == null)
    {
      return;
    }

    foreach (Action registration in _pendingRegistrations)
    {
      registration();
    }

    _pendingRegistrations.Clear();
  }

  public void RegisterHandler<T>(uint cmd, Action<T> handler) where T : IMessage<T>
  {
    if (handler == null)
    {
      throw new ArgumentNullException(nameof(handler));
    }

    int commandVersion = GetNextCommandVersion(cmd);

    if (_dispatcher == null)
    {
      _pendingRegistrations.Add(() =>
      {
        if (IsCurrentCommandVersion(cmd, commandVersion))
        {
          _dispatcher.Register(cmd, handler);
        }
      });
      return;
    }

    _dispatcher.Register(cmd, handler);
  }

  public bool UnregisterHandler(uint cmd)
  {
    GetNextCommandVersion(cmd);
    return _dispatcher != null && _dispatcher.Unregister(cmd);
  }

  private int GetNextCommandVersion(uint cmd)
  {
    int version = _commandVersions.TryGetValue(cmd, out int currentVersion)
      ? currentVersion + 1
      : 1;
    _commandVersions[cmd] = version;
    return version;
  }

  private bool IsCurrentCommandVersion(uint cmd, int version)
  {
    return _commandVersions.TryGetValue(cmd, out int currentVersion)
      && currentVersion == version;
  }

  public async Task Connect(string url)
  {
    ProtoRegister.RegisterAll(); // 注册解析 TODO 暂时
    _socketMgr = new SocketMgr();
    _dispatcher = new MessageDispatcher();
    FlushPendingRegistrations();
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
    Debug.Log("[Client] Send -- " + cmd + " -- " + message);
    await _socketMgr.Send(packet);
  }

  public void ReceiveMessage(byte[] data)
  {
    Packet packet = PacketCodec.Decode(data);
    IMessage message = ProtoMgr.Decode(packet.Cmd, packet.Body);
    Debug.Log($"Received Cmd={packet.Cmd}, message: {message}");
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
