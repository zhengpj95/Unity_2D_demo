using UnityEngine;

/// <summary>
/// Vampire UI 颜色表。适用于暗黑哥特风格的 720 × 1280 竖屏项目。
/// 所有颜色 Alpha 均为 255；颜色与屏幕分辨率无关。
/// 使用示例：tmpText.color = VampireUIColors.Text.Primary;
/// </summary>
public static class VampireUIColors
{
    /// <summary>通用文本颜色。</summary>
    public static class Text
    {
        public static readonly Color32 Primary = new Color32(241, 231, 213, 255);   // #F1E7D5 主文本
        public static readonly Color32 Secondary = new Color32(185, 176, 166, 255); // #B9B0A6 次级文本
        public static readonly Color32 Hint = new Color32(148, 141, 136, 255);      // #948D88 辅助说明
        public static readonly Color32 Disabled = new Color32(109, 104, 101, 255); // #6D6865 禁用文本
        public static readonly Color32 Number = new Color32(255, 255, 255, 255);   // #FFFFFF 重要数值、HP
        public static readonly Color32 Timer = new Color32(242, 229, 207, 255);    // #F2E5CF 战斗计时
    }

    /// <summary>语义颜色。普通正文使用 Text.Primary，强调信息再使用语义色。</summary>
    public static class Semantic
    {
        public static readonly Color32 Gold = new Color32(233, 185, 94, 255);           // #E9B95E 价值、金币
        public static readonly Color32 GoldHighlight = new Color32(255, 217, 133, 255); // #FFD985 重要奖励
        public static readonly Color32 Danger = new Color32(215, 67, 54, 255);          // #D74336 危险、生命、攻击
        public static readonly Color32 DangerHighlight = new Color32(255, 90, 70, 255);// #FF5A46 强危险提示
        public static readonly Color32 DangerDark = new Color32(138, 32, 29, 255);      // #8A201D 暗红强调
        public static readonly Color32 Success = new Color32(111, 203, 107, 255);       // #6FCB6B 恢复、正向增益
        public static readonly Color32 Info = new Color32(77, 163, 232, 255);           // #4DA3E8 冷却、魔法
        public static readonly Color32 InfoHighlight = new Color32(123, 200, 255, 255); // #7BC8FF 信息高亮
        public static readonly Color32 Purple = new Color32(173, 98, 232, 255);         // #AD62E8 神秘、特殊能力
        public static readonly Color32 PurpleHighlight = new Color32(212, 139, 255, 255);// #D48BFF 紫色高亮
    }

    /// <summary>品质框颜色，按图片从左到右排列；品质名称仅作备注，业务可选用任意子集。铜棕为补充的 UI 配色参考。</summary>
    public static class Quality
    {
        public static readonly Color32 Bronze = new Color32(160, 112, 72, 255); // #A07048 铜棕（Poor / 粗糙）
        public static readonly Color32 Silver = new Color32(184, 184, 184, 255); // #B8B8B8 银灰（Common / 普通）
        public static readonly Color32 Green = new Color32(98, 201, 107, 255); // #62C96B 绿色（Uncommon / 优秀）
        public static readonly Color32 Blue = new Color32(74, 159, 245, 255); // #4A9FF5 蓝色（Rare / 稀有）
        public static readonly Color32 Purple = new Color32(180, 92, 230, 255); // #B45CE6 紫色（Epic / 史诗）
        public static readonly Color32 OrangeGold = new Color32(240, 169, 46, 255); // #F0A92E 橙金（Legendary / 传奇）
        public static readonly Color32 Red = new Color32(237, 75, 59, 255); // #ED4B3B 亮红（Mythic / 神话）
        public static readonly Color32 DarkRed = new Color32(169, 31, 43, 255); // #A91F2B 黑红（Cursed / 诅咒）
        public static readonly Color32 PaleGold = new Color32(242, 226, 175, 255); // #F2E2AF 白金（Ancient / 远古）
    }

    /// <summary>标题与按钮文字。</summary>
    public static class Heading
    {
        public static readonly Color32 Hero = new Color32(243, 217, 161, 255);        // #F3D9A1 超大标题
        public static readonly Color32 Page = new Color32(233, 199, 141, 255);        // #E9C78D 页面标题
        public static readonly Color32 Dialog = new Color32(242, 211, 155, 255);      // #F2D39B 弹窗标题
        public static readonly Color32 Button = new Color32(247, 231, 191, 255);      // #F7E7BF 按钮文字
        public static readonly Color32 SmallButton = new Color32(243, 225, 189, 255); // #F3E1BD 小按钮文字
    }

    /// <summary>战斗飘字颜色。</summary>
    public static class Combat
    {
        public static readonly Color32 Damage = new Color32(243, 231, 212, 255);     // #F3E7D4 普通伤害
        public static readonly Color32 Critical = new Color32(255, 209, 71, 255);    // #FFD147 暴击
        public static readonly Color32 TakenDamage = new Color32(255, 85, 74, 255); // #FF554A 受到伤害
        public static readonly Color32 Heal = new Color32(101, 214, 111, 255);       // #65D66F 回血
        public static readonly Color32 Experience = new Color32(102, 175, 255, 255);// #66AFFF 经验
        public static readonly Color32 Coin = new Color32(255, 205, 77, 255);        // #FFCD4D 金币
    }

    /// <summary>
    /// TMP 材质描边配色参考。此类只提供颜色，不修改材质或描边宽度。
    /// </summary>
    public static class Outline
    {
        public static readonly Color32 Normal = new Color32(22, 16, 16, 255);   // #161010 正文
        public static readonly Color32 Title = new Color32(42, 12, 8, 255);     // #2A0C08 标题
        public static readonly Color32 Button = new Color32(71, 16, 12, 255);   // #47100C 按钮
        public static readonly Color32 Damage = new Color32(35, 11, 8, 255);    // #230B08 数值、伤害
        public static readonly Color32 Critical = new Color32(106, 37, 0, 255); // #6A2500 暴击
        public static readonly Color32 Health = new Color32(67, 16, 14, 255);   // #43100E HP
    }
}

