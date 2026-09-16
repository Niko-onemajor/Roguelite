using System;
using UnityEngine;

namespace Roguelite
{
    /// <summary>战斗伤害统一入口。</summary>
    public static partial class DamageSystem
    {
        public static readonly System.Random Rng = new System.Random();

        public static void HitEnemy(Enemy enemy, float damage, float critChance)
        {
            if (enemy == null || enemy.Data == null) return;
            // 暴击伤害倍率来自玩家属性(装备 CritDamage 词条生效)，默认 2x
            float critMult = PlayerStats.Instance != null ? PlayerStats.Instance.critMultiplier : 2f;
            enemy.TakeDamage(DamageUtilities.ComputeCrit(damage, DamageUtilities.RollCrit(critChance, Rng), critMult), true);
        }

        public static void RadiusHit(Vector2 origin, float radius, float damage, float critChance)
        {
            float r2 = radius * radius;
            for (int i = EnemyRegistry.All.Count - 1; i >= 0; i--)
            {
                Enemy e = EnemyRegistry.All[i];
                if (e == null || e.Data == null) continue;
                if (((Vector2)e.transform.position - origin).sqrMagnitude <= r2)
                    HitEnemy(e, damage, critChance);
            }
        }

        public static void MeleeHit(Vector2 origin, Vector2 dir, float range, float halfAngleDeg, float damage, float critChance)
        {
            float r2 = range * range;
            float halfCos = Mathf.Cos(halfAngleDeg * Mathf.Deg2Rad);
            for (int i = EnemyRegistry.All.Count - 1; i >= 0; i--)
            {
                Enemy e = EnemyRegistry.All[i];
                if (e == null || e.Data == null) continue;
                Vector2 to = (Vector2)e.transform.position - origin;
                if (to.sqrMagnitude > r2) continue;
                float dot = to.sqrMagnitude > 0.0001f ? Vector2.Dot(to.normalized, dir.normalized) : 1f;
                if (dot >= halfCos) HitEnemy(e, damage, critChance);
            }
        }
    }
}