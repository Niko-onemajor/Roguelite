using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Roguelite.Tests
{
    /// <summary>装备导入回归：多属性道具(ShopItemData.bonuses/passive)应用于玩家、文案展示、
    /// 商店按价格加权抽取、GameConfig 默认池数量。</summary>
    public class ItemImportTests
    {
        PlayerStats stats;
        GameObject statsGo;

        [SetUp]
        public void SetUp()
        {
            statsGo = new GameObject("stats");
            stats = statsGo.AddComponent<PlayerStats>();
            stats.ResetForRun();
            PlayerStats.Instance = stats;
        }

        [TearDown]
        public void TearDown()
        {
            if (PlayerStats.Instance == stats) PlayerStats.Instance = null;
            Object.DestroyImmediate(statsGo);
            GameEvents.ClearAll();
        }

        static ShopItemData Item(params StatBonus[] bonuses)
        {
            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.displayName = "测试装备";
            item.basePrice = 30;
            for (int i = 0; i < bonuses.Length; i++) item.bonuses.Add(bonuses[i]);
            return item;
        }

        [Test]
        public void MultiBonus_ApplyBonus_AppliesEachStat()
        {
            var item = Item(new StatBonus(StatType.AttackDamage, 5f),
                            new StatBonus(StatType.MaxHP, 40f),
                            new StatBonus(StatType.Armor, 10f));
            float baseDamage = stats.damage, baseHP = stats.maxHP, baseArmor = stats.armor;

            stats.ApplyBonus(item);

            Assert.That(stats.damage, Is.EqualTo(baseDamage + 5f));
            Assert.That(stats.maxHP, Is.EqualTo(baseHP + 40f));
            Assert.That(stats.armor, Is.EqualTo(baseArmor + 10f));
        }

        [Test]
        public void MultiBonus_ApplyBonus_AttackSpeedMultiplicative()
        {
            var item = Item(new StatBonus(StatType.AttackSpeed, 0.8f));
            float before = stats.attackInterval;

            stats.ApplyBonus(item);

            Assert.That(stats.attackInterval, Is.EqualTo(before * 0.8f).Within(1e-4f));
        }

        [Test]
        public void Describe_MultiBonus_ShowsAllLinesAndPassive()
        {
            var item = Item(new StatBonus(StatType.AttackDamage, 9f),
                            new StatBonus(StatType.CritChance, 0.12f));
            item.passive = "被动：暴击伤害提升";

            string text = StatText.Describe(item);

            Assert.That(text, Does.Contain("攻击力 +9"));
            Assert.That(text, Does.Contain("暴击 +12%"));
            Assert.That(text, Does.Contain("<color=#bf8c3f>被动：暴击伤害提升</color>"));
        }

        [Test]
        public void Describe_SingleStat_StillWorks()
        {
            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.displayName = "攻击力+6";
            item.statType = StatType.AttackDamage;
            item.addValue = 6f;

            Assert.That(StatText.Describe(item), Is.EqualTo("攻击力 +6"));
        }

        [Test]
        public void Shop_WeightedPick_ReturnsOnlyPoolItems()
        {
            var shopGo = new GameObject("shop");
            var shop = shopGo.AddComponent<ShopSystem>();
            shop.stats = stats;

            var pool = new[] { Item(new StatBonus(StatType.AttackDamage, 1f)),
                               Item(new StatBonus(StatType.MaxHP, 10f)),
                               Item(new StatBonus(StatType.Armor, 5f)) };
            pool[0].basePrice = 10; // 廉价常见
            pool[1].basePrice = 38; // 高价稀有
            pool[2].basePrice = 42;
            shop.pool = pool;

            for (int i = 0; i < 200; i++)
            {
                shop.OpenOffer();
                foreach (var slot in shop.Slots)
                {
                    Assert.That(slot.Item, Is.Not.Null);
                    Assert.That(System.Array.IndexOf(pool, slot.Item), Is.GreaterThanOrEqualTo(0));
                }
                shop.End();
            }
            Object.DestroyImmediate(shopGo);
        }

        [Test]
        public void GameConfig_Default_Contains_ImportedEquipment()
        {
            var cfg = GameConfig.Default();
            // 商店池=33 件导入装备(贪婪胫甲等 27 件已于精简时删除)；锻体独立池=10 张基础属性卡(商店不再出现)
            Assert.That(cfg.shopItems.Count, Is.EqualTo(33));
            Assert.That(cfg.forgeItems.Count, Is.EqualTo(10));
            Assert.That(cfg.forgeItems.TrueForAll(s => !s.IsMulti)); // 锻体卡均为单属性

            ShopItemData infinityEdge = cfg.shopItems.Find(s => s.displayName == "无尽之刃");
            Assert.That(infinityEdge, Is.Not.Null);
            Assert.That(infinityEdge.IsMulti, Is.True);
            Assert.That(infinityEdge.bonuses.Count, Is.EqualTo(3));
            Assert.That(infinityEdge.basePrice, Is.EqualTo(42));

            // 霸王血铠：+5AD/+55HP/41金，双被动(专横/报复)
            ShopItemData overlord = cfg.shopItems.Find(s => s.displayName == "霸王血铠");
            Assert.That(overlord, Is.Not.Null);
            Assert.That(overlord.basePrice, Is.EqualTo(41));
            Assert.That(overlord.bonuses.Count, Is.EqualTo(2));
            Assert.That(overlord.bonuses[0].type, Is.EqualTo(StatType.AttackDamage));
            Assert.That(overlord.bonuses[0].value, Is.EqualTo(5f));
            Assert.That(overlord.bonuses[1].type, Is.EqualTo(StatType.MaxHP));
            Assert.That(overlord.bonuses[1].value, Is.EqualTo(55f));
            Assert.That(overlord.passive, Does.Contain("专横").And.Contain("报复"));
            Assert.That(overlord.passiveType, Is.EqualTo(PassiveType.Tyrant));

            int multi = 0, withPassive = 0;
            foreach (var s in cfg.shopItems)
            {
                if (s.IsMulti) multi++;
                if (!string.IsNullOrEmpty(s.passive)) withPassive++;
            }
            Assert.That(multi, Is.EqualTo(33));
            Assert.That(withPassive, Is.EqualTo(29)); // 33 件中 4 件纯属性无文案(无尽之刃/狂战士胫甲/法师之靴/明朗之靴; 虚空之杖已有虚空穿透被动)
        }

        /// <summary>符文池：至少 30 张、名称不重复、全部带机制文案与多重属性。</summary>
        [Test]
        public void Default_Runepool_Valid()
        {
            var cfg = GameConfig.Default();
            Assert.That(cfg.runes.Count, Is.GreaterThanOrEqualTo(30));

            var names = new HashSet<string>();
            foreach (var r in cfg.runes)
            {
                Assert.That(r.displayName, Is.Not.Null.Or.Empty);
                Assert.That(names.Add(r.displayName), Is.True, "符文名称重复: " + r.displayName);
                Assert.That(string.IsNullOrEmpty(r.passive), Is.False, "符文缺机制文案: " + r.displayName);
                Assert.That(r.IsMulti, Is.True, "符文应为多属性卡: " + r.displayName);
            }
        }
    }
}