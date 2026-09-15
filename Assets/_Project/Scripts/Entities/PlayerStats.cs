using UnityEngine;

namespace Roguelite
{
    /// <summary>玩家属性与资源(HP/金币/击杀)，供商店增益与系统读取。</summary>
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; set; }

        public float maxHP = 120f;
        public float damage = 10f;
        public float attackInterval = 0.8f;
        public float range = 6f;
        public float moveSpeed = 8f; // 追兵3.2，差距2.5倍：能被甩开但需走位
        public float critChance = 0.05f;
        public float critMultiplier = 2f;
        public float pickupRadius = 2.5f;

        public float CurrentHP { get; private set; }
        public int Gold { get; private set; }
        public int Kills { get; private set; }

        void OnEnable() => Instance = this;

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        public void ResetForRun()
        {
            CurrentHP = maxHP;
            Gold = 0;
            Kills = 0;
            GameEvents.RaiseHP(CurrentHP, maxHP);
            GameEvents.RaiseGold(0);
        }

        public void TakeDamage(float dmg)
        {
            CurrentHP = Mathf.Max(0f, CurrentHP - dmg);
            GameEvents.RaiseHP(CurrentHP, maxHP);
            var flash = GetComponent<HitFlash>(); // 玩家受击反馈：闪红
            if (flash != null) flash.Flash(Color.red, 0.12f);
        }

        public void AddGold(int amount)
        {
            Gold += amount;
            GameEvents.RaiseGold(Gold);
        }

        public void NotifyKill() => Kills++;

        public void ApplyBonus(ShopItemData item) => ApplyBonus(item, 1f);

        /// <summary>锻体增益：multiplier 为稀有度倍率(白1x/金1.5x/彩2x)。
        /// AttackSpeed 是乘算词条，用 addValue^multiplier 使高阶收益递增(更强更快)，其余加算。</summary>
        public void ApplyBonus(ShopItemData item, float multiplier)
        {
            switch (item.statType)
            {
                case StatType.Damage: damage += item.addValue * multiplier; break;
                case StatType.AttackSpeed: attackInterval *= Mathf.Pow(item.addValue, multiplier); break;
                case StatType.MaxHP:
                    float bonus = item.addValue * multiplier;
                    maxHP += bonus;
                    CurrentHP += bonus;
                    break;
                case StatType.MoveSpeed: moveSpeed += item.addValue * multiplier; break;
                case StatType.Range: range += item.addValue * multiplier; break;
                case StatType.CritChance: critChance += item.addValue * multiplier; break;
                default: return;
            }
            GameEvents.RaiseHP(CurrentHP, maxHP);
        }
    }
}