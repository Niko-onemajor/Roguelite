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
                    PlayFx(ActiveType.Redemption, origin);
                    return true;

                case ActiveType.ManaMeld: // 实现者“法力具现”：消耗法力回血
                    if (!stats.TrySpendMana(PlayerStats.ManaMeldCost)) return false;
                    stats.Heal(stats.maxHP * PlayerStats.ManaMeldHealPct);
                    PlayFx(ActiveType.ManaMeld, origin);
                    return true;

                case ActiveType.MoveBurst: // 舒瑞娅的狂想曲
                    stats.StartMoveBurst(PlayerStats.MoveBurstDuration);
                    PlayFx(ActiveType.MoveBurst, origin);
                    return true;

                case ActiveType.AoeBlast: // 兰顿之兆：对周围敌人魔法伤害
                    DamageSystem.CastMagicAoe(origin, PlayerStats.AoeBlastRadius, 20f, PlayerStats.AoeBlastApRatio);
                    PlayFx(ActiveType.AoeBlast, origin);
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>触发特效反馈：按主动类型播放不同颜色粒子爆发 + 短促音效，让玩家明确感知效果已触发。
        /// EditMode(非播放态)直接跳过，避免批处理环境创建游戏对象。</summary>
        static void PlayFx(ActiveType type, Vector2 origin)
        {
            if (!Application.isPlaying) return;
            switch (type)
            {
                case ActiveType.Redemption: // 救赎：金白色治疗光
                    FxPlayer.Burst(origin, new Color(1f, 0.92f, 0.55f), 44);
                    FxPlayer.Beep(520f, 0.25f, 0.5f);
                    break;
                case ActiveType.ManaMeld: // 实现者：青蓝法力光
                    FxPlayer.Burst(origin, new Color(0.45f, 0.9f, 1f), 36);
                    FxPlayer.Beep(620f, 0.22f, 0.5f);
                    break;
                case ActiveType.MoveBurst: // 舒瑞娅：金黄冲刺光
                    FxPlayer.Burst(origin, new Color(1f, 0.82f, 0.25f), 46);
                    FxPlayer.Beep(720f, 0.22f, 0.5f);
                    break;
                case ActiveType.AoeBlast: // 兰顿之兆：红紫冲击波
                    FxPlayer.Burst(origin, new Color(1f, 0.4f, 0.55f), 56);
                    FxPlayer.Beep(440f, 0.3f, 0.55f);
                    break;
            }
        }
    }
}