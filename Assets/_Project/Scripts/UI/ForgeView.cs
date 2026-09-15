using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>锻体 UI：海克斯风格 3 选 1 卡牌，稀有度底色、悬停放大、刷新/升阶按钮。</summary>
    public class ForgeView : MonoBehaviour
    {
        /// <summary>由 GameBootstrap 注入。</summary>
        public ForgeSystem forge;

        GameObject panel;
        bool _subscribed;
        readonly List<CardUI> cards = new List<CardUI>();

        sealed class CardUI
        {
            public GameObject root;
            public Button button;
            public Text label;
            public Button refreshBtn;
            public Text refreshLabel;
            public Button upgradeBtn;
            public Text upgradeLabel;
        }

        public void Build(Transform parent)
        {
            panel = UIBuilder.Panel("Forge", parent);
            panel.SetActive(false);
            if (!_subscribed)
            {
                _subscribed = true;
                GameEvents.ForgeOffer += OnForgeOffered;
            }
            for (int i = 0; i < 3; i++) BuildCard(i);

            var hint = UIBuilder.Text("ForgeHint", panel.transform,
                "锤选 1 张卡强化本回合 · 点击卡牌生效 · 每卡可免费刷新 1 次 · 金币可升阶",
                26, new Color(0.9f, 0.9f, 0.9f), TextAnchor.MiddleCenter);
            var hrt = hint.rectTransform;
            hrt.anchorMin = new Vector2(0.2f, 0.32f);
            hrt.anchorMax = new Vector2(0.8f, 0.4f);
            hrt.offsetMin = Vector2.zero;
            hrt.offsetMax = Vector2.zero;

            var skip = UIBuilder.Button("SkipForge", panel.transform, "跳过本次锻体", Skip);
            var rt = skip.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.4f, 0.16f);
            rt.anchorMax = new Vector2(0.6f, 0.24f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        void Skip()
        {
            if (forge == null) return;
            forge.Skip();
            panel.SetActive(false);
        }

        void BuildCard(int i)
        {
            var root = UIBuilder.Button("ForgeCard_" + i, panel.transform, "", null);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.12f + 0.26f * i, 0.58f);
            rt.anchorMax = new Vector2(0.38f + 0.26f * i, 0.94f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            AddHoverScale(root, 1.08f);
            var btn = root.GetComponent<Button>();

            var refresh = UIBuilder.Button("ForgeRefresh_" + i, panel.transform, "刷新", null);
            var rrt = refresh.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.13f + 0.26f * i, 0.46f);
            rrt.anchorMax = new Vector2(0.24f + 0.26f * i, 0.56f);
            rrt.offsetMin = Vector2.zero;
            rrt.offsetMax = Vector2.zero;

            var upgrade = UIBuilder.Button("ForgeUpgrade_" + i, panel.transform, "升阶", null);
            var urt = upgrade.GetComponent<RectTransform>();
            urt.anchorMin = new Vector2(0.26f + 0.26f * i, 0.46f);
            urt.anchorMax = new Vector2(0.37f + 0.26f * i, 0.56f);
            urt.offsetMin = Vector2.zero;
            urt.offsetMax = Vector2.zero;

            cards.Add(new CardUI
            {
                root = root,
                button = btn,
                label = root.GetComponentInChildren<Text>(true),
                refreshBtn = refresh.GetComponent<Button>(),
                refreshLabel = refresh.GetComponentInChildren<Text>(true),
                upgradeBtn = upgrade.GetComponent<Button>(),
                upgradeLabel = upgrade.GetComponentInChildren<Text>(true),
            });
        }

        #region Unity Lifecycle
        void OnDisable()
        {
            if (!_subscribed) return;
            _subscribed = false;
            GameEvents.ForgeOffer -= OnForgeOffered;
        }
        #endregion

        #region Event Handlers
        void OnForgeOffered(ForgeSystem system)
        {
            forge = system;
            if (panel == null) return;
            panel.SetActive(true);
            for (int i = 0; i < cards.Count; i++)
            {
                bool has = i < system.CurrentCards.Count;
                cards[i].root.SetActive(has);
                if (has) RefreshCard(i);
            }
        }

        void RefreshCard(int idx)
        {
            var c = cards[idx];
            var card = forge.CurrentCards[idx];

            c.label.text = CardText(card);
            c.label.fontSize = 30;
            c.label.color = LabelColor(card.Rarity);
            c.button.onClick.RemoveAllListeners();
            int i = idx;
            c.button.onClick.AddListener(() => Choose(i));

            // 稀有度底色：白/金/彩
            Color bg = RarityColor(card.Rarity);
            var colors = c.button.colors;
            colors.normalColor = bg;
            colors.highlightedColor = Color.Lerp(bg, Color.white, 0.35f);
            colors.pressedColor = Color.Lerp(bg, Color.black, 0.25f);
            c.button.colors = colors;

            c.refreshBtn.interactable = !card.RefreshUsed;
            c.refreshLabel.text = card.RefreshUsed ? "已刷新" : "刷新";
            c.refreshBtn.onClick.RemoveAllListeners();
            c.refreshBtn.onClick.AddListener(() => TryRefresh(i));

            bool canUpgrade = card.Rarity < ForgeRarity.Rainbow &&
                              PlayerStats.Instance != null &&
                              PlayerStats.Instance.Gold >= card.UpgradePrice();
            c.upgradeBtn.interactable = canUpgrade;
            c.upgradeLabel.text = card.Rarity >= ForgeRarity.Rainbow ? "已满" : $"升阶 {card.UpgradePrice()}金";
            c.upgradeBtn.onClick.RemoveAllListeners();
            c.upgradeBtn.onClick.AddListener(() => TryUpgrade(i));
        }

        void Choose(int idx)
        {
            if (forge == null || !forge.IsAwaitingChoice) return;
            forge.Choose(idx);
            panel.SetActive(false);
        }

        void TryRefresh(int idx)
        {
            if (forge != null && forge.TryRefresh(idx)) RefreshCard(idx);
        }

        void TryUpgrade(int idx)
        {
            if (forge != null && forge.TryUpgrade(idx)) RefreshCard(idx);
        }
        #endregion

        #region Static Visual Helpers
        static string CardText(ForgeCard card)
        {
            string desc;
            switch (card.Item.statType)
            {
                case StatType.Damage: desc = $"伤害 +{(int)(card.Item.addValue * card.Multiplier())}"; break;
                case StatType.AttackSpeed: desc = $"攻速 ×{Mathf.Pow(card.Item.addValue, card.Multiplier()):0.##}"; break;
                case StatType.MaxHP: desc = $"生命 +{(int)(card.Item.addValue * card.Multiplier())}"; break;
                case StatType.MoveSpeed: desc = $"移速 +{(card.Item.addValue * card.Multiplier()):0.#}"; break;
                case StatType.Range: desc = $"射程 +{(card.Item.addValue * card.Multiplier()):0.#}"; break;
                case StatType.CritChance: desc = $"暴击 +{(int)(card.Item.addValue * card.Multiplier() * 100f)}%"; break;
                default: desc = card.Item.displayName; break;
            }
            return $"{RarityName(card.Rarity)} · {card.Item.displayName}\n{desc}";
        }

        static string RarityName(ForgeRarity r)
        {
            switch (r)
            {
                case ForgeRarity.Gold: return "金卡";
                case ForgeRarity.Rainbow: return "传说";
                default: return "白卡";
            }
        }

        static Color RarityColor(ForgeRarity r)
        {
            switch (r)
            {
                case ForgeRarity.Gold: return new Color(1f, 0.85f, 0.2f, 0.95f);
                case ForgeRarity.Rainbow: return new Color(0.55f, 0.2f, 0.9f, 0.95f);
                default: return new Color(0.88f, 0.88f, 0.9f, 0.95f);
            }
        }

        static Color LabelColor(ForgeRarity r)
        {
            switch (r)
            {
                case ForgeRarity.Gold: return new Color(0.35f, 0.25f, 0.05f);
                case ForgeRarity.Rainbow: return new Color(0.95f, 0.85f, 1f);
                default: return new Color(0.16f, 0.16f, 0.18f);
            }
        }

        static void AddHoverScale(GameObject go, float scale)
        {
            var et = go.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => go.transform.localScale = Vector3.one * scale);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => go.transform.localScale = Vector3.one);
            et.triggers.Add(enter);
            et.triggers.Add(exit);
        }
        #endregion
    }
}