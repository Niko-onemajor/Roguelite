using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>玩家属性类型（LOL 风格 17 项，对应 PlayerStats 字段）。
    /// 部分词条当前仅存储/展示，具体战斗挂钩待装备效果导入后启用。</summary>
    public enum StatType
    {
        AttackDamage,    // 攻击力
        AbilityPower,    // 法术强度
        AttackSpeed,     // 攻速（乘算词条 addValue 为乘积系数）
        CritChance,      // 暴击几率
        CritDamage,      // 暴击伤害（加算暴击倍率）
        ArmorPen,        // 护甲穿透
        MagicPen,        // 法术穿透
        Omnivamp,        // 全能吸血
        MaxHP,           // 生命值
        HPRegen,         // 生命回复
        Armor,           // 护甲
        MagicResist,     // 魔法抗性
        HealShieldPower, // 治疗与护盾强度
        AbilityHaste,    // 技能急速
        MoveSpeed,       // 移动速度
        AttackRange,     // 攻击距离
        Size,            // 体型
        Mana,            // 法力值
        Tenacity,        // 韧性(减免控制时间)
    }

    /// <summary>被动效果类型：多件被动装备以位掩码叠加(PlayerStats.passiveMask)，互不覆盖。
    /// 每个枚举值在 PlayerStats 有一组战斗挂钩(OnAttackHit/OnKill/魔法增幅/逐帧被动 TickPassives)。</summary>
    public enum PassiveType
    {
        None,
        // ── 已挂钩先例 ──
        Tyrant,          // 霸王血铠：额外生命→攻击力 + 已损生命%→攻击力
        ArcaneBolt,      // 海克斯科技枪刃：周期法术弹(耗法力)
        VoidPen,         // 虚空之杖：法术穿透 +40%(百分比穿透, 与固定穿透叠加)
        // ── AD 装备 ──
        Spellblade,      // 三相之力：每1.5s一次普攻附 攻击力×1 额外物理伤害
        SpellbladeArcane,// 黄昏与黎明：每1.5s一次普攻附 法强×1 额外魔法伤害
        Lifeline,        // 斯特拉克/玛莫提乌斯/原生质护带：生命<30% 获得护盾(冷却30s)
        BlackCleaver,    // 黑色切割者：普攻削减目标护甲10(5s, 上限30)
        DeathDance,      // 死亡之舞：所受物理伤害30% 转3s流血
        SpearPower,      // 朔极之矛：魔法伤害后 下一次魔法伤害+25%
        RuinKing,        // 破败王者之刃：普攻附加目标当前生命6% 物理伤害
        TitanicCleave,   // 巨型九头蛇：普攻对周围敌人造成 攻击力×0.8 溅射
        RavenousCleave,  // 贪欲九头蛇：普攻对周围敌人溅射 伤害×0.5
        Bloodshield,     // 饮血剑：吸血溢出→护盾(上限最大生命15%)
        Energized,       // 岚切/疾射火炮：每2.5s一次普攻附 10+攻击力×0.3 魔法伤害
        Longshot,        // 海克斯镜片C44：距离≥6伤害+25%；击杀+0.5攻击距离(上限3)
        KillVamp,        // 无尽饥渴/暴食胫甲：击杀永久+1%全能吸血(上限6%)
        Hurricane,       // 卢安娜的飓风：普攻对附近另一敌人造成50%伤害
        StrikerFervor,   // 幻影之舞/班德尔音管：普攻后2s内 攻速×0.9、移速×1.1
        CritBarrage,     // 猎魔人弩箭：每6s下一次持续弹幕3次必暴
        // ── AP 装备 ──
        LudensEcho,      // 卢登的回声：每2.5s 技能附伤+溅射
        LiandryBurn,     // 兰德里的折磨：技能施加灼烧(最大生命1%+法强×0.05)/秒 持续3s
        Deathcap,        // 灭世帽：法术强度×1.4
        VoidInfusion,    // 裂隙制造者：额外生命值6%→法术强度
        ReapHeal,        // 蜕生：参与击杀回复最大生命5%
        FocusedShot,     // 视界专注：魔法伤害+10%
        NightHarvest,    // 暗夜收割者：魔法伤害后 移速×1.3 持续2s
        ShadowflameLowHp,// 影焰：对生命<50%敌人魔法伤害+20%
        StormSurge,      // 风暴狂涌：每4s一次 技能/普攻附伤 10+法强×0.3 并移速×1.15
        Malignance,      // 残疫：技能命中附 8+法强×0.3 魔法伤害(每目标2s至多一次)
        CrownHealPower,  // 歌之权冠：最大法力×0.3%→治疗与护盾强度
        // ── 坦克装备 ──
        Sunfire,         // 日炎圣盾：每秒对周围2.8敌人 5+法强×0.15 魔法伤害
        FrozenHeart,     // 冰霜之心：周围4内敌人攻击间隔×1.5
        Thornmail,       // 荆棘之甲：受物理伤害时反弹 30% 魔法给最近敌人
        ForceOfNature,   // 自然之力：受魔法伤害后 移速×1.15/层(3层, 5s)
        Warmogs,         // 狂徒铠甲：5s未受伤后每秒回复最大生命6%
        EndlessHatred,   // 无终恨意：每1.5s 周围2.8 魔法伤害8+法强×0.2 并回复6生命
        DeadMans,        // 亡者的板甲：每1s积1层(上限5, +4%移速/层)；普攻消耗全部层数每层+2伤害
        AbyssalMask,     // 深渊面具：周围4.5内敌人承受魔法伤害×1.15
        Steelcaps,       // 铁板靴：受到的物理伤害×0.88
        GreedTreads,     // 贪婪胫甲：生命≥50%时伤害+8%；<50%时治疗护盾+20%
        FrostBite,       // 基克的聚合：普攻减速目标35%、2s
        ArdentCenser,    // 炽热香炉：治疗/护盾后4s内 攻速×0.88
        FlowingStaff,    // 流水法杖：治疗/护盾后4s内 +4法术强度
    }

    /// <summary>主动效果类型（装备栏 1-0 键触发）：有主动效果的装备购买后自动入槽，冷却受技能急速缩放。</summary>
    public enum ActiveType
    {
        None,
        Redemption, // 救赎：治疗自身+对半径内敌人魔法伤害
        ManaMeld,   // 实现者“法力具现”：消耗法力，回血加治疗护盾
        MoveBurst,  // 舒瑞娅的狂想曲：短暂提升移速
        AoeBlast,   // 兰顿之兆：对周围敌人魔法伤害
        Cleanse,    // 米凯尔的祝福：“净化”：治疗最大生命30%
    }

    /// <summary>一组属性加成（多项组合用于 LOL 风格装备；单属性道具走 statType/addValue）。</summary>
    [System.Serializable]
    public class StatBonus
    {
        public StatType type;
        public float value;

        public StatBonus(StatType type, float value)
        {
            this.type = type;
            this.value = value;
        }
    }

    [CreateAssetMenu(fileName = "ShopItemData", menuName = "Roguelite/ShopItemData")]
    public class ShopItemData : ScriptableObject
    {
        public string displayName = "增益";
        public string iconKey = "";               // 图标文件名(Resources/Items 下, 无扩展名; 空/缺失则文字兜底)
        public StatType statType;
        public int basePrice = 10;   // 商店固定售价（Brotato 风格，不再随购买递增）
        public float addValue = 1f;  // 每次加成(AttackSpeed 为乘积系数 0.85)
        public Color color = Color.white;

        /// <summary>多属性装备（LOL 导入）：非空时优先于 statType/addValue。</summary>
        public List<StatBonus> bonuses = new List<StatBonus>();

        /// <summary>被动/主动效果文案（仅展示，战斗挂钩后续实现）。</summary>
        public string passive = "";

        /// <summary>绑定的战斗被动（默认无；挂上后在 PlayerStats 动态计算增益）。</summary>
        public PassiveType passiveType = PassiveType.None;

        /// <summary>主动效果（装备栏 1-0 键触发）。购买带主动效果的装备自动入槽。</summary>
        public ActiveType activeType = ActiveType.None;

        /// <summary>主动效果基础冷却(秒)，实际受技能急速缩放。0=不设冷却。</summary>
        public float activeCooldown = 0f;

        public bool IsMulti => bonuses != null && bonuses.Count > 0;

        static readonly Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>();

        /// <summary>按 iconKey 从 Resources/Items 加载图标(带缓存)；无图标或缺失返回 null。</summary>
        public Sprite IconSprite
        {
            get
            {
                if (string.IsNullOrEmpty(iconKey)) return null;
                if (iconCache.TryGetValue(iconKey, out Sprite s)) return s;
                Sprite loaded = Resources.Load<Sprite>("Items/" + iconKey);
                iconCache[iconKey] = loaded;
                return loaded;
            }
        }
    }

    /// <summary>属性效果文案统一入口（商店/锻体/符文共用），mult 为倍率(锻体稀有度)。
    /// 多属性装备逐行展示各组加成，被动/主动效果另起一行金色小字。</summary>
    public static class StatText
    {
        public static string Describe(ShopItemData item, float mult = 1f)
        {
            if (item == null) return "";

            string body;
            if (item.IsMulti)
            {
                var lines = new List<string>();
                foreach (var b in item.bonuses) lines.Add(DescribeStat(b.type, b.value, mult));
                body = string.Join("\n", lines);
            }
            else
            {
                body = DescribeStat(item.statType, item.addValue, mult);
            }

            if (!string.IsNullOrEmpty(item.passive))
                body += "\n<color=#bf8c3f>" + item.passive + "</color>";
            return body;
        }

        static string DescribeStat(StatType type, float value, float mult)
        {
            float v = value * mult;
            switch (type)
            {
                case StatType.AttackDamage: return $"攻击力 +{(int)v}";
                case StatType.AbilityPower: return $"法术强度 +{(int)v}";
                case StatType.AttackSpeed: return $"攻速 ×{Mathf.Pow(value, mult):0.##}";
                case StatType.CritChance: return $"暴击 +{Mathf.RoundToInt(v * 100f)}%";
                case StatType.CritDamage: return $"暴击伤害 +{v:0.#}";
                case StatType.ArmorPen: return $"护甲穿透 +{(int)v}";
                case StatType.MagicPen: return $"法术穿透 +{(int)v}";
                case StatType.Omnivamp: return $"全能吸血 +{Mathf.RoundToInt(v * 100f)}%";
                case StatType.MaxHP: return $"生命 +{(int)v}";
                case StatType.HPRegen: return $"生命回复 +{v:0.#}";
                case StatType.Armor: return $"护甲 +{(int)v}";
                case StatType.MagicResist: return $"魔抗 +{(int)v}";
                case StatType.HealShieldPower: return $"治疗护盾 +{Mathf.RoundToInt(v * 100f)}%";
                case StatType.AbilityHaste: return $"技能急速 +{(int)v}";
                case StatType.MoveSpeed: return $"移速 +{v:0.#}";
                case StatType.AttackRange: return $"攻击距离 +{v:0.#}";
                case StatType.Size: return $"体型 +{v:0.##}";
                case StatType.Mana: return $"法力 +{(int)v}";
                case StatType.Tenacity: return $"韧性 +{Mathf.RoundToInt(v * 100f)}%";
                default: return "";
            }
        }
    }
}