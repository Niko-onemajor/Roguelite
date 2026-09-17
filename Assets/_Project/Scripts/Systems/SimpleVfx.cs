using System;
using UnityEngine;

namespace Roguelite
{
    /// <summary>代码生成的战斗视觉特效：近战挥砍/光环扩散/光束/爆点/位移残影。
    /// 全部由 SpriteRenderer / LineRenderer + 内置圆形 Sprite 构成，无外部贴图依赖，
    /// 运行时经 VfxFade 播放”缩放+淡出+自毁“动画，world 空间渲染，排序在普通精灵之上。</summary>
    public static class SimpleVfx
    {
        static Texture2D circleTex;
        static Sprite circleSprite;
        static readonly Material lineMat = new Material(Shader.Find("Sprites/Default"));

        const float SpriteWorldSize = 0.64f; // 圆形 Sprite 的基准世界尺寸(64px @100ppu)

        static Sprite CircleSprite()
        {
            if (circleSprite == null)
            {
                const int N = 64;
                circleTex = new Texture2D(N, N, TextureFormat.RGBA32, false);
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        Vector2 c = new Vector2((x + 0.5f) / N - 0.5f, (y + 0.5f) / N - 0.5f);
                        float d = c.magnitude * 2f;                    // 0 中心 → 1 边缘
                        float a = Mathf.Clamp01(1f - d);               // 中心实 → 边缘透明
                        circleTex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                    }
                circleTex.Apply();
                circleSprite = Sprite.Create(circleTex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
            }
            return circleSprite;
        }

        static void SetDiameter(SpriteRenderer sr, float diameter)
        {
            float s = diameter / SpriteWorldSize;
            sr.transform.localScale = new Vector3(s, s, 1f);
        }

        static SpriteRenderer NewCircle(Vector2 worldPos, Color color, float diameter)
        {
            var go = new GameObject("VfxCircle");
            go.transform.position = worldPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CircleSprite();
            sr.color = color;
            sr.sortingOrder = 200;
            SetDiameter(sr, diameter);
            return sr;
        }

        /// <summary>爆点：圆盘从小扩散到目标直径并淡出。</summary>
        public static void Burst(Vector2 pos, float diameter, Color color, float dur = 0.35f, float grow = 1.5f)
        {
            SpriteRenderer sr = NewCircle(pos, color, 0.15f);
            var fx = sr.gameObject.AddComponent<VfxFade>();
            fx.Init(dur, t =>
            {
                SetDiameter(sr, Mathf.Lerp(0.15f, diameter * grow, t));
                sr.color = new Color(color.r, color.g, color.b, color.a * (1f - t));
            });
        }

        /// <summary>扩散光环(冲击波)：圆盘从内圈扩散到外圈并淡出，常用于 AOE/持续技能每跳。</summary>
        public static void Ring(Vector2 pos, float diameter, Color color, float dur = 0.4f)
        {
            SpriteRenderer sr = NewCircle(pos, color, diameter * 0.45f);
            var fx = sr.gameObject.AddComponent<VfxFade>();
            fx.Init(dur, t =>
            {
                float d = Mathf.Lerp(diameter * 0.35f, diameter * 1.15f, t);
                SetDiameter(sr, d);
                sr.color = new Color(color.r, color.g, color.b, color.a * (1f - t) * 0.9f);
            });
        }

        /// <summary>光束(法师Q 死亡射线)：细长发光条沿方向铺开，随后淡出。</summary>
        public static void Beam(Vector2 from, Vector2 dir, float length, float width, Color color, float dur = 0.3f)
        {
            Vector2 d = dir.normalized;
            if (d.sqrMagnitude < 0.0001f) d = Vector2.right;
            Vector2 mid = from + d * (length * 0.5f);
            SpriteRenderer sr = NewCircle(mid, color, width);
            sr.transform.rotation = Quaternion.FromToRotation(Vector2.right, d);
            sr.transform.localScale = new Vector3(length / SpriteWorldSize, width / SpriteWorldSize, 1f);
            var fx = sr.gameObject.AddComponent<VfxFade>();
            fx.Init(dur, t =>
            {
                float w = Mathf.Lerp(width, width * 0.4f, t);
                sr.transform.localScale = new Vector3(length / SpriteWorldSize, Mathf.Max(0.02f, w / SpriteWorldSize), 1f);
                sr.color = new Color(color.r, color.g, color.b, color.a * (1f - t) * 0.85f);
            });
        }

        /// <summary>近战挥砍弧：面前扇形弧线一闪而过的刀光。</summary>
        public static void Swing(Vector2 pos, Vector2 dir, float radius, float halfAngleRad, Color color, float dur = 0.18f)
        {
            Vector2 d = dir.normalized;
            if (d.sqrMagnitude < 0.0001f) d = Vector2.right;
            float baseAngle = Mathf.Atan2(d.y, d.x);
            var go = new GameObject("VfxSwing");
            go.transform.position = pos;
            var lr = go.AddComponent<LineRenderer>();
            lr.material = lineMat;
            lr.positionCount = 24;
            lr.startWidth = lr.endWidth = 0.32f;
            lr.useWorldSpace = true;
            lr.sortingOrder = 200;
            var fx = go.AddComponent<VfxFade>();
            fx.Init(dur, t =>
            {
                float r = Mathf.Lerp(radius, radius * 1.22f, t);
                float sweep = halfAngleRad * Mathf.Clamp01(t * 3f); // 刀光从一侧扫到另一侧
                for (int i = 0; i < 24; i++)
                {
                    float a = baseAngle + Mathf.Lerp(-sweep, sweep, i / 23f);
                    lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
                }
                lr.startColor = lr.endColor = new Color(color.r, color.g, color.b, color.a * (1f - t));
            });
        }

        /// <summary>位移残影(射手Q 闪避突袭)：起终点连线段 + 终点爆点。</summary>
        public static void Dash(Vector2 from, Vector2 to, Color color, float dur = 0.35f)
        {
            var go = new GameObject("VfxDash");
            go.transform.position = from;
            var lr = go.AddComponent<LineRenderer>();
            lr.material = lineMat;
            lr.positionCount = 2;
            lr.startWidth = lr.endWidth = 0.45f;
            lr.useWorldSpace = true;
            lr.sortingOrder = 200;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            var fx = go.AddComponent<VfxFade>();
            float dist = Vector2.Distance(from, to);
            fx.Init(dur, t =>
            {
                float len = Mathf.Lerp(dist, dist * 0.3f, t);
                Vector2 dir = (to - from).normalized;
                Vector2 ahead = to - dir * len;
                lr.SetPosition(0, ahead);
                lr.SetPosition(1, to);
                lr.startColor = lr.endColor = new Color(color.r, color.g, color.b, color.a * (1f - t));
            });
            Burst(to, 1.2f, color, dur * 0.8f, 1.8f);
        }
    }

    /// <summary>特效播放器：t∈[0,1] 逐帧回调驱动缩放/透明度，播完自毁。</summary>
    public class VfxFade : MonoBehaviour
    {
        float _age;
        float _dur = 1f;
        Action<float> _step;

        public void Init(float dur, Action<float> step)
        {
            _dur = Mathf.Max(0.05f, dur);
            _step = step;
        }

        void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / _dur);
            try { _step?.Invoke(t); }
            catch (Exception e) { Debug.LogWarning("Vfx step error: " + e.Message); }
            if (t >= 1f) Destroy(gameObject);
        }
    }
}