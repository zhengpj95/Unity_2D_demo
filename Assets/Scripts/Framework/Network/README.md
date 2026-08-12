# Network 模块说明

这个模块主要负责 Unity 客户端与服务端之间的网络连接、消息发送与接收，以及 protobuf 协议包的编解码与分发。

## 1. 模块职责

当前的 Network 目录中，核心逻辑主要分为两层：

- 底层 Socket 层：负责 WebSocket 连接、状态管理和原始数据收发。
- 上层协议层：负责打包/解包、消息分发和业务处理。

## 2. 关键文件

### 2.1 SocketMgr.cs

`SocketMgr` 是底层 WebSocket 管理器，基于 `NativeWebSocket` 实现。

主要功能：

- 建立连接：`Connect(string url)`
- 发送数据：`Send(byte[] data)`
- 关闭连接：`Close()`
- 状态管理：`SocketState`
- 事件通知：`OnConnected`、`OnMessage`、`OnError`、`OnClosed`

状态枚举：

- `None`
- `Connecting`
- `Connected`
- `Closing`
- `Closed`
- `Error`

实现特点：

- 使用事件回调处理 WebSocket 的 open / message / error / close。
- `IsConnected` 会判断 `_socket.State == WebSocketState.Open`。
- `Dispose()` 会清理事件绑定和状态，避免连接对象残留。

这是整个网络层的基础设施，负责把底层 socket 的生命周期封装起来，便于上层直接调用。

### 2.2 NetworkMgr.cs

`NetworkMgr` 是业务网络入口，继承自 `Singleton<NetworkMgr>`，作为全局网络管理器使用。

主要功能：

- 连接服务器：`Connect(string url)`
- 发送 protobuf 消息：`Send<T>(uint cmd, T message)`
- 接收消息并解包：`ReceiveMessage(byte[] data)`
- 关闭连接：`Close()`
- 释放资源：`Dispose()`

其流程大致为：

1. 调用 `ProtoRegister.RegisterAll()` 注册协议类型。
2. 创建 `SocketMgr` 和 `MessageDispatcher`。
3. 注册消息处理器：`_dispatcher.Register<s2c_user_login>(Cmd.S2C_USER_LOGIN, s2cUserLogin)`。
4. 监听 `_socketMgr.OnMessage`，收到消息后调用 `ReceiveMessage()`。
5. `ReceiveMessage()` 调用 `PacketCodec.Decode(data)` 解析数据包。
6. 使用 `ProtoMgr.Decode(packet.Cmd, packet.Body)` 反序列化消息体。
7. 通过 `_dispatcher.Dispatch(packet.Cmd, message)` 分发给对应业务处理函数。

## 3. 业务处理链路

当前代码的典型处理链路如下：

- 客户端调用 `NetworkMgr.Send(cmd, message)`
- `message` 先经过 `ProtoMgr.Encode()` 编码
- 再经过 `PacketCodec.Encode(cmd, body)` 组装成完整 packet
- 通过 `SocketMgr.Send(packet)` 发送到服务器
- 服务器返回数据后，`SocketMgr` 触发 `OnMessage`
- `NetworkMgr.ReceiveMessage()` 解包并分发给消息处理器

```shell
                Server
                  │
                  │ WebSocket
                  ▼
            SocketMgr
                  │
                byte[]
                  │
                  ▼
             PacketCodec
                  │
              ┌───┴───┐
              │       │
             Cmd     Body
              │       │
              └───┬───┘
                  ▼
               ProtoMgr
                  │
                  ▼
            IMessage
                  │
                  ▼
        MessageDispatcher
                  │
          ┌───────┼────────┐
          ▼       ▼        ▼
      LoginHandler UserHandler BagHandler
```

## 4. 当前实现的特点

- 采用 `WebSocket` 作为传输协议，适合实时游戏通信。
- 采用 `protobuf` 作为消息序列化格式，利于体积较小、解析效率较高。
- 通过 `cmd` 号作为消息路由标识，支持不同业务消息分发。
- 使用 `MessageDispatcher` 统一管理消息回调，减少业务代码耦合。

## 5. 现状与注意事项

当前代码中使用了这几个重要组件：

- `PacketCodec`：负责封包/拆包
- `ProtoMgr`：负责 protobuf 编解码
- `MessageDispatcher`：负责消息分发
- `Cmd`：协议命令枚举/常量

代码结构比较清晰，适合继续扩展更多网络消息类型。当前的示例逻辑里，`s2c_user_login` 已经接入了分发处理，说明该网络层具备较好的扩展基础。

## 6. 典型用法

在实际业务中，通常是这样使用：

- 先调用 `NetworkMgr.Instance.Connect(url)` 建立连接
- 注册消息监听器
- 调用 `NetworkMgr.Instance.Send(cmd, protoMessage)` 发送消息
- 在对应回调中处理服务端返回

## 7. 总结

这个网络模块已经具备：

- 连接管理
- 消息发送
- 消息接收
- 协议解码
- 消息分发

的基础能力，是一个适合游戏客户端扩展的标准网络层骨架。后续如果继续开发，可在此基础上增加：

- 心跳机制
- 重连机制
- 超时处理
- 断线重连通知
- 更完善的错误日志和状态回调

