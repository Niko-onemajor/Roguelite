using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>锻体 UI：花费 10 金币开启的海克斯风格 3 选 1 卡牌，稀有度底色、悬停放大。
    /// 无刷新/升阶：选择即生效，可跳过。</summary>
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
            public Text label;    // 名称(图标下)与稀有度
            public Text desc;     // 效果描述
            public Image icon;    // 图标(置顶居中)
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
                "锻体：花费 10 金币开启 · 选择 1 张卡强化本回合 · 点击卡牌生效 · 可跳过",
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
            rt.anchorMin = new Vector2(0.12f + 0.26f * i, 0.6f);
            rt.anchorMax = new Vector2(0.38f + 0.26f * i, 0.96f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            AddHoverScale(root, 1.08f);

            var label = root.GetComponentInChildren<Text>(true);
            // 名称(含稀有度)：图标下方居中
            SetRect(label.rectTransform, 0.04f, 0.62f, 0.96f, 0.72f);
            label.fontSize = 28;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;

            // 图标：置顶居中(视觉焦点)
            var iconGo = new GameObject("Icon", typeof(Image));
            iconGo.transform.SetParent(root.transform, false);
            SetRect(iconGo.transform as RectTransform, 0.27f, 0.74f, 0.73f, 0.98f);
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // 效果描述：卡片主体(raycastTarget=false 不拦截选择点击)
            var desc = UIBuilder.Text("ForgeDesc_" + i, root.transform, "", 20, Color.white, TextAnchor.UpperLeft);
            SetRect(desc.rectTransform, 0.1f, 0.12f, 0.9f, 0.6f);

            cards.Add(new CardUI
            {
                root = root,
                button = root.GetComponent<Button>(),
                label = label,
                desc = desc,
                icon = iconImg,
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

            // 稀有度不再写进标题/颜色(看卡牌底色即可)：标题直接显示放大后的数值行，与描述首行一致。
            c.label.text = FirstStatLine(card.Item, card.Multiplier());
            c.label.color = ContentColor;
            c.desc.text = StatText.Describe(card.Item, card.Multiplier());
            c.desc.color = ContentColor;
            c.icon.enabled = card.Item.IconSprite != null;
            c.icon.sprite = card.Item.IconSprite;
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
            if (c.button.targetGraphic != null) c.button.targetGraphic.color = bg;
        }

        void Choose(int idx)
        {
            if (forge == null || !forge.IsAwaitingChoice) return;
            forge.Choose(idx);
            panel.SetActive(false);
        }
        #endregion

        #region Static Visual Helpers
        static void SetRect(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>标题：属性描述放大后的首行(如 "攻击力 +9")，与描述正文首行数值一致。</summary>
        static string FirstStatLine(ShopItemData item, float mult)
        {
            string full = StatText.Describe(item, mult);
            if (string.IsNullOrEmpty(full)) return item != null ? item.displayName : "";
            int nl = full.IndexOf('\n');
            return nl > 0 ? full.Substring(0, nl) : full;
        }

        /// <summary>卡面文字统一深色：三种稀有度底色(浅灰/亮黄/中紫)上均清晰可读。</summary>
        static readonly Color ContentColor = new Color(0.12f, 0.12f, 0.14f);

        static Color RarityColor(ForgeRarity r)
        {
            switch (r)
            {
                case ForgeRarity.Gold: return new Color(1f, 0.85f, 0.2f, 0.95f);
                case ForgeRarity.Rainbow: return new Color(0.55f, 0.2f, 0.9f, 0.95f);
                default: return new Color(0.88f, 0.88f, 0.9f, 0.95f);
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