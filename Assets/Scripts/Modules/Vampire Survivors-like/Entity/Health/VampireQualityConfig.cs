using System;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike
{
    /// <summary>
    /// 品质显示配置：按需启用颜色品质，并编辑名称、颜色和参考备注。
    /// 列表顺序由配置决定，不代表枚举数值顺序；此资源不会自动替换现有颜色表。
    /// </summary>
    [CreateAssetMenu(fileName = "VampireQualityConfig", menuName = "Vampire/品质配置")]
    public sealed class VampireQualityConfig : ScriptableObject
    {
        /// <summary>单个品质的显示数据；同一配置内每种枚举只保留一项。</summary>
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("是否选用此品质；关闭后仍保留配置。")]
            public bool Enabled = true;

            [Tooltip("按品质框颜色命名的枚举标识。")]
            public VampireQuality Quality;

            [Tooltip("可编辑的显示名称，默认使用中文颜色名。")]
            public string DisplayName;

            [Tooltip("品质文字或界面元素使用的颜色。")]
            public Color Color = UnityEngine.Color.white;

            [Tooltip("参考品质名称，如 Ancient / 远古；仅作备注。")]
            public string Note;
        }

        [SerializeField, Tooltip("按图片从左到右配置；可关闭或删除不使用的项，每种品质只保留一项。")]
        private List<Entry> _entries = new List<Entry>();

        /// <summary>全部配置项，包含未启用项；使用方应检查 Enabled，不在运行时修改资源。</summary>
        public IReadOnlyList<Entry> Entries => _entries;
    }
}
