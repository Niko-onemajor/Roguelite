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
            public Image glow;    // 顶部能量光条(颜色=稀有度)
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
            AddCardChamfers(); // 卡牌斜切边，摆脱"太方正"观感

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

            // 参考图风格：稀有度不再作为整卡填充底色，统一近黑深色卡面 + 银灰金属细边框，
            // 稀有度(白/金/彩)由顶部能量光条颜色表达。
            var btn = root.GetComponent<Button>();
            var fc = btn.colors;
            fc.normalColor = new Color(0.04f, 0.04f, 0.06f, 0.96f);
            fc.highlightedColor = new Color(0.11f, 0.12f, 0.16f, 0.96f);
            fc.pressedColor = new Color(0.02f, 0.02f, 0.03f, 0.96f);
            btn.colors = fc;
            if (btn.targetGraphic != null) btn.targetGraphic.color = fc.normalColor;

            // 银灰金属描边(全包细边框)
            var frame = root.AddComponent<Outline>();
            frame.effectColor = new Color(0.75f, 0.77f, 0.81f, 0.9f);
            frame.effectDistance = new Vector2(2f, -2f);

            // 顶部能量光条：置顶横条，颜色在 RefreshCard 按稀有度填充
            var glowGo = new GameObject("Glow", typeof(Image));
            glowGo.transform.SetParent(root.transform, false);
            var glowImg = glowGo.GetComponent<Image>();
            glowImg.raycastTarget = false;
            SetRect(glowGo.transform as RectTransform, 0f, 0.965f, 1f, 1f);

            // 图标：居中偏上，保持原比例显示(letterbox 适配，不拉伸)
            var iconGo = new GameObject("Icon", typeof(Image));
            iconGo.transform.SetParent(root.transform, false);
            SetRect(iconGo.transform as RectTransform, 0.28f, 0.58f, 0.72f, 0.88f);
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // 主标题：图标正下方，白色大字
            var label = root.GetComponentInChildren<Text>(true);
            SetRect(label.rectTransform, 0.04f, 0.48f, 0.96f, 0.58f);
            label.fontSize = 30;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;

            // 数值说明：卡片底部，灰蓝小字(次要信息弱化)
            var desc = UIBuilder.Text("ForgeDesc_" + i, root.transform, "", 20, DescColor, TextAnchor.UpperLeft);
            SetRect(desc.rectTransform, 0.1f, 0.1f, 0.9f, 0.46f);

            cards.Add(new CardUI
            {
                root = root,
                button = btn,
                label = label,
                desc = desc,
                icon = iconImg,
                glow = glowImg,
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

            // 参考图层次：主标题白色(稀有度看顶部光条) / 数值描述灰蓝弱化
            c.label.text = FirstStatLine(card.Item, card.Multiplier());
            c.label.color = new Color(0.96f, 0.96f, 1f);
            c.desc.text = StatText.Describe(card.Item, card.Multiplier());
            c.desc.color = DescColor;
            c.icon.enabled = card.Item.IconSprite != null;
            c.icon.sprite = card.Item.IconSprite;
            // 顶部能量光条 = 稀有度颜色(白/金/彩)，卡面统一近黑
            c.glow.color = RarityColor(card.Rarity);
            c.button.onClick.RemoveAllListeners();
            int i = idx;
            c.button.onClick.AddListener(() => Choose(i));
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

        /// <summary>数值描述统一灰蓝：近黑卡面上弱化的次要信息(参考图同款层级)。</summary>
        static readonly Color DescColor = new Color(0.63f, 0.66f, 0.73f);

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

        /// <summary>卡牌四角斜切：以与 Forge 面板底色相近的半透明方块旋转 45° 盖住卡牌尖角，
        /// 呈现"切角卡片"观感，摆脱"太方正"的样式。</summary>
        void AddCardChamfers()
        {
            const float side = 0.06f; // 方块边长(锚点单位)：半对角线≈0.042，决定切角深度
            for (int i = 0; i < 3; i++)
            {
                float x0 = 0.12f + 0.26f * i, x1 = 0.38f + 0.26f * i;
                const float y0 = 0.6f, y1 = 0.96f;
                AddChamferCorner(x0, y1, side); // 左上
                AddChamferCorner(x1, y1, side); // 右上
                AddChamferCorner(x0, y0, side); // 左下
                AddChamferCorner(x1, y0, side); // 右下
            }
        }

        void AddChamferCorner(float cx, float cy, float side)
        {
            var go = new GameObject("Chamfer", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panel.transform, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.5f); // 与 Forge 面板底色(黑/45%)接近，形成切角暗区
            img.raycastTarget = false;               // 不拦截卡片点击
            var rt = go.transform as RectTransform;
            // 方块以角点为圆心，旋转后四角成 45° 切刀，把卡牌尖角"切"掉
            SetRect(rt, cx - side * 0.5f, cy - side * 0.5f, cx + side * 0.5f, cy + side * 0.5f);
            rt.rotation = Quaternion.Euler(0f, 0f, 45f);
        }
        #endregion
    }
}