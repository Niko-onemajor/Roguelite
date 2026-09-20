using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>符文稀有度(视觉配色用；效果数值仍走 StatText)。</summary>
    public enum RuneRarity { Common, Gold, Purple }

    /// <summary>单张符文卡：效果道具 + 稀有度 + 是否已用刷新。</summary>
    public class RuneCard
    {
        public ShopItemData Item;
        public RuneRarity Rarity;
        public bool RefreshUsed;
    }

    /// <summary>符文系统(海克斯大乱斗风格)：开局/第7/11/15波开始前弹出，
    /// 3 张符文三选一，每张各 1 次刷新机会；效果应用并记录到玩家符文列表。</summary>
    public class RuneSystem : MonoBehaviour
    {
        public List<ShopItemData> pool;

        public const int CardCount = 3;
        public static readonly int[] RarityWeights = { 60, 30, 10 }; // 白/金/紫

        readonly System.Random rng = new System.Random();
        public readonly List<RuneCard> Cards = new List<RuneCard>();

        public bool IsAwaitingChoice { get; private set; }

        /// <summary>弹出 3 张不重复符文卡。池为空时直接结束选择，避免卡死波次流程。</summary>
        public void OpenOffer()
        {
            IsAwaitingChoice = true;
            Cards.Clear();

            if (pool == null || pool.Count == 0)
            {
                IsAwaitingChoice = false;
                GameEvents.RaiseRuneOffer(this);
                return;
            }

            var available = new List<ShopItemData>(pool);
            while (Cards.Count < CardCount && available.Count > 0)
            {
                int idx = rng.Next(available.Count);
                var item = available[idx];
                available.RemoveAt(idx);
                Cards.Add(new RuneCard { Item = item, Rarity = RollRarity() });
            }

            GameEvents.RaiseRuneOffer(this);
        }

        /// <summary>刷新第 idx 张符文：每张仅限 1 次，刷新后稀有度重掷。</summary>
        public bool TryRefresh(int idx)
        {
            if (!IsAwaitingChoice || idx < 0 || idx >= Cards.Count) return false;
            RuneCard card = Cards[idx];
            if (card.RefreshUsed) return false;

            var available = new List<ShopItemData>(pool);
            foreach (var c in Cards)
                if (c != card) available.Remove(c.Item);
            if (available.Count == 0) return false;

            card.Item = available[rng.Next(available.Count)];
            card.Rarity = RollRarity();
            card.RefreshUsed = true;
            return true;
        }

        /// <summary>选择第 idx 张符文生效：应用增益、记录符文、特效音效反馈，结束选择。</summary>
        public void Choose(int idx)
        {
            if (!IsAwaitingChoice || idx < 0 || idx >= Cards.Count) return;
            RuneCard card = Cards[idx];

            PlayerStats.Instance.ApplyBonus(card.Item);
            PlayerStats.Instance.RecordRune(
                $"符文 · {card.Item.displayName}",
                StatText.Describe(card.Item),
                RarityColor(card.Rarity),
                RuneSource.Rune);
            Vector3 fxPos = PlayerController.Instance != null
                ? PlayerController.Instance.transform.position
                : transform.position;
            FxPlayer.LevelUp(fxPos, RarityColor(card.Rarity), RarityPitch(card.Rarity));

            IsAwaitingChoice = false;
            Cards.Clear();
        }

        /// <summary>跳过本次符文选择（无加成）。</summary>
        public void Skip()
        {
            if (!IsAwaitingChoice) return;
            IsAwaitingChoice = false;
            Cards.Clear();
        }

        public static RuneRarity RollRarity()
        {
            int roll = UnityEngine.Random.Range(0, 100);
            int cum = 0;
            for (int i = 0; i < RarityWeights.Length; i++)
            {
                cum += RarityWeights[i];
                if (roll < cum) return (RuneRarity)i;
            }
            return RuneRarity.Common;
        }

        static Color RarityColor(RuneRarity r)
        {
            switch (r)
            {
                case RuneRarity.Gold: return new Color(1f, 0.85f, 0.2f);
                case RuneRarity.Purple: return new Color(0.8f, 0.45f, 1f);
                default: return new Color(0.9f, 0.9f, 0.95f);
            }
        }

        static float RarityPitch(RuneRarity r)
        {
            switch (r)
            {
                case RuneRarity.Gold: return 880f;
                case RuneRarity.Purple: return 1100f;
                default: return 660f;
            }
        }
    }
}