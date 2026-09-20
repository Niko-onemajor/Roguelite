using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>符文选择视图(海克斯大乱斗风格)：3 张符文卡三选一，每张带 1 次刷新按钮，
    /// 稀有度底色(白/金/紫)区分视觉强度；点击卡片生效。</summary>
    public class RuneView : MonoBehaviour
    {
        /// <summary>由 GameBootstrap 注入。</summary>
        public RuneSystem rune;

        GameObject panel;
        bool _subscribed;
        readonly List<CardUI> cards = new List<CardUI>();

        sealed class CardUI
        {
            public GameObject root;
            public Button button;
            public Image icon;    // 图标(置顶居中)
            public Text label;    // 名称(图标下方)
            public Text desc;     // 属性/被动描述(独立区块)
            public Button refreshBtn;
            public Text refreshLabel;
        }

        public void Build(Transform parent)
        {
            panel = UIBuilder.Panel("Rune", parent);
            panel.SetActive(false);
            if (!_subscribed)
            {
                _subscribed = true;
                GameEvents.RuneOffer += OnRuneOffered;
            }
            for (int i = 0; i < RuneSystem.CardCount; i++) BuildCard(i);

            var title = UIBuilder.Text("RuneTitle", panel.transform, "选择符文", 64, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0.2f, 0.88f);
            title.rectTransform.anchorMax = new Vector2(0.8f, 0.97f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            var skip = UIBuilder.Button("SkipRune", panel.transform, "跳过", Skip);
            var rt = skip.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.4f, 0.1f);
            rt.anchorMax = new Vector2(0.6f, 0.18f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        void Skip()
        {
            if (rune == null) return;
            rune.Skip();
            panel.SetActive(false);
        }

        void BuildCard(int i)
        {
            var root = UIBuilder.Button("RuneCard_" + i, panel.transform, "", null);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f + 0.27f * i, 0.3f);
            rt.anchorMax = new Vector2(0.37f + 0.27f * i, 0.85f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // 图标：置顶居中(视觉焦点)
            var iconGo = new GameObject("Icon", typeof(Image));
            iconGo.transform.SetParent(root.transform, false);
            var iRt = iconGo.transform as RectTransform;
            iRt.anchorMin = new Vector2(0.3f, 0.58f);
            iRt.anchorMax = new Vector2(0.7f, 0.88f);
            iRt.offsetMin = Vector2.zero;
            iRt.offsetMax = Vector2.zero;
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // 名称：图标下方大标题
            var label = root.GetComponentInChildren<Text>(true);
            var lRt = label.rectTransform;
            lRt.anchorMin = new Vector2(0.04f, 0.46f);
            lRt.anchorMax = new Vector2(0.96f, 0.58f);
            lRt.offsetMin = Vector2.zero;
            lRt.offsetMax = Vector2.zero;
            label.fontSize = 32;
            label.alignment = TextAnchor.MiddleCenter;

            // 描述：卡片底部独立文本区(raycastTarget=false 不拦截卡片点击)
            var desc = UIBuilder.Text("RuneDesc_" + i, root.transform, "", 20, Color.white, TextAnchor.UpperLeft);
            var dRt = desc.rectTransform;
            dRt.anchorMin = new Vector2(0.08f, 0.08f);
            dRt.anchorMax = new Vector2(0.92f, 0.42f);
            dRt.offsetMin = Vector2.zero;
            dRt.offsetMax = Vector2.zero;

            // 每张卡的 1 次刷新按钮(底部横条；父级 Button 的同级子节点不会抢占点击)
            var refreshGo = UIBuilder.Button("Refresh_" + i, root.transform, "刷新", null);
            var rRt = refreshGo.GetComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0f, 0f);   // 卡片内右下角
            rRt.anchorMax = new Vector2(1f, 0f);
            rRt.pivot = new Vector2(0.5f, 0f);
            rRt.anchoredPosition = new Vector2(0f, 12f);
            rRt.sizeDelta = new Vector2(-40f, 44f);
            var rLabel = refreshGo.GetComponentInChildren<Text>(true);
            rLabel.fontSize = 22;

            cards.Add(new CardUI
            {
                root = root,
                button = root.GetComponent<Button>(),
                icon = iconImg,
                label = label,
                desc = desc,
                refreshBtn = refreshGo.GetComponent<Button>(),
                refreshLabel = rLabel,
            });
        }

        #region Unity Lifecycle
        void OnDisable()
        {
            if (!_subscribed) return;
            _subscribed = false;
            GameEvents.RuneOffer -= OnRuneOffered;
        }
        #endregion

        #region Event Handlers
        void OnRuneOffered(RuneSystem system)
        {
            rune = system;
            if (panel == null) return;
            panel.SetActive(true);
            for (int i = 0; i < cards.Count; i++)
            {
                bool has = i < system.Cards.Count;
                cards[i].root.SetActive(has);
                if (!has) continue;
                RefreshCard(i);
            }
        }

        void RefreshCard(int idx)
        {
            CardUI c = cards[idx];
            RuneCard card = rune.Cards[idx];

            c.label.text = card.Item.displayName;
            c.label.color = RarityLabelColor(card.Rarity);
            c.icon.enabled = card.Item.IconSprite != null;
            c.icon.sprite = card.Item.IconSprite;
            c.desc.text = StatText.Describe(card.Item);
            c.desc.color = RarityLabelColor(card.Rarity);

            Color bg = RarityBg(card.Rarity);
            var colors = c.button.colors;
            colors.normalColor = bg;
            colors.highlightedColor = Color.Lerp(bg, Color.white, 0.35f);
            colors.pressedColor = Color.Lerp(bg, Color.black, 0.25f);
            c.button.colors = colors;

            // 卡片主点击 = 选择
            c.button.onClick.RemoveAllListeners();
            int i = idx;
            c.button.onClick.AddListener(() => Choose(i));

            // 刷新按钮：每张限 1 次
            c.refreshBtn.onClick.RemoveAllListeners();
            c.refreshBtn.onClick.AddListener(() => DoRefresh(i));
            if (card.RefreshUsed)
            {
                c.refreshLabel.text = "已刷新";
                c.refreshBtn.interactable = false;
            }
            else
            {
                c.refreshLabel.text = "刷新";
                c.refreshBtn.interactable = true;
            }
        }

        void DoRefresh(int idx)
        {
            if (rune == null || !rune.TryRefresh(idx)) return;
            RefreshCard(idx);
        }

        void Choose(int idx)
        {
            if (rune == null || !rune.IsAwaitingChoice) return;
            rune.Choose(idx);
            panel.SetActive(false);
        }
        #endregion

        #region Static Visual Helpers
        static Color RarityBg(RuneRarity r)
        {
            switch (r)
            {
                case RuneRarity.Gold: return new Color(1f, 0.85f, 0.2f, 0.95f);
                case RuneRarity.Purple: return new Color(0.55f, 0.2f, 0.9f, 0.95f);
                default: return new Color(0.88f, 0.88f, 0.92f, 0.95f);
            }
        }

        static Color RarityLabelColor(RuneRarity r)
        {
            switch (r)
            {
                case RuneRarity.Gold: return new Color(0.35f, 0.25f, 0.05f);
                case RuneRarity.Purple: return new Color(0.96f, 0.88f, 1f);
                default: return new Color(0.16f, 0.16f, 0.18f);
            }
        }
        #endregion
    }
}