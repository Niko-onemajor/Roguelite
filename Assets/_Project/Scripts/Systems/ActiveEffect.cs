using UnityEngine;

namespace Roguelite
{
    /// <summary>主动装备栏槽位：一件带主动效果的装备 + 剩余冷却(受技能急速缩放)。</summary>
    public class ActiveSlot
    {
        public ShopItemData Item;
        public float Remaining; // 剩余冷却秒数，<=0 即可用
    }

    /// <summary>主动效果执行器：数字键触发后按 ActiveType 分发具体逻辑。
    /// 返回 false 表示本次触发失败(法力不足等)，调用方不进入冷却。</summary>
    public static class ActiveEffect
    {
        public static bool Run(PlayerStats stats, ShopItemData item, Vector2 origin)
        {
            if (stats == null || item == null) return false;
            switch (item.activeType)
            {
                case ActiveType.Redemption:
                    stats.Heal(stats.maxHP * PlayerStats.RedemptionHealPct);   // 治疗自身
                    DamageSystem.CastMagicAoe(origin, 5f, 15f, 0.5f);           // 对周围敌人魔法伤害(真实伤害以魔法计)
                    return true;

                case ActiveType.ManaMeld: // 实现者“法力具现”：消耗法力回血
                    if (!stats.TrySpendMana(PlayerStats.ManaMeldCost)) return false;
                    stats.Heal(stats.maxHP * PlayerStats.ManaMeldHealPct);
                    return true;

                case ActiveType.MoveBurst: // 舒瑞娅的狂想曲
                    stats.StartMoveBurst(PlayerStats.MoveBurstDuration);
                    return true;

                case ActiveType.AoeBlast: // 兰顿之兆：对周围敌人魔法伤害
                    DamageSystem.CastMagicAoe(origin, PlayerStats.AoeBlastRadius, 20f, PlayerStats.AoeBlastApRatio);
                    return true;

                default:
                    return false;
            }
        }
    }
}