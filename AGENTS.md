# AGENTS.md

## 项目定位

这是一个 Unity + C# 游戏项目。修改代码前先理解现有架构，不要为了完成一个小需求大范围重写项目。

当前项目架构事实与主要调用关系记录在 `Docs/Architecture.md`。处理架构、Module、UI、Network、资源或全局生命周期相关任务时，应先阅读该文档，再阅读目标代码。

### 项目事实源

判断“项目当前已经实现了什么”时，按以下优先级取证：

1. 当前分支中的实际代码与 `ProjectSettings/ProjectVersion.txt`。
2. `Docs/Architecture.md`。
3. `Assets/Scripts/Framework/**/README.md` 等局部框架文档。
4. `.codex/skills/**` 中的任务型操作规范。
5. 本文件中的开发规则。
6. TODO、issue、聊天记录和未来规划。

不要把计划中的类、目录或架构当作现有实现。文档和代码冲突时，以代码为准，并在架构发生实质变化时同步更新 `Docs/Architecture.md`。

## 基本原则

- 优先保持现有代码风格、目录结构、命名和生命周期约定。
- 先分析，再修改；涉及多个文件时先给出简短计划。
- 只修改完成任务所需的文件，不要改动无关代码、场景、Prefab 或配置。
- 不要凭空创建第三方依赖；新增依赖前说明原因和替代方案。
- 修改完成后检查 diff，并尽可能运行相关编译、测试或静态检查。
- 不确定业务规则时先从调用方、已有实现和文档取证；只有仓库无法消除关键歧义时才向用户提问。
- 新增通用 Manager、Service、Bus 或基础设施前，先搜索是否已有同职责实现，避免重复系统。

## Unity 约定

- 目标 Unity 版本以项目的 `ProjectSettings/ProjectVersion.txt` 为准。
- Unity 生命周期必须明确：`Awake` 负责引用和基础初始化，`OnEnable`/`OnDisable` 负责事件订阅，`OnDestroy` 负责释放资源。
- 避免在 `Update` 中创建临时对象、字符串拼接或 LINQ；高频路径优先考虑缓存和对象复用。
- 不要在运行时随意修改 Prefab 资源本身；运行时实例和编辑器资源要区分处理。
- UI 对象的层级、Canvas、粒子特效和排序问题，应从渲染顺序与父子节点关系分析，不要用随机加大的 sorting order 修补。
- 修改序列化字段时考虑已有场景和 Prefab 的兼容性；不要轻易重命名或删除字段。必要时使用 `FormerlySerializedAs`。
- 协程、Tween、事件和异步任务必须在对象销毁或界面关闭时停止/解绑，避免回调访问已销毁对象。
- Unity API 调用必须尽量发生在主线程。
- 不要手工修改 `Library/`、`Temp/`、`Logs/`、`obj/` 等生成目录。
- 新增、移动或删除 Unity Asset 时注意对应 `.meta` 文件；不要无意中造成 GUID 变化。
- 除非任务确实需要，避免直接大范围编辑 `.unity`、`.prefab` 等 YAML 文件。

## C# 代码规范

- 类型、方法、属性和公开成员使用 `PascalCase`；私有字段使用项目现有风格，默认采用 `_camelCase`。
- 一个类只承担清晰的职责；避免 God class、静态全局状态和隐藏依赖。
- 能使用接口表达依赖时，不直接依赖具体实现；避免模块之间互相持有具体类型造成循环依赖。
- 优先使用明确的类型和不可变数据；不要为了“灵活”滥用 `object`、反射或字符串查找。
- 公共 API 写清楚参数、返回值、生命周期和异常/失败行为。
- 不要捕获异常后静默忽略；日志要包含模块、操作和关键标识。
- 不要为了省几行代码使用难以阅读的表达式或过度泛型化。

## UI 架构

- UI 遵循 `UIManager -> Presenter -> View` 的职责划分。
- View 负责 Unity 组件和显示状态；Presenter 负责界面逻辑、状态转换和事件协调；UIManager 负责创建、打开、关闭和销毁界面。
- 优先使用“非泛型基类 + 泛型 Presenter + MonoBehaviour View”模式，避免让 Unity Inspector 直接处理复杂泛型组件。
- `OnOpen` 参数必须明确类型和生命周期；关闭界面时清理监听、Tween、协程和临时数据。
- UI 事件不要直接把业务逻辑塞进 Button 回调；统一转交 Presenter 或 Command。
- 当前 UI 资源加载方式、层级与窗口缓存语义以 `UIManager.cs` 的真实实现为准，不要把计划中的 Addressables/AssetBundle 方案当作已经落地。

## 业务模块

