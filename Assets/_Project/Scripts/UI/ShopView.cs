using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>商店视图：渲染 3 张增益卡片，点击购买；无金币/未开店则不响应。</summary>
    public class ShopView : MonoBehaviour
    {
        /// <summary>由 GameBootstrap 注入。</summary>
        public ShopSystem shop;

        GameObject panel;
        readonly List<Card> cards = new List<Card>();

        sealed class Card
        {
            public GameObject root;
            public Text label;
            public Button button;
        }

        public void Build(Transform parent)
        {
            panel = UIBuilder.Panel("Shop", parent);
            panel.SetActive(false);
            for (int i = 0; i < 3; i++) BuildCard(i);
        }

        void BuildCard(int i)
        {
            var root = UIBuilder.Button("Card_" + i, panel.transform, "", null);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.12f + 0.26f * i, 0.3f);
            rt.anchorMax = new Vector2(0.38f + 0.26f * i, 0.7f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            cards.Add(new Card { root = root, label = root.GetComponent<Text>(), button = root.GetComponent<Button>() });
        }

        #region Unity Lifecycle
        void OnEnable() => GameEvents.ShopOpened += OnShopOpened;

        void OnDisable() => GameEvents.ShopOpened -= OnShopOpened;
        #endregion

        #region Event Handlers
        void OnShopOpened(ShopOffer offer)
        {
            if (panel == null) return;
            panel.SetActive(true);
            for (int i = 0; i < cards.Count; i++)
            {
                bool has = i < offer.items.Count;
                cards[i].root.SetActive(has);
                if (!has) continue;
                var item = offer.items[i];
                var price = offer.prices[i];
                cards[i].label.text = $"{item.displayName}\n{price} 金币";
                cards[i].button.onClick.RemoveAllListeners();
                int idx = i;
                cards[i].button.onClick.AddListener(() => TryBuy(cards[idx], item));
            }
        }

        void TryBuy(Card card, ShopItemData item)
        {
            if (shop == null || !shop.TryPurchase(item)) return;
            foreach (var c in cards) c.button.onClick.RemoveAllListeners();
            panel.SetActive(false);
        }
        #endregion
    }
}