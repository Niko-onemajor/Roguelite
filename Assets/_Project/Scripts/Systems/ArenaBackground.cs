using UnityEngine;

namespace Roguelite
{
    /// <summary>竞技场背景：固定在世界原点、只铺满玩家可活动的正方形竞技场(边长 2×Radius)，
    /// 正方形之外无背景(露出相机底色)。相机跟随玩家，出界时视野边缘自然呈空白。
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
            go.transform.position = new Vector3(0f, 0f, 0f); // 固定在世界原点，不随相机移动

            // 只覆盖竞技场方形区域：边长 = 2×Radius(20)，范围外不铺背景
            float edge = ArenaBounds.Radius * 2f;
            float bw = Mathf.Max(0.0001f, sprite.bounds.size.x);
            float bh = Mathf.Max(0.0001f, sprite.bounds.size.y);
            go.transform.localScale = new Vector3(edge / bw, edge / bh, 1f);
        }
    }
}