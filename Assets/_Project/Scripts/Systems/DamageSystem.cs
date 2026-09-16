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
            PlayerStats ps = PlayerStats.Instance;
            // 暴击伤害倍率来自玩家属性(装备 CritDamage 词条生效)，默认 2x；猎魔人弹幕期间必定暴击
            float critMult = ps != null ? ps.critMultiplier : 2f;
            if (ps != null && ps.BarrageCount > 0) { critChance = 1f; ps.BarrageCount--; }
            bool crit = DamageUtilities.RollCrit(critChance, Rng);
            Vector2 hitPos = enemy.transform.position;
            float before = enemy.Health;
            enemy.TakeDamage(DamageUtilities.ComputeCrit(damage, crit, critMult), crit);
            if (ps != null && enemy != null && enemy.Health > 0f && before - enemy.Health > 0f)
                ps.OnAttackHit(enemy, before - enemy.Health, hitPos); // 装备普攻 on-hit 被动(破败/咒刃/黑切/分裂箭等)
        }

        /// <summary>法术伤害统一入口(装备/符文特效触发)：应用 死亡之帽/裂隙制造者/歌之权冠 动态属性与各类魔法增幅被动。
        /// 基础值 + 法强加成，最终经怪物魔抗、玩家法穿与深渊面具易伤结算。</summary>
        public static void CastMagic(Enemy enemy, float baseDamage, float apRatio)
        {
            if (enemy == null || enemy.Data == null) return;
            PlayerStats ps = PlayerStats.Instance;
            float ap = ps != null ? ps.TotalAbilityPower : 0f;
            float dmg = baseDamage + ap * apRatio;

            if (ps != null)
            {
                // 朔极之矛：上一次魔法命中 充能 25% 增幅(命中即触发下一次)
                if (ps.TakeSpearBoost()) { dmg *= 1.25f; }
                // 视界专注：魔法伤害常驻 +10%
                if (ps.HasPassive(PassiveType.FocusedShot)) dmg *= 1.10f;
                // 影焰：对生命<50%的敌人魔法伤害 +20%
                if (ps.HasPassive(PassiveType.ShadowflameLowHp) && enemy.Data.maxHP > 0f && enemy.Health / enemy.Data.maxHP < 0.5f)
                    dmg *= 1.20f;
                // 风暴狂涌：每4s一次附伤 + 短暂移速
                if (ps.HasPassive(PassiveType.StormSurge) && ps.StormSurgeReady)
                {
                    dmg += 10f + ap * 0.3f;
                    ps.ConsumeStormSurge();
                }
                // 卢登的回声：每2.5s一次 技能附伤并溅射附近另一敌人
                if (ps.HasPassive(PassiveType.LudensEcho) && ps.LudenReady)
                {
                    float splash = 12f + ap * 0.4f;
                    dmg += splash;
                    Enemy other = Nearest(exclude: enemy, origin: enemy.transform.position, radius: 2.5f);
                    if (other != null) other.TakeMagicDamage(splash * 0.5f);
                    ps.ConsumeLuden();
                }
                // 残疫：每目标2s至多一次 附伤
                if (ps.HasPassive(PassiveType.Malignance) && enemy.malignCd <= 0f)
                {
                    dmg += 8f + ap * 0.3f;
                    enemy.malignCd = 2f;
                }
                // 兰德里的折磨：技能施加灼烧 DoT
                if (ps.HasPassive(PassiveType.LiandryBurn))
                    ps.ApplyBurn(enemy, enemy.Data.maxHP * 0.01f + ap * 0.05f, 3f);
                // 暗夜收割者：魔法伤害后 移速×1.3 持续2s
                if (ps.HasPassive(PassiveType.NightHarvest)) ps.PushMoveAmp(1.3f, 2f);
            }

            enemy.TakeMagicDamage(dmg);

            if (ps != null)
            {
                // 朔极之矛：成功命中后 给下一次魔法伤害充能
                if (ps.HasPassive(PassiveType.SpearPower)) ps.SetSpearBoost();
                // 风暴狂涌的移速也是伤害后触发
                if (ps.HasPassive(PassiveType.StormSurge)) ps.PushMoveAmp(1.15f, 1.5f);
            }
        }

        public static void CastMagicAoe(Vector2 origin, float radius, float baseDamage, float apRatio)
        {
            float r2 = radius * radius;
            for (int i = EnemyRegistry.All.Count - 1; i >= 0; i--)
            {
                Enemy e = EnemyRegistry.All[i];
                if (e == null || e.Data == null) continue;
                if (((Vector2)e.transform.position - origin).sqrMagnitude <= r2)
                    CastMagic(e, baseDamage, apRatio);
            }
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

        /// <summary>以 origin 为圆心、radius 内 除 exclude 外最近的生命敌人(卢登溅射用)。</summary>
        static Enemy Nearest(Enemy exclude, Vector2 origin, float radius)
        {
            float r2 = radius * radius;
            Enemy best = null;
            float bestSq = r2;
            for (int i = 0; i < EnemyRegistry.All.Count; i++)
            {
                Enemy e = EnemyRegistry.All[i];
                if (e == null || e == exclude || e.Data == null) continue;
                float sq = ((Vector2)e.transform.position - origin).sqrMagnitude;
                if (sq <= bestSq) { bestSq = sq; best = e; }
            }
            return best;
        }
    }
}