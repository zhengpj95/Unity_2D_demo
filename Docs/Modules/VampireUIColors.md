# Vampire UI 颜色配置

本文件记录 Vampire Survivors-like 模块当前的颜色常量。代码来源是：
`Assets/Scripts/Modules/Vampire Survivors-like/Entity/Health/VampireUIColors.cs`。

所有颜色以 `#RRGGBB` 格式维护；如需透明度，可使用 `#RRGGBBAA`。运行时由
`FromHex` 转换为 `Color32`，格式不合法会抛出异常。

## 文本颜色

| 配置项           | Hex       | 用途                   |
| ---------------- | --------- | ---------------------- |
| `Text.Primary`   | `#F1E7D5` | 主要正文、常规标签     |
| `Text.Secondary` | `#B9B0A6` | 辅助说明、低优先级标签 |
| `Text.Highlight` | `#FFFFFF` | 数值、奖励等强调信息   |
| `Text.Disabled`  | `#6D6865` | 不可用或不可交互状态   |

## 文本描边颜色

描边宽度属于具体 TMP 样式或材质配置，本表仅定义描边颜色。

| 配置项            | Hex       | 用途           |
| ----------------- | --------- | -------------- |
| `Outline.Default` | `#161010` | 正文、HUD 数值 |
| `Outline.Title`   | `#2A0C08` | 标题           |
| `Outline.Button`  | `#47100C` | 按钮文字       |
| `Outline.Damage`  | `#230B08` | 伤害数字       |

## 品质框颜色

品质索引与 `vampire02` 图片从左到右的顺序一致。`VampireQuality` 的枚举值和
`Assets/Configs/Survivor/VampireQualityConfig.asset` 的 `Quality` 整数必须保持同步。

| 索引 | 枚举         | 显示名 | Hex       | 参考品质         |
| ---- | ------------ | ------ | --------- | ---------------- |
| 0    | `Bronze`     | 铜棕   | `#A07048` | Poor / 粗糙      |
| 1    | `Silver`     | 银灰   | `#B8B8B8` | Common / 普通    |
| 2    | `Green`      | 绿色   | `#62C96B` | Uncommon / 优秀  |
| 3    | `Blue`       | 蓝色   | `#4A9FF5` | Rare / 稀有      |
| 4    | `Purple`     | 紫色   | `#B45CE6` | Epic / 史诗      |
| 5    | `OrangeGold` | 橙金   | `#F0A92E` | Legendary / 传奇 |
| 6    | `Red`        | 亮红   | `#ED4B3B` | Mythic / 神话    |
| 7    | `DarkRed`    | 黑红   | `#A91F2B` | Cursed / 诅咒    |
| 8    | `PaleGold`   | 白金   | `#F2E2AF` | Ancient / 远古   |

## 修改规则

1. 只调整颜色时，修改 `VampireUIColors.cs` 中对应的 Hex 值；品质颜色还应同步检查 `VampireQualityConfig.asset` 的 `Color` 字段。
2. 不要为新增颜色修改既有品质枚举索引；该索引已被 Unity 资源序列化使用。
3. 新增文本或描边颜色前，先确认现有语义项不能表达目标用途，避免重新堆积无使用场景的色值。
4. 修改后在 Unity Editor 检查 TMP 文字和品质框在实际深色背景下的对比度。