- 每个业务域（如 Bag、Chat、Shop、User、Network）原则上独立成 Module；但新增前必须先检查仓库现有模块划分，不要仅按示例名字创建空模块。
- Module 继承统一的 `BaseModule`，内部管理本模块的 Presenter、Proxy、Command 和事件订阅。
- `ModuleManager` 负责模块创建、初始化、启动、关闭和释放；模块之间通过接口、事件或明确的服务依赖通信。
- 使用优先级、常驻模块和显式依赖解决加载顺序，不要在模块内部偷偷查找并初始化其他模块。
- 禁止循环依赖。若两个模块互相依赖，应提取稳定的接口或上移到共享服务层。
- MVC 业务开发可参考 `.codex/skills/unity-mvc-development/SKILL.md`，但非 MVC 工作不要强行套用该 Skill。


## Survivor 模块开发

修改 `Assets/Scripts/Modules/Vampire Survivors-like/` 前，必须先阅读：

1. `Docs/Architecture.md`
2. `Docs/Modules/Survivor.md`
3. `.codex/skills/unity-mvc-development/SKILL.md`
4. 目标 Module、`BaseModule`、`BaseProxy` 与真实调用方

Survivor 的运行时数据必须由 `SurvivorModel` 保存、由 `SurvivorProxy` 持有和修改。Presenter / View 只负责展示和交互；技能选择等 UI 通过回调交给 `SurvivorGameplayController` 编排，不直接调用武器逻辑或修改 `Time.timeScale`。

## 网络与 Protobuf

- 网络层按 `Network / Packet / Proto / Dispatcher` 的职责分层：连接管理、封包解包、协议编解码、消息分发各自负责；实际文件组织以当前 `Assets/Scripts/Framework/Network` 为准。
- Packet 的 `cmd` 使用项目统一的无符号整数类型；不要在不同协议中混用宽度。
- 收包流程必须校验长度、cmd、协议类型和解码结果；异常包不能导致主循环或连接状态崩溃。
- Proto 注册优先使用自动生成代码或统一注册表，避免手写大量易遗漏的注册语句。
- 网络回调不要直接操作 UI；通过 Module、Proxy、事件或消息总线转交业务层。

## 性能与内存

- 关注 GC Alloc、Instantiate/Destroy、GetComponent、Find、反射、字符串拼接和 LINQ 在高频路径中的使用。
- UI、特效和网络对象优先考虑缓存、对象池和批量更新，但不要为了理论性能引入无法维护的复杂度。
- 材质、Shader、TMP 和 UI 组件的运行时实例化要明确所有权和释放策略，避免无意中为每个对象创建独立 Material。

## 架构变更规则

出现以下变化之一时，在同一次提交中检查并更新 `Docs/Architecture.md`：

- 新增或移除项目级 Manager / Service；
- 修改 `GameMgr` 启动或释放链路；
- 修改 Module 生命周期、职责或跨模块依赖规则；
- 修改 UIManager / Presenter / View 的主要关系；
- 修改 Network / Packet / Proto / Dispatcher 的主要数据流；
- 新增影响多个业务模块的 Framework 能力；
- 目录职责发生明显变化。

普通业务逻辑、小型 Bug 修复或不改变架构边界的重构不必机械更新架构文档。

## 修改流程

1. 阅读 `Docs/Architecture.md`、相关目录、基类、接口和调用方。
2. 搜索目标符号，确认计划修改的能力确实存在于当前分支。
3. 说明问题、方案和受影响范围。
4. 以最小改动实现功能。
5. 检查编译错误、生命周期问题、序列化兼容性和事件泄漏。
6. 查看 `git diff`，确认没有无关修改，并判断是否需要同步更新架构文档。
7. 总结修改内容、验证结果和仍需人工在 Unity Editor 中确认的事项。

## 禁止事项

- 不要执行破坏性 Git 操作，例如 `reset --hard`、强制覆盖用户修改或删除整个目录。
- 不要提交密钥、Token、账号信息、构建产物或本地缓存。
- 不要修改 `Library/`、`Temp/`、`Logs/` 等 Unity 生成目录，除非任务明确要求。
- 不要把 Unity + C# 项目改写成 Unity + TypeScript 方案。
- 不要为了让代码“更架构化”而一次性重写大量可工作的现有系统。
- 不要把聊天上下文当作唯一项目知识；可长期复用的架构决定应落入仓库文档或代码。

## 沟通要求

- 用中文说明方案和结果，代码中的命名遵循项目语言规范。
- 对架构权衡说明“为什么这样做”和“有什么代价”。
- 区分“仓库当前实现”“本次修改”“未来建议”，不要混在一起描述。
- 如果无法运行 Unity Editor 或完整测试，明确说明已验证范围，不要声称测试通过。
