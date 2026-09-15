using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Roguelite.Tests
{
    /// <summary>锻体系统：付费开启、3 卡生成、倍率选卡、符文记录。</summary>
    public class ForgeSystemTests
    {
        ForgeSystem forge;
        PlayerStats stats;
        GameObject forgeGo;
        GameObject statsGo;

        List<ShopItemData> pool;

        [SetUp]
        public void SetUp()
        {
            forgeGo = new GameObject("ForgeSystem");
            forge = forgeGo.AddComponent<ForgeSystem>();

            statsGo = new GameObject("PlayerStats");
            stats = statsGo.AddComponent<PlayerStats>();
            stats.ResetForRun();
            PlayerStats.Instance = stats;

            pool = new List<ShopItemData>
            {
                Item(StatType.Damage, 6f),
                Item(StatType.MaxHP, 20f),
                Item(StatType.MoveSpeed, 0.5f),
                Item(StatType.Range, 1f),
                Item(StatType.CritChance, 0.08f),
                Item(StatType.AttackSpeed, 0.85f),
            };
        }

        [TearDown]
        public void TearDown()
        {
            if (PlayerStats.Instance == stats) PlayerStats.Instance = null;
            Object.DestroyImmediate(statsGo);
            Object.DestroyImmediate(forgeGo);
            GameEvents.ClearAll();
        }

        [Test]
        public void OpenForge_Gives_Exactly_3_Cards_From_Pool()
        {
            forge.OpenForge(pool);
            Assert.That(forge.CurrentCards, Has.Count.EqualTo(3));
            Assert.That(forge.IsAwaitingChoice, Is.True);

            var seen = new HashSet<ShopItemData>();
            foreach (var card in forge.CurrentCards)
                Assert.That(seen.Add(card.Item), Is.True, "卡池初始 3 卡不应重复");
        }

        [Test]
        public void OpenForge_With_Tiny_Pool_Still_Gives_At_Most_Count()
        {
            forge.OpenForge(new List<ShopItemData> { pool[0] });
            Assert.That(forge.CurrentCards, Has.Count.EqualTo(1));
        }

        [Test]
        public void OpenForge_Raises_Event()
        {
            ForgeSystem raised = null;
            GameEvents.ForgeOffer += OnRaised;
            try
            {
                forge.OpenForge(pool);
                Assert.That(raised, Is.SameAs(forge));
            }
            finally
            {
                GameEvents.ForgeOffer -= OnRaised;
            }

            void OnRaised(ForgeSystem f) => raised = f;
        }

        [Test]
        public void TryOpenForge_Pays_10_And_Opens()
        {
            forge.Pool = pool;
            stats.AddGold(25);
            Assert.That(ForgeSystem.OpenPrice, Is.EqualTo(10));

            bool ok = forge.TryOpenForge();

            Assert.That(ok, Is.True);
            Assert.That(stats.Gold, Is.EqualTo(15));
            Assert.That(forge.IsAwaitingChoice, Is.True);
            Assert.That(forge.CurrentCards, Has.Count.EqualTo(3));
        }

        [Test]
        public void TryOpenForge_No_Gold_Fails_Without_Deduct()
        {
            stats.AddGold(9); // 不足 10
            int before = stats.Gold;

            bool ok = forge.TryOpenForge();

            Assert.That(ok, Is.False);
            Assert.That(stats.Gold, Is.EqualTo(before)); // 未扣费
            Assert.That(forge.IsAwaitingChoice, Is.False);
        }

        [Test]
        public void TryOpenForge_Already_Awaiting_Rejected()
        {
            forge.Pool = pool;
            stats.AddGold(999);
            forge.TryOpenForge();
            int before = stats.Gold;

            Assert.That(forge.TryOpenForge(), Is.False);
            Assert.That(stats.Gold, Is.EqualTo(before)); // 二次开启不重复扣费
        }

        [Test]
        public void Choose_Applies_Bonus_With_Rarity_Multiplier_And_Closes()
        {
            forge.OpenForge(pool);
            var card = forge.CurrentCards[0];
            card.Item = pool[0]; // Damage +6
            card.Rarity = ForgeRarity.Gold; // 1.5x → +9
            float before = stats.damage;

            forge.Choose(0);

            Assert.That(stats.damage, Is.EqualTo(before + 9f).Within(0.0001f));
            Assert.That(forge.IsAwaitingChoice, Is.False);
            Assert.That(forge.CurrentCards, Is.Empty);
        }

        [Test]
        public void Choose_Records_Rune_Info()
        {
            forge.OpenForge(pool);
            var card = forge.CurrentCards[0];
            card.Item = pool[0];
            card.Rarity = ForgeRarity.Rainbow;
            int before = stats.Runes.Count;

            forge.Choose(0);

            Assert.That(stats.Runes.Count, Is.EqualTo(before + 1));
            Assert.That(stats.Runes[before].Name, Does.Contain("传说"));
            Assert.That(stats.Runes[before].Desc, Does.Contain("伤害"));
        }

        [Test]
        public void Skip_Closes_Without_Bonus()
        {
            forge.OpenForge(pool);
            stats.AddGold(10);
            forge.TryOpenForge();
            float before = stats.damage;

            forge.Skip();

            Assert.That(forge.IsAwaitingChoice, Is.False);
            Assert.That(forge.CurrentCards.Count, Is.Zero);
            Assert.That(stats.damage, Is.EqualTo(before));
        }

        [Test]
        public void RollRarity_Weights_Stay_In_Bounds()
        {
            const int iterations = 3000;
            var counts = new int[3];
            for (int i = 0; i < iterations; i++)
                counts[(int)ForgeSystem.RollRarity()]++;

            // 白 65%、金 30%、彩 5%，容差 ±4%
            Assert.That(counts[0] / (float)iterations, Is.InRange(0.61f, 0.69f));
            Assert.That(counts[1] / (float)iterations, Is.InRange(0.26f, 0.34f));
            Assert.That(counts[2] / (float)iterations, Is.InRange(0.01f, 0.09f));
        }

        static ShopItemData Item(StatType type, float add)
        {
            var i = ScriptableObject.CreateInstance<ShopItemData>();
            i.statType = type;
            i.addValue = add;
            return i;
        }
    }
}