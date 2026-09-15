using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>已获取符文(每回合锻体选择的卡牌效果)，用于暂停面板回看。</summary>
    public sealed class RuneInfo
    {
        public string Name;   // 卡牌名：如 伤害+6
        public string Desc;   // 生效后效果描述：如 金卡 ×1.5
        public Color Color;   // 稀有度对应颜色
    }

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

        /// <summary>自动入库金币累计值(回合末未拾取金币自动结算的部分)。</summary>
        public int BankedGold { get; private set; }

        /// <summary>本局已获取的符文(每回合锻体选择的卡牌效果)，供暂停面板查看。</summary>
        public readonly List<RuneInfo> Runes = new List<RuneInfo>();

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
            BankedGold = 0;
            Runes.Clear();
            GameEvents.RaiseHP(CurrentHP, maxHP);
            GameEvents.RaiseGold(0);
            GameEvents.RaiseGoldBanked(0);
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

        /// <summary>回合末自动入库金币：计入 Gold 与 BankedGold 累计，并广播入库事件(HUD 显示)。</summary>
        public void BankGold(int amount)
        {
            if (amount <= 0) return;
            BankedGold += amount;
            AddGold(amount);
            GameEvents.RaiseGoldBanked(BankedGold);
        }

        /// <summary>记录一枚符文(锻体选定卡牌的效果)，供暂停面板查看。</summary>
        public void RecordRune(string name, string desc, Color color)
        {
            Runes.Add(new RuneInfo { Name = name, Desc = desc, Color = color });
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