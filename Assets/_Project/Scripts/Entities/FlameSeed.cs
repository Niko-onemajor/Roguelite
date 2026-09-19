using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>法师R 火焰之种：带物理飞行弹体，在敌方单位间"延迟弹射"(飞向目标，命中后才转向下一目标)，
    /// 与连锁闪电的瞬发链式不同。命中敌人造成魔法伤害并短暂减速；已命中目标不再重复弹射。
    /// Update→Advance 形式封装，便于 EditMode 测试显式驱动。</summary>
    public class FlameSeed : MonoBehaviour
    {
        public const float MoveSpeed = 7f;     // 飞行速度(世界单位/秒)，体现滞空延迟
        public const int MaxBounces = 4;       // 至多命中 4 个敌人
        public const float SlowMult = 0.65f;   // 命中减速至 65%(即减速35%)
        public const float SlowDuration = 1.5f;// 减速持续时间(秒)

        readonly List<Enemy> visited = new List<Enemy>();
        float damage;
        float apRatio;
        Enemy current;

        void Awake()
        {
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = BuildCircleSprite();
            sr.color = new Color(1f, 0.55f, 0.15f, 0.95f); // 火焰橙
            transform.localScale = Vector3.one * 0.55f;    // 视觉直径约0.55世界单位
        }

        void Update() => Advance(Time.deltaTime);

        /// <summary>发射：从 origin 起飞向最近敌人；无目标则销毁。</summary>
        public void Launch(Vector2 origin, float baseDamage, float apRatioValue)
        {
            transform.position = origin;
            damage = baseDamage;
            apRatio = apRatioValue;
            current = EnemyRegistry.Nearest(origin, float.MaxValue);
            if (current == null) { PoolManager.Return(gameObject); return; }
            visited.Add(current);
        }

        /// <summary>手动推进一帧(EditMode 测试显式调用；运行时由 Update 驱动)。</summary>
        internal void Advance(float dt)
        {
            if (current == null) { PoolManager.Return(gameObject); return; }
            Vector2 target = current.transform.position;
            transform.position = Vector2.MoveTowards(transform.position, target, MoveSpeed * dt);
            if (Vector2.Distance(transform.position, target) < 0.25f) Hit(current);
        }

        /// <summary>命中当前目标：魔法伤害+减速+爆花特效；若还有弹射次数则寻找下一个未命中目标转向。</summary>
        void Hit(Enemy enemy)
        {
            if (enemy != null && enemy.Data != null)
            {
                DamageSystem.CastMagic(enemy, damage, apRatio); // 魔法伤害(经魔抗/法穿结算)
                enemy.Slow(SlowMult, SlowDuration);             // 短暂减速
                SimpleVfx.Burst(enemy.transform.position, 1.2f, new Color(1f, 0.6f, 0.15f), 0.35f);
            }
            if (visited.Count >= MaxBounces) { EndFly(); return; }
            Enemy next = NextTarget();
            if (next == null) { EndFly(); return; }
            if (enemy != null)
                SimpleVfx.Dash(enemy.transform.position, next.transform.position, new Color(1f, 0.65f, 0.2f, 0.8f), 0.3f); // 弹射路径
            visited.Add(next);
            current = next;
        }

        /// <summary>弹射终结：清空当前目标(Advance 随之停止)并归池。</summary>
        void EndFly()
        {
            current = null;
            PoolManager.Return(gameObject);
        }

        /// <summary>下一个目标：排除已命中者，取全场最近。</summary>
        Enemy NextTarget()
        {
            Enemy best = null;
            float bestSq = float.PositiveInfinity;
            Vector2 pos = transform.position;
            for (int i = 0; i < EnemyRegistry.All.Count; i++)
            {
                Enemy e = EnemyRegistry.All[i];
                if (e == null || e.Data == null || visited.Contains(e)) continue;
                float sq = ((Vector2)e.transform.position - pos).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = e; }
            }
            return best;
        }

        /// <summary>代码生成圆形渐变贴图(中心亮边缘透明)，无外部图片依赖。</summary>
        static Sprite BuildCircleSprite()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - (size - 1) * 0.5f, dy = y - (size - 1) * 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / (size * 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d * d)));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}