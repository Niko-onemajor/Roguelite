using NUnit.Framework;
using Roguelite;
using UnityEngine;

namespace Roguelite.Tests
{
    /// <summary>商店系统(Brotato 风格)：3 槽展示、锁定/解锁、刷新10金币保留锁定槽、固定价购买、结束关闭。</summary>
    public class ShopSystemTests
    {
        PlayerStats stats;
        GameObject statsGo;
        ShopSystem shop;
        GameObject shopGo;
        ShopItemData[] pool;

        [SetUp]
        public void SetUp()
        {
            statsGo = new GameObject("stats");
            stats = statsGo.AddComponent<PlayerStats>();
            stats.AddGold(100);
            PlayerStats.Instance = stats;

            shopGo = new GameObject("shop");
            shop = shopGo.AddComponent<ShopSystem>();
            shop.stats = stats;

            pool = new ShopItemData[6];
            for (int i = 0; i < 6; i++)
            {
                var item = ScriptableObject.CreateInstance<ShopItemData>();
                item.displayName = "item" + i;
                item.basePrice = 10;
                item.addValue = 1f;
                pool[i] = item;
            }
            shop.pool = pool;
        }

        [TearDown]
        public void TearDown()
        {
            if (PlayerStats.Instance == stats) PlayerStats.Instance = null;
            Object.DestroyImmediate(shopGo);
            Object.DestroyImmediate(statsGo);
            foreach (var p in pool) Object.DestroyImmediate(p);
            GameEvents.ClearAll();
        }

        [Test]
        public void OpenOffer_Fills_Three_Slots()
        {
            shop.OpenOffer();
            Assert.That(shop.Slots, Has.Count.EqualTo(3));
            foreach (var slot in shop.Slots) Assert.That(slot.Item, Is.Not.Null);
            Assert.That(shop.IsAwaitingChoice, Is.True);
        }

        [Test]
        public void TryPurchase_Deducts_Fixed_Price_And_Empties_Slot()
        {
            shop.OpenOffer();
            int price = shop.Slots[0].Item.basePrice;
            int before = stats.Gold;

            Assert.That(shop.TryPurchase(0), Is.True);
            Assert.That(stats.Gold, Is.EqualTo(before - price));
            Assert.That(shop.Slots[0].Item, Is.Null);
            Assert.That(shop.IsAwaitingChoice, Is.True); // 商店保持打开，可继续购买
        }

        [Test]
        public void TryPurchase_Fails_When_Gold_Short()
        {
            var broke = new GameObject("broke");
            var brokeStats = broke.AddComponent<PlayerStats>();
            var brokeShop = broke.AddComponent<ShopSystem>();
            brokeShop.stats = brokeStats;
            brokeShop.pool = new[] { pool[0] };
            brokeShop.OpenOffer();
            brokeStats.AddGold(5);

            for (int i = 0; i < brokeShop.Slots.Count; i++)
            {
                if (brokeShop.Slots[i].Item == null) continue;
                Assert.That(brokeShop.TryPurchase(i), Is.False);
            }
            Assert.That(brokeStats.Gold, Is.EqualTo(5));
            Object.DestroyImmediate(broke);
        }

        [Test]
        public void TryPurchase_Sold_Slot_Fails()
        {
            shop.OpenOffer();
            shop.TryPurchase(0);
            Assert.That(shop.TryPurchase(0), Is.False);
        }

        [Test]
        public void ToggleLock_Toggles_Slot()
        {
            shop.OpenOffer();
            shop.ToggleLock(1);
            Assert.That(shop.Slots[1].Locked, Is.True);
            shop.ToggleLock(1);
            Assert.That(shop.Slots[1].Locked, Is.False);
        }

        [Test]
        public void ToggleLock_EmptySlot_Rejected()
        {
            shop.OpenOffer();
            shop.TryPurchase(0); // 槽 0 变空
            shop.ToggleLock(0);
            Assert.That(shop.Slots[0].Locked, Is.False);
        }

        [Test]
        public void TryRefresh_Costs_Ten_And_Keeps_Locked_Slots()
        {
            shop.OpenOffer();
            shop.ToggleLock(0);
            var kept = shop.Slots[0].Item;
            int before = stats.Gold;

            Assert.That(shop.TryRefresh(), Is.True);
            Assert.That(stats.Gold, Is.EqualTo(before - ShopSystem.RefreshPrice));
            Assert.That(shop.Slots[0].Item, Is.SameAs(kept)); // 锁定槽保持原货
            Assert.That(shop.Slots[0].Locked, Is.True);
        }

        [Test]
        public void TryRefresh_Fails_Without_Gold()
        {
            var broke = new GameObject("broke");
            var brokeStats = broke.AddComponent<PlayerStats>();
            var brokeShop = broke.AddComponent<ShopSystem>();
            brokeShop.stats = brokeStats;
            brokeShop.pool = pool;
            brokeStats.AddGold(5);
            brokeShop.OpenOffer();
            int before = brokeStats.Gold;

            Assert.That(brokeShop.TryRefresh(), Is.False);
            Assert.That(brokeStats.Gold, Is.EqualTo(before));
            Object.DestroyImmediate(broke);
        }

        [Test]
        public void End_Clears_Locks_And_Closes()
        {
            shop.OpenOffer();
            shop.ToggleLock(2);
            shop.End();
            Assert.That(shop.IsAwaitingChoice, Is.False);
            foreach (var s in shop.Slots) Assert.That(s.Locked, Is.False);
        }

        [Test]
        public void Actions_Ignored_When_Closed()
        {
            Assert.That(shop.TryRefresh(), Is.False);
            Assert.That(shop.TryPurchase(0), Is.False);
            shop.ToggleLock(0); // 未开店不应抛异常
            Assert.That(shop.Slots, Has.Count.EqualTo(0));
        }
    }
}