using System;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>海克斯风格锻体稀有度：越高倍率越大。</summary>
    public enum ForgeRarity
    {
        White = 0,  // 1.0x
        Gold = 1,   // 1.5x
        Rainbow = 2 // 2.0x
    }

    /// <summary>锻体单张卡片数据：基础道具 + 稀有度。</summary>
    public class ForgeCard
    {
        public ShopItemData Item { get; set; }
        public ForgeRarity Rarity { get; set; }

        public ForgeCard(ShopItemData item, ForgeRarity rarity)
        {
            Item = item;
            Rarity = rarity;
        }

        public float Multiplier() => 1f + 0.5f * (int)Rarity;
    }

    /// <summary>锻体系统：花费 10 金币开启，海克斯风格 3 选 1（无升阶/刷新）。</summary>
    public class ForgeSystem : MonoBehaviour
    {
        /// <summary>开启锻体界面的金币费用。</summary>
        public const int OpenPrice = 10;

        /// <summary>可提供的锻体选项池，由 GameBootstrap 从 GameConfig 注入。</summary>
        public List<ShopItemData> Pool { get; set; }

        /// <summary>当前待选择的 3 张卡。</summary>
        public List<ForgeCard> CurrentCards { get; } = new List<ForgeCard>();

        /// <summary>是否正在等待玩家选择。</summary>
        public bool IsAwaitingChoice { get; private set; }

        /// <summary>稀有度权重：索引对应 ForgeRarity，值为权重。总权重=100。</summary>
        public static readonly int[] RarityWeights = { 65, 30, 5 };

        readonly System.Random rng = new System.Random();

        /// <summary>付费开启锻体：金币不足返回 false；足够则扣 10 金币并展示 3 选 1。</summary>
        public bool TryOpenForge()
        {
            if (IsAwaitingChoice) return false;
            if (PlayerStats.Instance == null || PlayerStats.Instance.Gold < OpenPrice) return false;
            PlayerStats.Instance.AddGold(-OpenPrice);
            OpenForge();
            return true;
        }

        /// <summary>打开锻体选择：抽 3 张不重复的卡片，触发 UI 展示。(费用由 TryOpenForge 扣除)</summary>
        public void OpenForge() => OpenForge(Pool);

        /// <summary>打开锻体选择：抽 3 张不重复的卡片，触发 UI 展示。</summary>
        public void OpenForge(List<ShopItemData> pool)
        {
            Pool = pool;
            CurrentCards.Clear();
            IsAwaitingChoice = true;

            var available = new List<ShopItemData>(Pool);
            while (CurrentCards.Count < 3 && available.Count > 0)
            {
                int idx = rng.Next(available.Count);
                var item = available[idx];
                available.RemoveAt(idx);
                var rarity = RollRarity();
                CurrentCards.Add(new ForgeCard(item, rarity));
            }

            GameEvents.RaiseForgeOffer(this);
        }

        /// <summary>按权重抽一张稀有度。</summary>
        public static ForgeRarity RollRarity()
        {
            int roll = UnityEngine.Random.Range(0, 100);
            int cum = 0;
            for (int i = 0; i < RarityWeights.Length; i++)
            {
                cum += RarityWeights[i];
                if (roll < cum) return (ForgeRarity)i;
            }
            return ForgeRarity.White;
        }

        /// <summary>选择第 idx 张卡生效：应用倍率增益、记录符文、触发升级特效音效，结束锻体阶段。</summary>
        public void Choose(int idx)
        {
            if (!IsAwaitingChoice || idx < 0 || idx >= CurrentCards.Count) return;

            var card = CurrentCards[idx];
            PlayerStats.Instance.ApplyBonus(card.Item, card.Multiplier());
            RecordRune(card);
            Vector3 fxPos = PlayerController.Instance != null
                ? PlayerController.Instance.transform.position
                : transform.position;
            FxPlayer.LevelUp(fxPos, RarityColor(card.Rarity), RarityPitch(card.Rarity));

            IsAwaitingChoice = false;
            CurrentCards.Clear();
        }

        /// <summary>跳过本次锻体，不选卡（无加成）。</summary>
        public void Skip()
        {
            if (!IsAwaitingChoice) return;
            IsAwaitingChoice = false;
            CurrentCards.Clear();
        }

        void RecordRune(ForgeCard card)
        {
            if (PlayerStats.Instance == null) return;
            PlayerStats.Instance.RecordRune(
                $"{RarityName(card.Rarity)} · {card.Item.displayName}",
                BonusText(card),
                RarityColor(card.Rarity));
        }

        static string BonusText(ForgeCard card)
        {
            switch (card.Item.statType)
            {
                case StatType.Damage: return $"伤害 +{(int)(card.Item.addValue * card.Multiplier())}";
                case StatType.AttackSpeed: return $"攻速 ×{Mathf.Pow(card.Item.addValue, card.Multiplier()):0.##}";
                case StatType.MaxHP: return $"生命 +{(int)(card.Item.addValue * card.Multiplier())}";
                case StatType.MoveSpeed: return $"移速 +{(card.Item.addValue * card.Multiplier()):0.#}";
                case StatType.Range: return $"射程 +{(card.Item.addValue * card.Multiplier()):0.#}";
                case StatType.CritChance: return $"暴击 +{(int)(card.Item.addValue * card.Multiplier() * 100f)}%";
                default: return card.Item.displayName;
            }
        }

        static string RarityName(ForgeRarity r)
        {
            switch (r)
            {
                case ForgeRarity.Gold: return "金卡";
                case ForgeRarity.Rainbow: return "传说";
                default: return "白卡";
            }
        }

        static Color RarityColor(ForgeRarity r)
        {
            switch (r)
            {
                case ForgeRarity.Gold: return new Color(1f, 0.85f, 0.2f);
                case ForgeRarity.Rainbow: return new Color(0.8f, 0.45f, 1f);
                default: return new Color(1f, 1f, 1f);
            }
        }

        static float RarityPitch(ForgeRarity r)
        {
            switch (r)
            {
                case ForgeRarity.Gold: return 880f;
                case ForgeRarity.Rainbow: return 1100f;
                default: return 660f;
            }
        }
    }
}
