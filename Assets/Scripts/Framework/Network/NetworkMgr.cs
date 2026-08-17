using UnityEngine;
using System.Threading.Tasks;
using System;
using Google.Protobuf;
using System.Collections.Generic;

public class NetworkMgr : Singleton<NetworkMgr>
{
  private SocketMgr _socketMgr;
  private MessageDispatcher _dispatcher;
  private readonly Dictionary<string, IMessageModule> _modules = new();
  private readonly List<Action> _pendingRegistrations = new();

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

  public void RegisterModule(IMessageModule module)
  {
    if (module == null)
    {
      throw new ArgumentNullException(nameof(module));
    }

    if (_modules.ContainsKey(module.ModuleName))
    {
      throw new InvalidOperationException($"Module already registered: {module.ModuleName}");
    }

    _modules[module.ModuleName] = module;

    if (_dispatcher == null)
    {
      _pendingRegistrations.Add(() => module.Register(this));
      return;
    }

    module.Register(this);
  }

  public void RegisterHandler<T>(uint cmd, Action<T> handler) where T : IMessage<T>
  {
    if (handler == null)
    {
      throw new ArgumentNullException(nameof(handler));
    }

    if (_dispatcher == null)
    {
      _pendingRegistrations.Add(() =>
      {
        _dispatcher.Register(cmd, handler);
        MessageCommandTable.Register(cmd, "Unspecified");
      });
      return;
    }

    _dispatcher.Register(cmd, handler);
    MessageCommandTable.Register(cmd, "Unspecified");
  }

  public void RegisterHandler<T>(string moduleName, uint cmd, Action<T> handler) where T : IMessage<T>
  {
    if (string.IsNullOrWhiteSpace(moduleName))
    {
      throw new ArgumentException("Module name cannot be empty.", nameof(moduleName));
    }

    MessageCommandTable.Register(cmd, moduleName);
    RegisterHandler(cmd, handler);
  }

  public bool UnregisterHandler(uint cmd)
  {
    return _dispatcher != null && _dispatcher.Unregister(cmd);
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