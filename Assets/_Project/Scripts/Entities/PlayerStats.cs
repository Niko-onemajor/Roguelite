using UnityEngine;

namespace Roguelite
{
    /// <summary>玩家属性与资源(HP/金币/击杀)，供商店增益与系统读取。</summary>
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; set; }

        public float maxHP = 100f;
        public float damage = 10f;
        public float attackInterval = 0.8f;
        public float range = 6f;
        public float moveSpeed = 3.5f;
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
        }

        public void AddGold(int amount)
        {
            Gold += amount;
            GameEvents.RaiseGold(Gold);
        }

        public void NotifyKill() => Kills++;

        public void ApplyBonus(ShopItemData item)
        {
            switch (item.statType)
            {
                case StatType.Damage: damage += item.addValue; break;
                case StatType.AttackSpeed: attackInterval *= item.addValue; break;
                case StatType.MaxHP:
                    maxHP += item.addValue;
                    CurrentHP += item.addValue;
                    break;
                case StatType.MoveSpeed: moveSpeed += item.addValue; break;
                case StatType.Range: range += item.addValue; break;
                case StatType.CritChance: critChance += item.addValue; break;
                default: return;
            }
            GameEvents.RaiseHP(CurrentHP, maxHP);
        }
    }
}