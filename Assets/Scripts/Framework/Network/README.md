# Network 模块说明

## 0. 当前状态、已知限制与后续规划

当前已实现连接状态 `ConnectionState` / `ConnectionStateChanged`、基础固定间隔重连，以及首次连接失败后的重连。`GameMgr.EnableSocketConnection` 是开发期代码开关，默认关闭时不会主动建立 Socket，本地服务端未启动也可运行客户端流程。

以下内容按优先级记录，尚未实现：

1. **收包边界保护（P0）**：`ReceiveMessage` 尚未隔离 Packet、Proto 解析和业务 Handler 异常；需要增加最大包体限制、异常日志（cmd、包长）与单包失败后的继续收包策略。
2. **连接任务取消与地址切换策略（P1）**：同一地址的并发 `Connect` 已合并，旧 Socket 会先关闭再释放；当前地址切换要求先 `Close()`，后续可按业务需要补充取消令牌或受控切换策略。
3. **发送结果与请求超时（已实现基础版本）**：`Send` 返回 `NetworkSendResult`；`Request` 可等待指定 responseCmd 并超时。`MessageId` 表示请求/响应的业务协议类型，同一响应协议应由唯一职责方处理，因此同一 responseCmd 仅允许一个等待请求。
4. **框架与业务解耦（已处理）**：`NetworkMgr` 仅上报 `ConnectionFailed`，通用提示与重载场景逻辑已迁移到 `MiscModule`。
5. **重连策略完善（P2）**：当前为固定间隔重试；后续可增加指数退避、随机抖动和取消令牌，避免大量客户端同时重连。
6. **心跳与平台验证（P2）**：需要加入心跳/超时检测，并按目标平台验证 NativeWebSocket 的消息队列驱动与主线程回调要求。
7. **可观测性与测试（P2）**：当前连接、重连、收发原始字节、协议编解码、分发、关闭与异常均已输出 `[SocketMgr]` / `[NetworkMgr]` 调试日志（记录 URL、状态、cmd、消息类型和字节长度，不输出完整协议内容）。后续补充连接次数、重连次数、收发包量、失败原因统计，以及 PacketCodec、ProtoMgr、Dispatcher 和重连状态机的自动化测试。

服务端地址当前处于本地测试阶段，保留 `ws://localhost:3000`。暂不引入多环境配置；准备联调或发布时，再将地址抽取为明确的环境配置即可。

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
- 连接状态：`ConnectionState` 与 `ConnectionStateChanged`
- 发送 protobuf 消息：`Send<T>(uint cmd, T message)`，返回 `NetworkSendResult`
- 请求响应：`Request<TRequest, TResponse>(...)`，按 responseCmd 等待一次响应并支持超时
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

### 协议源文件与生成代码

- 协议源文件位于 `Assets/Configs/message.proto`，它是登录、注册与配置消息的定义来源。
- 运行时不读取 `.proto` 文件；客户端实际编译和注册的是由 `protoc` 生成的 `Assets/Scripts/Define/Proto/Message.cs`。
- 修改 `message.proto` 后，必须重新生成 `Message.cs`，再同步检查 `MessageID.cs` 与 `ProtoRegister.cs` 的命令号和 Parser 注册是否一致。
- 移动协议源文件时必须同时移动其 `.meta`，以保留 Unity GUID；不要复制已有 `.meta` 创建新协议文件。

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
- 指数退避的重连策略
- 业务层基于 `ConnectionStateChanged` 的断线重连交互
- 更完善的错误日志、指标与自动化测试

