# Survivor 主界面美术资源

本目录保存从 `Docs/Art/References/SurvivorMainMenuReference-720x1280-v1.png` 拆分并标准化的主界面资源。它们是新增版本，不替换 `Login/` 下已有登录背景或 `Resources/UI/` 下现有图集。

| 文件 | 尺寸 | 用途 | Alpha |
| --- | --- | --- | --- |
| `vs-main-menu-background-720x1280-v2.png` | 720 × 1280 | **当前推荐**主界面完整背景；保留左右破损旗帜、左上乌鸦与右侧灯笼，不包含交互 UI。 | 无 |
| `vs-main-menu-background-720x1280-v1.png` | 720 × 1280 | 首个无 UI 底图版本；保留作回退参考，缺少两侧旗帜。 | 无 |
| `vs-main-menu-ui-atlas-1280x1280-v1.png` | 1280 × 1280 | 主界面黑铁哥特控件图集；包含框体、角色圆框、箭头、红色主按钮与小装饰。 | 有 |

## Unity 导入约定

首次由 Unity Editor 发现本目录时，会为目录和两张 PNG 自动创建 `.meta` 文件。不要复制已有资源的 `.meta`，否则会造成 GUID 重复。

### 背景

- `Texture Type`：`Sprite (2D and UI)`，`Sprite Mode`：`Single`。
- 禁用 Mip Maps；保持默认 `Pixels Per Unit = 100`。
- 在 720 × 1280 Canvas 中用 `Image` 显示，使用 `Preserve Aspect`，四边对齐到背景区域。
- 文件本身没有透明通道，不要把它误设为遮罩或按钮 Sprite。

### 图集

- `Texture Type`：`Sprite (2D and UI)`，`Sprite Mode`：`Multiple`。
- 禁用 Mip Maps；保留 Alpha；压缩使用 `None` 或可接受透明边缘的高质量设置。
- 在 Sprite Editor 中使用**手动矩形切片**，并保留每个控件外侧透明边距；不要使用自动切片。
- 框体需要做九宫格时，只在内侧深色填充区域设置 Border，金属尖刺、宝石、蝙蝠和骷髅必须保留在固定边缘内。

## 图集内容命名建议

切片后的 Sprite 建议使用以下名字，供后续 Prefab 和 UI 绑定统一引用：

```text
MainMenuProfileFrame
MainMenuResourceFrame
MainMenuSettingsButton
MainMenuCharacterFrameLarge
MainMenuCharacterFrameSmallA
MainMenuCharacterFrameSmallB
MainMenuArrowLeft
MainMenuArrowRight
MainMenuNavigationFrameA
MainMenuNavigationFrameB
MainMenuNavigationFrameC
MainMenuStartButtonFrame
MainMenuOrnamentGem
MainMenuOrnamentSkull
MainMenuOrnamentBat
MainMenuOrnamentDivider
```

图集只是控件皮肤：玩家头像、货币图标、文字、角色立绘和运行时状态由 UI Prefab 或 Presenter 提供，不能烘焙进这些基础 Sprite。

## 风格与来源

所有主界面后续资源制作，都必须遵循 [SurvivorVisualStyleGuide.md](../../../../Docs/Art/SurvivorVisualStyleGuide.md)。该规范中的项目原始背景和 `survivor01.png` / `survivor02.png` 仍是风格细节的最高依据。
