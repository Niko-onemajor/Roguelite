using UnityEngine;

namespace Roguelite
{
    public enum StatType { Damage, AttackSpeed, MaxHP, MoveSpeed, Range, CritChance }

    [CreateAssetMenu(fileName = "ShopItemData", menuName = "Roguelite/ShopItemData")]
    public class ShopItemData : ScriptableObject
    {
        public string displayName = "增益";
        public StatType statType;
        public int basePrice = 10;
        public int priceStep = 5;   // 每购买一次价格递增
        public float addValue = 1f; // 每次加成(AttackSpeed 为乘积系数 0.85)
        public Color color = Color.white;
    }
}