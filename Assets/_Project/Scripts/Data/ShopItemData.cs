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
    }

    [CreateAssetMenu(fileName = "ShopItemData", menuName = "Roguelite/ShopItemData")]
    public class ShopItemData : ScriptableObject
    {
        public string displayName = "增益";
        public StatType statType;
        public int basePrice = 10;   // 商店固定售价（Brotato 风格，不再随购买递增）
        public float addValue = 1f;  // 每次加成(AttackSpeed 为乘积系数 0.85)
        public Color color = Color.white;
    }

    /// <summary>属性效果文案统一入口（商店/锻体/符文共用），mult 为倍率(锻体稀有度)。</summary>
    public static class StatText
    {
        public static string Describe(ShopItemData item, float mult = 1f)
        {
            if (item == null) return "";
            float v = item.addValue * mult;
            switch (item.statType)
            {
                case StatType.AttackDamage: return $"攻击力 +{(int)v}";
                case StatType.AbilityPower: return $"法术强度 +{(int)v}";
                case StatType.AttackSpeed: return $"攻速 ×{Mathf.Pow(item.addValue, mult):0.##}";
                case StatType.CritChance: return $"暴击 +{(int)(v * 100f)}%";
                case StatType.CritDamage: return $"暴击伤害 +{v:0.#}";
                case StatType.ArmorPen: return $"护甲穿透 +{(int)v}";
                case StatType.MagicPen: return $"法术穿透 +{(int)v}";
                case StatType.Omnivamp: return $"全能吸血 +{(int)(v * 100f)}%";
                case StatType.MaxHP: return $"生命 +{(int)v}";
                case StatType.HPRegen: return $"生命回复 +{v:0.#}";
                case StatType.Armor: return $"护甲 +{(int)v}";
                case StatType.MagicResist: return $"魔抗 +{(int)v}";
                case StatType.HealShieldPower: return $"治疗护盾 +{(int)(v * 100f)}%";
                case StatType.AbilityHaste: return $"技能急速 +{(int)v}";
                case StatType.MoveSpeed: return $"移速 +{v:0.#}";
                case StatType.AttackRange: return $"攻击距离 +{v:0.#}";
                case StatType.Size: return $"体型 +{v:0.##}";
                default: return item.displayName;
            }
        }
    }
}