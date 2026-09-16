using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>商店视图(Brotato 风格)：同时展示 3 个装备槽，每槽右上角锁定/解锁按钮，
    /// 锁定槽位在刷新时不换货；底部 刷新商店10金币 / 锻体10金币 / 结束商店 按钮，顶部金币显示。
    /// 购买固定价不涨价，商店保持打开直到点击“结束商店”。</summary>
    public class ShopView : MonoBehaviour
    {
        /// <summary>由 GameBootstrap 注入。</summary>
        public ShopSystem shop;
        /// <summary>由 GameBootstrap 注入：商店内的“锻体”按钮触发锻体覆盖层(ForgeView)。</summary>
        public ForgeSystem forge;
        /// <summary>由 GameBootstrap 注入：玩家详情面板(属性/装备/符文)，供查看已获增益后搭配购买。</summary>
        public PlayerInfoView info;

        GameObject panel;
        Text goldText;
        bool _subscribed;
        readonly List<SlotUI> slots = new List<SlotUI>();

        sealed class SlotUI
        {
            public GameObject root;
            public Button button;
            public Text label;
            public Button lockBtn;
            public Text lockLabel;
        }

        public void Build(Transform parent)
        {
            panel = UIBuilder.Panel("Shop", parent);
            panel.SetActive(false);
            if (!_subscribed)
            {
                _subscribed = true;
                GameEvents.ShopOpened += OnShopOpened;
                GameEvents.GoldChanged += OnGoldChanged;
            }

            var title = UIBuilder.Text("ShopTitle", panel.transform, "商店", 64, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, 0.3f, 0.92f, 0.7f, 0.99f);

            goldText = UIBuilder.Text("Gold", panel.transform, "金币: 0", 36, new Color(1f, 0.9f, 0.4f), TextAnchor.MiddleRight);
            SetRect(goldText.rectTransform, 0.72f, 0.92f, 0.98f, 0.99f);

            for (int i = 0; i < ShopSystem.SlotCount; i++) BuildSlot(i);

            var refresh = UIBuilder.Button("RefreshShop", panel.transform, $"刷新商店({ShopSystem.RefreshPrice}金币)", RefreshShop);
            SetRect(refresh.GetComponent<RectTransform>(), 0.06f, 0.13f, 0.26f, 0.21f);
            var forgeBtn = UIBuilder.Button("OpenForge", panel.transform, $"锻体({ForgeSystem.OpenPrice}金币)", OpenForge);
            SetRect(forgeBtn.GetComponent<RectTransform>(), 0.39f, 0.13f, 0.59f, 0.21f);
            var end = UIBuilder.Button("EndShop", panel.transform, "结束商店", EndShop);
            SetRect(end.GetComponent<RectTransform>(), 0.72f, 0.13f, 0.92f, 0.21f);

            var viewBtn = UIBuilder.Button("ViewInfo", panel.transform, "查看属性 / 装备 / 符文", OpenInfo);
            SetRect(viewBtn.GetComponent<RectTransform>(), 0.06f, 0.05f, 0.26f, 0.11f);

            var hint = UIBuilder.Text("ShopHint", panel.transform,
                "购买固定价 · 锁定槽位可保留装备不被刷新替换", 22, new Color(0.85f, 0.85f, 0.85f), TextAnchor.MiddleCenter);
            SetRect(hint.rectTransform, 0.3f, 0.05f, 0.94f, 0.11f);
        }

        void BuildSlot(int i)
        {
            var root = UIBuilder.Button("Slot_" + i, panel.transform, "", null);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.06f + 0.29f * i, 0.28f);
            rt.anchorMax = new Vector2(0.35f + 0.29f * i, 0.86f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var label = root.GetComponentInChildren<Text>(true);
            label.fontSize = 28;

            // 右上角锁定/解锁小按钮(子节点 Button 优先拦截点击，不会触发购买)
            var lockGo = UIBuilder.Button("Lock_" + i, root.transform, "锁定", null);
            SetRect(lockGo.GetComponent<RectTransform>(), 0.55f, 0.74f, 1f, 1f);
            var lockLabel = lockGo.GetComponentInChildren<Text>(true);
            lockLabel.fontSize = 18;

            slots.Add(new SlotUI
            {
                root = root,
                button = root.GetComponent<Button>(),
                label = label,
                lockBtn = lockGo.GetComponent<Button>(),
                lockLabel = lockLabel,
            });
        }

        #region Unity Lifecycle
        void OnDisable()
        {
            if (!_subscribed) return;
            _subscribed = false;
            GameEvents.ShopOpened -= OnShopOpened;
            GameEvents.GoldChanged -= OnGoldChanged;
        }
        #endregion

        #region Event Handlers
        void OnShopOpened(ShopSystem system)
        {
            shop = system;
            if (panel == null) return;
            panel.SetActive(true);
            UpdateGold();
            Render();
        }

        void OnGoldChanged(int gold)
        {
            if (panel == null || goldText == null || !panel.activeSelf) return;
            goldText.text = "金币: " + gold;
        }
        #endregion

        #region Actions
        void RefreshShop()
        {
            if (shop == null || !shop.TryRefresh()) return;
            Render();
        }

        void OpenForge()
        {
            if (forge != null) forge.TryOpenForge(); // ForgeView 覆盖层响应 ForgeOffer
        }

        /// <summary>查看玩家已获 属性/装备/符文，便于搭配购买(详情覆盖层，关闭后回到商店)。</summary>
        void OpenInfo()
        {
            if (info != null) info.Open();
        }

        void EndShop()
        {
            if (shop == null) return;
            shop.End();
            panel.SetActive(false);
        }

        void Buy(int idx)
        {
            if (shop == null || !shop.TryPurchase(idx)) return;
            Render();
        }

        void ToggleLock(int idx)
        {
            if (shop == null) return;
            shop.ToggleLock(idx);
            Render();
        }
        #endregion

        #region Render
        void UpdateGold()
        {
            if (goldText == null) return;
            int gold = shop != null && shop.stats != null ? shop.stats.Gold : 0;
            goldText.text = "金币: " + gold;
        }

        void Render()
        {
            if (shop == null) return;
            for (int i = 0; i < slots.Count; i++)
            {
                bool has = i < shop.Slots.Count;
                slots[i].root.SetActive(has);
                if (has) RenderSlot(i);
            }
        }

        void RenderSlot(int idx)
        {
            SlotUI s = slots[idx];
            ShopSlot slot = shop.Slots[idx];
            bool sold = slot.Item == null;

            s.label.text = sold
                ? "已售出"
                : $"{slot.Item.displayName}\n{StatText.Describe(slot.Item)}\n价格 {shop.PriceOf(slot.Item)} 金币";
            s.button.interactable = !sold;
            s.button.onClick.RemoveAllListeners();
            if (!sold)
            {
                int i = idx;
                s.button.onClick.AddListener(() => Buy(i));
            }

            s.lockBtn.gameObject.SetActive(!sold);
            s.lockBtn.onClick.RemoveAllListeners();
            s.lockBtn.onClick.AddListener(() => ToggleLock(idx));
            s.lockLabel.text = slot.Locked ? "解锁" : "锁定";
            var colors = s.lockBtn.colors;
            colors.normalColor = slot.Locked
                ? new Color(0.9f, 0.6f, 0.15f, 1f)
                : new Color(0.25f, 0.35f, 0.55f, 1f);
            colors.highlightedColor = Color.Lerp(colors.normalColor, Color.white, 0.3f);
            colors.pressedColor = Color.Lerp(colors.normalColor, Color.black, 0.25f);
            s.lockBtn.colors = colors;
        }

        static void SetRect(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        #endregion
    }
}