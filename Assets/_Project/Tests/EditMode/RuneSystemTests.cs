using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Roguelite.Tests
{
    /// <summary>符文系统(海克斯大乱斗风格)：3 卡三选一、每卡 1 次刷新、选择应用增益并记录符文、跳过无加成。</summary>
    public class RuneSystemTests
    {
        RuneSystem rune;
        PlayerStats stats;
        GameObject runeGo;
        GameObject statsGo;
        List<ShopItemData> pool;

        [SetUp]
        public void SetUp()
        {
            runeGo = new GameObject("RuneSystem");
            rune = runeGo.AddComponent<RuneSystem>();

            statsGo = new GameObject("PlayerStats");
            stats = statsGo.AddComponent<PlayerStats>();
            stats.ResetForRun();
            PlayerStats.Instance = stats;

            pool = new List<ShopItemData>
            {
                Item(StatType.AttackDamage, 6f),
                Item(StatType.MaxHP, 20f),
                Item(StatType.MoveSpeed, 0.5f),
                Item(StatType.AttackRange, 1f),
                Item(StatType.CritChance, 0.08f),
                Item(StatType.AttackSpeed, 0.85f),
            };
            rune.pool = pool;
        }

        [TearDown]
        public void TearDown()
        {
            if (PlayerStats.Instance == stats) PlayerStats.Instance = null;
            Object.DestroyImmediate(statsGo);
            Object.DestroyImmediate(runeGo);
            GameEvents.ClearAll();
        }

        [Test]
        public void OpenOffer_Gives_Three_Distinct_Cards()
        {
            rune.OpenOffer();
            Assert.That(rune.Cards, Has.Count.EqualTo(RuneSystem.CardCount));
            Assert.That(rune.IsAwaitingChoice, Is.True);

            var seen = new HashSet<ShopItemData>();
            foreach (var c in rune.Cards)
                Assert.That(seen.Add(c.Item), Is.True, "符文初始 3 卡不应重复");
        }

        [Test]
        public void OpenOffer_Tiny_Pool_Gives_At_Most_Count()
        {
            rune.pool = new List<ShopItemData> { pool[0] };
            rune.OpenOffer();
            Assert.That(rune.Cards, Has.Count.EqualTo(1));
        }

        [Test]
        public void OpenOffer_Raises_Event()
        {
            RuneSystem raised = null;
            GameEvents.RuneOffer += OnRaised;
            try
            {
                rune.OpenOffer();
                Assert.That(raised, Is.SameAs(rune));
            }
            finally
            {
                GameEvents.RuneOffer -= OnRaised;
            }

            void OnRaised(RuneSystem r) => raised = r;
        }

        [Test]
        public void TryRefresh_Limited_To_Once_Per_Card()
        {
            rune.OpenOffer();

            Assert.That(rune.TryRefresh(0), Is.True);
            Assert.That(rune.Cards[0].RefreshUsed, Is.True);
            Assert.That(rune.TryRefresh(0), Is.False); // 第二次刷新被拒绝
            Assert.That(rune.TryRefresh(99), Is.False);
            Assert.That(rune.TryRefresh(-1), Is.False);
        }

        [Test]
        public void Choose_Applies_Bonus_And_Records_Rune_And_Closes()
        {
            rune.OpenOffer();
            rune.Cards[0].Item = pool[0]; // 攻击力 +6
            rune.Cards[0].Rarity = RuneRarity.Gold;
            float before = stats.damage;
            int runeCount = stats.Runes.Count;

            rune.Choose(0);

            Assert.That(stats.damage, Is.EqualTo(before + 6f).Within(0.0001f));
            Assert.That(stats.Runes.Count, Is.EqualTo(runeCount + 1));
            Assert.That(stats.Runes[runeCount].Name, Does.Contain("符文"));
            Assert.That(stats.Runes[runeCount].Desc, Does.Contain("攻击力"));
            Assert.That(rune.IsAwaitingChoice, Is.False);
            Assert.That(rune.Cards, Is.Empty);
        }

        [Test]
        public void Skip_Closes_Without_Bonus()
        {
            rune.OpenOffer();
            float before = stats.damage;

            rune.Skip();

            Assert.That(rune.IsAwaitingChoice, Is.False);
            Assert.That(rune.Cards, Is.Empty);
            Assert.That(stats.damage, Is.EqualTo(before));
        }

        static ShopItemData Item(StatType type, float add)
        {
            var i = ScriptableObject.CreateInstance<ShopItemData>();
            i.displayName = "item " + type;
            i.statType = type;
            i.addValue = add;
            return i;
        }
    }
}