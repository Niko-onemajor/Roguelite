using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>角色美术加载：优先 Resources/Sprites/&lt;name&gt;.png，
    /// 缺失时自动回退共享占位圆，保证无美术阶段游戏可运行。结果按名称缓存。</summary>
    public static class SpriteArt
    {
        public const string Folder = "Sprites";
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// <summary>共享占位圆(白色)。</summary>
        public static Sprite Fallback => PlaceholderArt.Circle();

        /// <summary>加载 <paramref name="name"/> 对应贴图；不存在则返回占位圆。</summary>
        public static Sprite LoadOrPlaceholder(string name)
        {
            if (string.IsNullOrEmpty(name)) return Fallback;
            if (cache.TryGetValue(name, out Sprite cached)) return cached;
            Sprite found = Resources.Load<Sprite>(Folder + "/" + name);
            cache[name] = found != null ? found : Fallback;
            return cache[name];
        }

        /// <summary>是否存在真实贴图(用于决定角色走贴图原色还是占位染色)。</summary>
        public static bool HasReal(string name) => LoadOrPlaceholder(name) != Fallback;

        /// <summary>归一化系数：把 <paramref name="sprite"/> 显示为 targetWorld(世界单位) 宽所需 localScale。
        /// 使任意 PPU/像素尺寸的贴图都与占位圆(1 单位宽)等观；占位圆本身 factor=1。</summary>
        public static float NormalizeFactor(Sprite sprite, float targetWorld)
        {
            if (sprite == null) return 1f;
            float w = sprite.bounds.size.x;
            return w > 0.0001f ? targetWorld / w : 1f;
        }
    }
}