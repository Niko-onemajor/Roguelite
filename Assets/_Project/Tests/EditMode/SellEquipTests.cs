using NUnit.Framework;
using Roguelite;
using UnityEngine;

namespace Roguelite.Tests
{
    /// <summary>商店出售：Brotato 式 8 装备栏(含被动装备占槽)、满 8 拒购、出售八折返金、
    /// 属性/被动反向清除、出售后装备重新可刷出、主动装备出栏后不可触发。</summary>
    public class SellEquipTests
    {
        GameObject playerGo;
        PlayerStats stats;
        GameObject shopGo;
        ShopSystem shop;

        [SetUp]
        public void SetUp()
        {
            playerGo = new GameObject("Player", typeof(PlayerStats));
            stats = playerGo.GetComponent<PlayerStats>();
            stats.ResetForRun();
            PlayerStats.Instance = stats;
            stats.AddGold(1000);

            shopGo = new GameObject("Shop", typeof(ShopSystem));
            shop = shopGo.GetComponent<ShopSystem>();
            shop.stats = stats;
        }

        [TearDown]
        public void TearDown()
        {
            if (PlayerStats.Instance == stats) PlayerStats.Instance = null;
            Object.DestroyImmediate(shopGo);
            Object.DestroyImmediate(playerGo);
            GameEvents.ClearAll();
        }

        static ShopItemData Item(string name, StatType type, float value, int price, PassiveType passive = PassiveType.None)
        {
            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.displayName = name;
            item.statType = type;
            item.addValue = value;
            item.basePrice = price;
            item.passiveType = passive;
            return item;
        }

        /// <summary>打开商店并购买 0 号槽。</summary>
        bool BuyFirst()
        {
            shop.OpenOffer();
            return shop.TryPurchase(0);
        }

        [Test]
        public void Sell_Refunds_80_Percent_And_Reverts_Stats()
        {
            var item = Item("测试剑", StatType.AttackDamage, 9f, 40, PassiveType.BlackCleaver);
            shop.pool = new[] { item };

            Assert.That(BuyFirst(), Is.True);
            Assert.That(stats.Gold, Is.EqualTo(1000 - 40));
            Assert.That(stats.damage, Is.EqualTo(10f + 9f).Within(1e-4f));
            Assert.That(stats.HasPassive(PassiveType.BlackCleaver), Is.True);
            Assert.That(stats.EquippedCount, Is.EqualTo(1));

            Assert.That(shop.TrySell(item), Is.True);

            Assert.That(stats.Gold, Is.EqualTo(1000 - 40 + 32)); // 40×0.8=32
            Assert.That(stats.damage, Is.EqualTo(10f).Within(1e-4f)); // 属性还原
            Assert.That(stats.HasPassive(PassiveType.BlackCleaver), Is.False); // 被动清除
            Assert.That(stats.EquippedCount, Is.EqualTo(0)); // 槽位清空
        }

        [Test]
        public void Sell_Refunds_Hp_Reverted_And_Clamped()
        {
            var item = Item("测试甲", StatType.MaxHP, 40f, 30);
            shop.pool = new[] { item };

            Assert.That(BuyFirst(), Is.True);
            Assert.That(stats.maxHP, Is.EqualTo(160f).Within(1e-4f));
            Assert.That(stats.CurrentHP, Is.EqualTo(160f).Within(1e-4f));

            Assert.That(shop.TrySell(item), Is.True);
            Assert.That(stats.maxHP, Is.EqualTo(120f).Within(1e-4f));
            Assert.That(stats.CurrentHP, Is.EqualTo(120f).Within(1e-4f)); // 高于上限被钳回
            Assert.That(stats.Gold, Is.EqualTo(1000 - 30 + 24)); // 30×0.8=24
        }

        [Test]
        public void Sell_Refunds_AttackSpeed_Divisor_Reverted()
        {
            var item = Item("测试鞋", StatType.AttackSpeed, 0.8f, 20);
            shop.pool = new[] { item };

            Assert.That(BuyFirst(), Is.True);
            Assert.That(stats.attackInterval, Is.EqualTo(0.8f * 0.8f).Within(1e-4f));

            Assert.That(shop.TrySell(item), Is.True);
            Assert.That(stats.attackInterval, Is.EqualTo(0.8f).Within(1e-4f)); // 除法还原
        }

        [Test]
        public void Purchase_Rejected_When_Equip_Full_8()
        {
            var pool = new ShopItemData[9];
            for (int i = 0; i < pool.Length; i++)
                pool[i] = Item("装" + i, StatType.AttackDamage, 1f, 10);
            shop.pool = pool;

            for (int i = 0; i < 8; i++)
                Assert.That(BuyFirst(), Is.True, $"第 {i + 1} 件应购买成功");

            Assert.That(stats.EquippedCount, Is.EqualTo(8));
            Assert.That(stats.IsEquipFull, Is.True);
            Assert.That(BuyFirst(), Is.False); // 第 9 件：装备栏满，拒绝购买
            Assert.That(stats.EquippedCount, Is.EqualTo(8));
        }

        [Test]
        public void Sell_Allows_Item_To_Reappear_In_Shop_Pool()
        {
            var item = Item("唯一剑", StatType.AttackDamage, 2f, 10);
            shop.pool = new[] { item };

            Assert.That(BuyFirst(), Is.True);
            shop.OpenOffer();
            Assert.That(shop.Slots[0].Item, Is.Null); // owned 中不再提供

            Assert.That(shop.TrySell(item), Is.True);
            shop.OpenOffer();
            Assert.That(shop.Slots[0].Item, Is.SameAs(item)); // 出售后可重新刷出
        }

        [Test]
        public void Sell_Active_Item_Removes_Slot_And_Cannot_Trigger()
        {
            var item = Item("移速爆发", StatType.MoveSpeed, 1f, 30);
            item.activeType = ActiveType.MoveBurst;
            item.activeCooldown = 5f;
            shop.pool = new[] { item };

            Assert.That(BuyFirst(), Is.True);
            Assert.That(stats.EquipSlots[0].Item, Is.SameAs(item));
            Assert.That(stats.TryUseActive(0, Vector2.zero), Is.True); // 主动可用

            Assert.That(shop.TrySell(item), Is.True);
            Assert.That(stats.EquipSlots[0], Is.Null);
            Assert.That(stats.TryUseActive(0, Vector2.zero), Is.False); // 出栏后不可触发
        }

        [Test]
        public void Sell_Not_Equipped_Item_Returns_False()
        {
            var owned = Item("已装剑", StatType.AttackDamage, 2f, 10);
            var other = Item("未装剑", StatType.AttackDamage, 2f, 10);
            shop.pool = new[] { owned, other };

            Assert.That(BuyFirst(), Is.True);

            Assert.That(shop.TrySell(other), Is.False); // 未装备的道具不可卖
            Assert.That(stats.EquippedCount, Is.EqualTo(1));
            Assert.That(stats.Gold, Is.EqualTo(1000 - 10));
        }
    }
}