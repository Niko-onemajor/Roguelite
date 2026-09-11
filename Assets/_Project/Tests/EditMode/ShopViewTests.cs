using NUnit.Framework;
using Roguelite;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelite.Tests
{
    /// <summary>商店/结算 UI 回归：卡片标签渲染、跳过按钮、结算面板显示。</summary>
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
            item.priceStep = 5;
            item.statType = StatType.Damage;
            item.addValue = 6f;
            return item;
        }

        [Test]
        public void OpenOffer_SetsCardLabels_NoThrow()
        {
            var canvas = UIBuilder.Canvas();
            var sv = canvas.AddComponent<ShopView>();
            sv.Build(canvas.transform);

            var offer = new ShopOffer();
            for (int i = 0; i < 3; i++)
            {
                offer.items.Add(Item("效果_" + i, 10 + i));
                offer.prices.Add(10 + i);
            }
            Assert.DoesNotThrow(() => GameEvents.RaiseShop(offer));

            var panel = canvas.transform.Find("Shop");
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.gameObject.activeSelf, Is.True);
            var label = panel.Find("Card_0/Text").GetComponent<Text>();
            Assert.That(label, Is.Not.Null);
            Assert.That(label.text, Does.Contain("效果_0"));
            Assert.That(label.text, Does.Contain("10 金币"));
        }

        [Test]
        public void SkipButton_ClosesOfferAndAdvances()
        {
            var canvas = UIBuilder.Canvas();
            var sv = canvas.AddComponent<ShopView>();
            sv.Build(canvas.transform);

            var sys = new GameObject("shop").AddComponent<ShopSystem>();
            sv.shop = sys;
            sys.pool = new[] { Item("A", 10), Item("B", 10), Item("C", 10) };
            sys.OpenOffer();
            Assert.That(sys.IsAwaitingChoice, Is.True);

            var skipBtn = canvas.transform.Find("Shop/Skip").GetComponent<Button>();
            Assert.That(skipBtn, Is.Not.Null);
            skipBtn.onClick.Invoke();

            Assert.That(sys.IsAwaitingChoice, Is.False);
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