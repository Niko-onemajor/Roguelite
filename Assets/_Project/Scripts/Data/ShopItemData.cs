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

    /// <summary>被动效果类型：绑定的战斗被动在 PlayerStats 计算动态增益。</summary>
    public enum PassiveType
    {
        None,
        Tyrant, // 霸王血铠“专横+报复”：额外生命值→攻击力 + 已损失生命值%→攻击力
        ArcaneBolt, // 海克斯科技枪刃 被动法术：冷却受技能急速缩放，消耗法力，伤害=基础+法强×0.7(魔法穿透结算)
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

        public bool IsMulti => bonuses != null && bonuses.Count > 0;
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