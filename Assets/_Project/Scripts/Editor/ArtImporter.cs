using System.IO;
using UnityEditor;
using UnityEngine;

namespace Roguelite.EditorTools
{
    /// <summary>
    /// Resources/Sprites 贴图导入管家：
    /// 1) AssetPostprocessor 在任意导入(含批处理)时自动把该目录 PNG 配成 Sprite(2D)，
    ///    保证 Resources.Load&lt;Sprite&gt; 直接可用，无需手动设 Inspector。
    /// 2) 菜单 "Roguelite → 重新配置 Sprites 贴图" 一键重刷整个目录。
    /// 规范：PPU=32、Point 过滤(像素风锐利)、禁 mipmap、无压缩；bg_floor 例外用
    ///     Bilinear+Repeat(背景铺贴)。
    /// </summary>
    public static class ArtImporter
    {
        public const string SpritesFolder = "Assets/_Project/Resources/Sprites";
        public const int PixelsPerUnit = 32;

        static bool IsSpritePath(string assetPath)
        {
            return assetPath.StartsWith(SpritesFolder, System.StringComparison.OrdinalIgnoreCase)
                && assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase);
        }

        static bool IsBackground(string assetPath)
        {
            return Path.GetFileNameWithoutExtension(assetPath) == "bg_floor";
        }

        [MenuItem("Roguelite/重新配置 Sprites 贴图")]
        public static void ReconfigureAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { SpritesFolder });
            int changed = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsSpritePath(path)) continue;
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti != null && Apply(ti)) changed++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ArtImporter] 已更新 {changed} 张贴图（PPU={PixelsPerUnit}）");
        }

        /// <summary>把 TextureImporter 配置为 Sprite 参数。仅改属性，不触发 SaveAndReimport
        /// (用于导入管线内)；返回是否有变更。</summary>
        public static bool Apply(TextureImporter ti)
        {
            if (ti == null) return false;
            bool isBackground = IsBackground(ti.assetPath);
            bool hasMipmap = ti.mipmapEnabled != false;
            bool dirty = false;

            if (ti.textureType != TextureImporterType.Sprite) { ti.textureType = TextureImporterType.Sprite; dirty = true; }
            if (ti.spriteImportMode != SpriteImportMode.Single) { ti.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            if (ti.spritePixelsPerUnit != PixelsPerUnit) { ti.spritePixelsPerUnit = PixelsPerUnit; dirty = true; }
            if (!ti.alphaIsTransparency) { ti.alphaIsTransparency = true; dirty = true; }
            if (hasMipmap) { ti.mipmapEnabled = false; dirty = true; }
            if (ti.textureCompression != TextureImporterCompression.Uncompressed) { ti.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }

            // 像素风 sprite → Point；背景 → Bilinear + Repeat(可平铺)
            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            FilterMode wantFilter = isBackground ? FilterMode.Bilinear : FilterMode.Point;
            TextureWrapMode wantWrap = isBackground ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            if (settings.filterMode != wantFilter) { settings.filterMode = wantFilter; dirty = true; }
            if (settings.wrapMode != wantWrap) { settings.wrapMode = wantWrap; dirty = true; }
            if (dirty) ti.SetTextureSettings(settings);
            return dirty;
        }

        /// <summary>导入管线钩子：Resources/Sprites 下的 PNG 自动转 Sprite，无需手工配置。</summary>
        public class SpriteAutoPostprocessor : AssetPostprocessor
        {
            void OnPreprocessTexture()
            {
                if (!IsSpritePath(assetPath)) return;
                Apply(assetImporter as TextureImporter);
            }
        }
    }
}