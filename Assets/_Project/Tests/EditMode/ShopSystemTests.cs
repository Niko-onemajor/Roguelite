using NUnit.Framework;
using Roguelite;
using UnityEngine;

namespace Roguelite.Tests
{
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

            shopGo = new GameObject("shop");
            shop = shopGo.AddComponent<ShopSystem>();
            shop.stats = stats;

            pool = new ShopItemData[6];
            for (int i = 0; i < 6; i++)
            {
                var item = ScriptableObject.CreateInstance<ShopItemData>();
                item.displayName = "item" + i;
                item.basePrice = 10;
                item.priceStep = 5;
                item.addValue = 1f;
                pool[i] = item;
            }
            shop.pool = pool;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(shopGo);
            Object.DestroyImmediate(statsGo);
            foreach (var p in pool) Object.DestroyImmediate(p);
            GameEvents.ClearAll();
        }

        [Test]
        public void OpenOffer_Returns_Three_Distinct_Items()
        {
            var offer = shop.OpenOffer();
            Assert.That(offer.items.Count, Is.EqualTo(3));
            Assert.That(new System.Collections.Generic.HashSet<ShopItemData>(offer.items).Count, Is.EqualTo(3));
            Assert.That(shop.IsAwaitingChoice, Is.True);
        }

        [Test]
        public void PriceOf_Increases_After_Purchase()
        {
            int before = shop.PriceOf(pool[0]);
            shop.OpenOffer();
            var purchased = shop.TryPurchase(pool[0]);
            Assert.That(purchased, Is.True);
            Assert.That(shop.PriceOf(pool[0]), Is.EqualTo(before + 5));
        }

        [Test]
        public void TryPurchase_Fails_When_Gold_Short()
        {
            var broke = new GameObject("broke");
            var brokeStats = broke.AddComponent<PlayerStats>();
            var brokeShop = broke.AddComponent<ShopSystem>();
            brokeShop.stats = brokeStats;
            brokeShop.pool = new[] { pool[0] };
            brokeStats.AddGold(5);

            Assert.That(brokeShop.TryPurchase(pool[0]), Is.False);
            Assert.That(brokeStats.Gold, Is.EqualTo(5));
            Object.DestroyImmediate(broke);
        }

        [Test]
        public void TryPurchase_Closes_Offer()
        {
            shop.OpenOffer();
            shop.TryPurchase(pool[0]);
            Assert.That(shop.IsAwaitingChoice, Is.False);
        }
    }
}