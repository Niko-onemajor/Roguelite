using UnityEngine;

namespace Roguelite
{
    /// <summary>竞技场背景：在相机正下方铺整屏 bg_floor 贴图(简单拉伸覆盖视野)。
    /// 素材缺失时静默跳过，保留相机底色，保证任何阶段都能跑。</summary>
    public static class ArenaBackground
    {
        public static void Build(Camera cam)
        {
            if (cam == null) return;
            Sprite sprite = SpriteArt.LoadOrPlaceholder("bg_floor");
            if (sprite == SpriteArt.Fallback) return; // 无背景素材：保留底色

            var go = new GameObject("ArenaBackground", typeof(SpriteRenderer));
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -100;
            go.transform.position = new Vector3(0f, 0f, 0f);

            // 覆盖整个相机视野：高=orthoSize*2，宽按纵横比(竞技场半径外区域也铺满)
            float worldH = cam.orthographicSize * 2f;
            float worldW = worldH * cam.aspect;
            float bw = Mathf.Max(0.0001f, sprite.bounds.size.x);
            float bh = Mathf.Max(0.0001f, sprite.bounds.size.y);
            go.transform.localScale = new Vector3(worldW / bw, worldH / bh, 1f);
        }
    }
}