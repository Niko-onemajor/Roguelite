using UnityEngine;

namespace Roguelite
{
    /// <summary>运行时生成白色圆形占位 Sprite（单位圆 world 半径 0.5，ppu=32，用 SpriteRenderer.color 上色）。</summary>
    public static class PlaceholderArt
    {
        const int Resolution = 32;
        const float PixelsPerUnit = 32f;

        public static Sprite Circle()
        {
            var tex = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            float c = (Resolution - 1) / 2f;
            var px = new Color[Resolution * Resolution];
            for (int y = 0; y < Resolution; y++)
                for (int x = 0; x < Resolution; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01(c - d + 0.5f);
                    px[y * Resolution + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, Resolution, Resolution), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }
    }
}