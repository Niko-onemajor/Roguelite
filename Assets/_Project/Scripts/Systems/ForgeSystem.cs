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

    /// <summary>锻体单张卡片数据：基础道具 + 稀有度 + 刷新是否已用。</summary>
    public class ForgeCard
    {
        public ShopItemData Item { get; set; }
        public ForgeRarity Rarity { get; set; }
        public bool RefreshUsed { get; set; }

        public ForgeCard(ShopItemData item, ForgeRarity rarity)
        {
            Item = item;
            Rarity = rarity;
            RefreshUsed = false;
        }

        public float Multiplier() => 1f + 0.5f * (int)Rarity;
        public int UpgradePrice() => 10 * ((int)Rarity + 1);
    }

    /// <summary>锻体系统：海克斯风格 3 选 1，每张限刷新 1 次免费，可花金币升阶。</summary>
    public class ForgeSystem : MonoBehaviour
    {
        /// <summary>可提供的锻体选项池，由 GameBootstrap 从 GameConfig 注入。</summary>
        public List<ShopItemData> Pool { get; set; }

        /// <summary>当前待选择的 3 张卡。</summary>
        public List<ForgeCard> CurrentCards { get; } = new List<ForgeCard>();

        /// <summary>是否正在等待玩家选择。</summary>
        public bool IsAwaitingChoice { get; private set; }

        /// <summary>稀有度权重：索引对应 ForgeRarity，值为权重。总权重=100。</summary>
        public static readonly int[] RarityWeights = { 65, 30, 5 };

        readonly System.Random rng = new System.Random();

        /// <summary>打开锻体选择：抽 3 张不重复的卡片，触发 UI 展示。</summary>
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

        /// <summary>尝试刷新第 idx 张卡：每张只能刷新一次，免费。返回是否成功。</summary>
        public bool TryRefresh(int idx)
        {
            if (!IsAwaitingChoice || idx < 0 || idx >= CurrentCards.Count) return false;
            var card = CurrentCards[idx];
            if (card.RefreshUsed) return false;

            var available = new List<ShopItemData>(Pool);
            foreach (var c in CurrentCards)
                if (c != card) available.Remove(c.Item);

            if (available.Count == 0) return false;

            int selected = rng.Next(available.Count);
            card.Item = available[selected];
            card.Rarity = RollRarity();
            card.RefreshUsed = true;
            return true;
        }

        /// <summary>尝试升阶第 idx 张卡：升一级，扣对应价格。返回是否成功（金币不足已是彩→失败）。</summary>
        public bool TryUpgrade(int idx)
        {
            if (!IsAwaitingChoice || idx < 0 || idx >= CurrentCards.Count) return false;
            var card = CurrentCards[idx];
            if (card.Rarity >= ForgeRarity.Rainbow) return false;

            int price = card.UpgradePrice();
            if (PlayerStats.Instance == null || PlayerStats.Instance.Gold < price) return false;

            PlayerStats.Instance.AddGold(-price);
            card.Rarity++;
            return true;
        }

        /// <summary>选择第 idx 张卡生效，结束锻体阶段。</summary>
        public void Choose(int idx)
        {
            if (!IsAwaitingChoice || idx < 0 || idx >= CurrentCards.Count) return;

            var card = CurrentCards[idx];
            PlayerStats.Instance.ApplyBonus(card.Item, card.Multiplier());

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
    }
}
