using NUnit.Framework;
using Roguelite;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelite.Tests
{
    /// <summary>商店视图回归(Brotato 布局)：3 槽渲染、锁定切换、刷新扣费、结束关闭、结算面板。</summary>
    public class ShopViewTests
    {
        [TearDown]
        public void TearDown()
        {
            GameEvents.ClearAll();
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                Object.DestroyImmediate(go);
        }

        static ShopItemData Item(string name, int price)
        {
            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.displayName = name;
            item.basePrice = price;
            item.statType = StatType.AttackDamage;
            item.addValue = 6f;
            return item;
        }

        static ShopSystem BuildShop(GameObject host, int gold)
        {
            var stats = new GameObject("stats").AddComponent<PlayerStats>();
            stats.AddGold(gold);
            PlayerStats.Instance = stats;
            var sys = host.AddComponent<ShopSystem>();
            sys.stats = stats;
            sys.pool = new[] { Item("A", 10), Item("B", 12), Item("C", 15), Item("D", 20) };
            return sys;
        }

        [Test]
        public void OpenOffer_Renders_Three_Slots_With_Price()
        {
            var canvas = UIBuilder.Canvas();
            var sv = canvas.AddComponent<ShopView>();
            sv.Build(canvas.transform);
            sv.shop = BuildShop(canvas.gameObject, 100);

            sv.shop.OpenOffer();

            Assert.That(canvas.transform.Find("Shop").gameObject.activeSelf, Is.True);
            var label = canvas.transform.Find("Shop/Slot_0/Text").GetComponent<Text>();
            Assert.That(label, Is.Not.Null);
            Assert.That(label.text, Is.EqualTo(sv.shop.Slots[0].Item.displayName)); // 名称独立顶部展示
            var desc = canvas.transform.Find("Shop/Slot_0/Desc_0").GetComponent<Text>();
            Assert.That(desc.text, Does.Contain("攻击力")); // 池中道具均为攻击力词条
            var price = canvas.transform.Find("Shop/Slot_0/Price_0").GetComponent<Text>();
            Assert.That(price.text, Does.Contain("金币"));  // 价格独立底部展示
        }

        [Test]
        public void LockButton_Toggles_Lock_State()
        {
            var canvas = UIBuilder.Canvas();
            var sv = canvas.AddComponent<ShopView>();
            sv.Build(canvas.transform);
            sv.shop = BuildShop(canvas.gameObject, 100);
            sv.shop.OpenOffer();

            var lockBtn = canvas.transform.Find("Shop/Slot_0/Lock_0").GetComponent<Button>();
            lockBtn.onClick.Invoke();
            Assert.That(sv.shop.Slots[0].Locked, Is.True);
            lockBtn.onClick.Invoke();
            Assert.That(sv.shop.Slots[0].Locked, Is.False);
        }

        [Test]
        public void RefreshButton_Deducts_Refresh_Price()
        {
            var canvas = UIBuilder.Canvas();
            var sv = canvas.AddComponent<ShopView>();
            sv.Build(canvas.transform);
            sv.shop = BuildShop(canvas.gameObject, 100);
            sv.shop.OpenOffer();
            int expected = sv.shop.stats.Gold - ShopSystem.RefreshPrice;

            var refreshBtn = canvas.transform.Find("Shop/RefreshShop").GetComponent<Button>();
            refreshBtn.onClick.Invoke();

            Assert.That(sv.shop.stats.Gold, Is.EqualTo(expected));
        }

        [Test]
        public void EndButton_Closes_Shop()
        {
            var canvas = UIBuilder.Canvas();
            var sv = canvas.AddComponent<ShopView>();
            sv.Build(canvas.transform);
            sv.shop = BuildShop(canvas.gameObject, 100);
            sv.shop.OpenOffer();

            var endBtn = canvas.transform.Find("Shop/EndShop").GetComponent<Button>();
            endBtn.onClick.Invoke();

            Assert.That(sv.shop.IsAwaitingChoice, Is.False);
            Assert.That(canvas.transform.Find("Shop").gameObject.activeSelf, Is.False);
        }

        [Test]
        public void GameEnded_ShowsResultPanel()
        {
            var canvas = UIBuilder.Canvas();
            var gov = canvas.AddComponent<GameOverView>();
            gov.Build(canvas.transform);

            GameEvents.RaiseGameEnded(true, 5, 42);

            var panel = canvas.transform.Find("GameOver");
            Assert.That(panel.gameObject.activeSelf, Is.True);
            Assert.That(panel.Find("Title").GetComponent<Text>().text, Is.EqualTo("胜利！"));
            Assert.That(panel.Find("Stats").GetComponent<Text>().text, Does.Contain("42"));
            Assert.That(panel.Find("Restart"), Is.Not.Null);
        }
    }
}