# 第三方资源来源清单

本文记录仓库中的第三方美术、源码、二进制插件和直接 UPM 依赖。清单描述当前仓库能够验证的事实；缺少许可证文件、订单记录或下载记录的项目统一标记为“待确认”。

## 维护规则

- 新增第三方内容时记录：当前路径、资源名称、作者/发布者、来源链接、版本、获取渠道、许可证和本地证据。
- 商店购买或免费下载的资源应保存订单、下载记录或许可证快照。同一资源在不同发布渠道可能采用不同许可证。
- 随资源提供的 `LICENSE`、`NOTICE`、`credits.txt` 等文件应与资源一起提交。
- 第三方原始资源经过裁剪、重命名或二次加工后，应补充“原目录 → 使用目录”的对应关系。
- UPM 包的准确版本以 `Packages/manifest.json` 和 `Packages/packages-lock.json` 为准。

## 已确认或已有本地证据

| 当前路径                                                             | 资源 / 作者                                      | 来源                                                                                 | 版本       | 许可证与本地证据                                                               | 后续处理                                                     |
| -------------------------------------------------------------------- | ------------------------------------------------ | ------------------------------------------------------------------------------------ | ---------- | ------------------------------------------------------------------------------ | ------------------------------------------------------------ |
| `Assets/Arts/Pixel Adventure`                                        | Pixel Adventure 1 / Pixel Frog                   | [官方 itch.io 页面](https://pixelfrog-assets.itch.io/pixel-adventure-1)              | 本地未记录 | 官方页标记为 CC0 1.0；仓库内没有许可证副本                                     | 保存许可证快照或随包许可证                                   |
| `Assets/Arts/Tiny RPG Forest`                                        | Tiny RPG Forest / Luis Zuno（Ansimuz）           | [OpenGameArt 页面](https://opengameart.org/content/tiny-rpg-forest)                  | 本地未记录 | 页面将美术标记为 CC0；页面中的音乐采用单独的署名要求。本地目录当前为 `Artwork` | 若以后导入音乐，单独登记音乐来源和署名要求                   |
| `Assets/Arts/Goblin`                                                 | LPC 组合角色 / 多位作者                          | 目录内 `credits.txt` 列出的 OpenGameArt 与 GitHub 页面                               | 本地未记录 | `credits.txt` 按组成图层列出 OGA-BY、CC-BY、CC-BY-SA、GPL、CC0 等许可          | 保留完整 `credits.txt`，发行时按选用的许可完成署名和对应义务 |
| `Assets/Arts/GreenPig`                                               | LPC 组合角色 / 多位作者                          | 目录内 `credits.txt` 列出的 OpenGameArt 页面                                         | 本地未记录 | `credits.txt` 按组成图层列出 OGA-BY、CC-BY、CC-BY-SA、GPL、CC0 等许可          | 保留完整 `credits.txt`，发行时按选用的许可完成署名和对应义务 |
| `Assets/TextMesh Pro`                                                | TextMesh Pro Essential Resources / Unity         | Unity Editor 随 `com.unity.textmeshpro` 导入                                         | 3.0.7      | 版本来自 `Packages/manifest.json`；目录保存字体、样式和 Shader 等项目资源      | 升级包后检查 Essential Resources 的兼容性                    |
| `Assets/XLua`、`Assets/Plugins` 中各平台 `xlua` 库                   | xLua / Tencent                                   | [Tencent/xLua](https://github.com/Tencent/xLua)；名称和本地 changelog 可对应到该项目 | 2.1.15     | 版本来自 `Assets/XLua/CHANGELOG.txt`；仓库内未找到 LICENSE                     | 补充与当前版本匹配的 LICENSE、提交号或原始发布包记录         |
| `Assets/Plugins/Protobuf/Google.Protobuf.dll`                        | Google.Protobuf                                  | 获取渠道待确认                                                                       | 3.35.1     | 版本来自 DLL 产品元数据；仓库内未找到许可证副本                                | 记录 NuGet 或发布包来源并补许可证                            |
| `Assets/Plugins/Protobuf/System.Runtime.CompilerServices.Unsafe.dll` | Microsoft System.Runtime.CompilerServices.Unsafe | 获取渠道待确认                                                                       | 6.1.2      | 版本来自 DLL 产品元数据；仓库内未找到许可证副本                                | 记录 NuGet 或发布包来源并补许可证                            |

## 获取渠道或许可证待确认

| 当前路径                                                               | 当前可识别内容                        | 已知线索                                                                                                                                                                                                          | 缺少的信息                                                                 |
| ---------------------------------------------------------------------- | ------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------- |
| `Assets/Arts/Tiny Swords`                                              | Tiny Swords / Pixel Frog              | 存在 [官方 itch.io 页面](https://pixelfrog-assets.itch.io/tiny-swords) 和 [Unity Asset Store 页面](https://assetstore-fallback.unity.com/packages/2d/environments/tiny-swords-352566)；Asset Store 页面版本为 1.0 | 无法从仓库判断实际获取渠道。两个渠道的许可证不同，需要用下载记录或订单确认 |
| `Assets/Arts/TopDown_Shooter`                                          | Top-down shooter 角色、枪械和子弹图片 | 文件名不足以唯一确定原始资源包                                                                                                                                                                                    | 作者、下载页、版本、许可证                                                 |
| `Assets/Arts/Enemy`                                                    | 多组敌人及 Hero Knight 图片           | 目录内未找到统一来源文件                                                                                                                                                                                          | 每组素材的作者、下载页、版本、许可证                                       |
| `Assets/Arts/Effects`                                                  | 像素特效图片                          | 目录内未找到来源文件                                                                                                                                                                                              | 作者、下载页、版本、许可证                                                 |
| `Assets/Arts/HealthHeartSystem`、`Assets/Arts/Hp`、`Assets/Arts/Skill` | 血量、技能等 UI 图片                  | 目录内未找到来源文件                                                                                                                                                                                              | 作者、下载页、版本、许可证                                                 |
| `Assets/Arts/Melee`、`Assets/Arts/UI`                                  | 玩法和 UI 混合素材                    | 目录内未找到统一来源文件                                                                                                                                                                                          | 按原始资源包拆分来源，并记录各自许可证                                     |
| `Assets/Resources/Audio`                                               | WAV、MP3 与 AudioMixer                | 目录内未找到统一来源文件                                                                                                                                                                                          | 每个音频包或单曲的作者、下载页、许可证                                     |
| `Assets/Resources/UI`、`Assets/Resources/Shaders`                      | 字体、图片和 Shader                   | 目录内未找到统一来源文件                                                                                                                                                                                          | 作者、下载页、版本、许可证                                                 |

## 直接 UPM 依赖

| 包名                         | 当前声明                                                              | 来源                                                              |
| ---------------------------- | --------------------------------------------------------------------- | ----------------------------------------------------------------- |
| `com.endel.nativewebsocket`  | Git 分支 `upm-2`，锁定提交 `c612a4fef60f2ae57614b73202d2d261ba56aa3e` | [endel/NativeWebSocket](https://github.com/endel/NativeWebSocket) |
| `com.unity.addressables`     | 1.21.21                                                               | Unity Registry                                                    |
| `com.unity.cinemachine`      | 2.10.5                                                                | Unity Registry                                                    |
| `com.unity.collab-proxy`     | 2.10.0                                                                | Unity Registry                                                    |
| `com.unity.feature.2d`       | 2.0.1                                                                 | Unity Registry                                                    |
| `com.unity.ide.rider`        | 3.0.36                                                                | Unity Registry                                                    |
| `com.unity.ide.visualstudio` | 2.0.22                                                                | Unity Registry                                                    |
| `com.unity.inputsystem`      | 1.14.2                                                                | Unity Registry                                                    |
| `com.unity.test-framework`   | 1.1.33                                                                | Unity Registry                                                    |
| `com.unity.textmeshpro`      | 3.0.7                                                                 | Unity Registry                                                    |
| `com.unity.timeline`         | 1.7.7                                                                 | Unity Registry                                                    |
| `com.unity.ugui`             | 1.0.0                                                                 | Unity Registry                                                    |
| `com.unity.visualscripting`  | 1.9.4                                                                 | Unity Registry                                                    |

Unity 内置模块和上述包的间接依赖由 `Packages/packages-lock.json` 完整记录，本清单不重复展开。

## 当前需要补齐的来源

1. 确认 Tiny Swords 的实际获取渠道并保存对应凭证。
2. 为 xLua、Google.Protobuf 和 Unsafe DLL 补充与当前版本匹配的许可证及来源记录。
3. 追溯 `TopDown_Shooter`、零散美术、音频、字体和 Shader 的来源。
4. 根据 `Goblin/credits.txt` 与 `GreenPig/credits.txt` 整理最终发行所需的署名文本。

最后核对日期：2026-09-13。
