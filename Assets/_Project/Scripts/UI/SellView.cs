using UnityEngine;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>商店内的出售覆盖层：列出装备栏 8 槽(主动/被动装备)，点击即按原价八折卖出。
    /// 卖出的装备从“已拥有”集合移除，之后可重新在商店刷出(可再入手)。关闭后回到商店。</summary>
    public class SellView : MonoBehaviour
    {
        /// <summary>由 ShopView 注入。</summary>
        public ShopSystem shop;

        GameObject panel;
        readonly GameObject[] rows = new GameObject[PlayerStats.EquipmentSlotCount];
        readonly Text[] rowTexts = new Text[PlayerStats.EquipmentSlotCount];
        readonly Text[] sellLabels = new Text[PlayerStats.EquipmentSlotCount];
        readonly Button[] sellButtons = new Button[PlayerStats.EquipmentSlotCount];

        public void Build(Transform parent)
        {
            panel = UIBuilder.Panel("SellPanel", parent);
            panel.SetActive(false);

            var title = UIBuilder.Text("SellTitle", panel.transform, "出售装备（售价八折）", 44,
                new Color(1f, 0.7f, 0.3f), TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, 0.2f, 0.9f, 0.8f, 0.975f);

            var hint = UIBuilder.Text("SellHint", panel.transform, "点击“出售”卖掉已装备的道具，卖出价 = 原价×0.8",
                20, new Color(0.85f, 0.85f, 0.85f), TextAnchor.MiddleCenter);
            SetRect(hint.rectTransform, 0.1f, 0.85f, 0.9f, 0.9f);

            for (int i = 0; i < rows.Length; i++) BuildRow(i);

            var close = UIBuilder.Button("SellClose", panel.transform, "返回商店", Close);
            SetRect(close.GetComponent<RectTransform>(), 0.4f, 0.04f, 0.6f, 0.1f);
        }

        void BuildRow(int i)
        {
            var go = UIBuilder.Panel($"SellRow_{i}", panel.transform);
            float bottom = 0.145f + 0.083f * (rows.Length - 1 - i);
            SetRect(go.GetComponent<RectTransform>(), 0.07f, bottom, 0.93f, bottom + 0.075f);

            var text = UIBuilder.Text($"SellRowText_{i}", go.transform, "", 20, Color.white, TextAnchor.MiddleLeft);
            SetRect(text.rectTransform, 0.02f, 0f, 0.72f, 1f);

            var btnGo = UIBuilder.Button($"SellBtn_{i}", go.transform, "出售", null);
            SetRect(btnGo.GetComponent<RectTransform>(), 0.74f, 0.12f, 0.98f, 0.88f);
            var btnLabel = btnGo.GetComponentInChildren<Text>(true);
            btnLabel.fontSize = 20;

            int idx = i;
            var btn = btnGo.GetComponent<Button>();
            btn.onClick.AddListener(() => OnSell(idx));

            rows[i] = go;
            rowTexts[i] = text;
            sellLabels[i] = btnLabel;
            sellButtons[i] = btn;
        }

        public void Open()
        {
            if (panel == null) return;
            panel.SetActive(true);
            Render();
        }

        public void Close() { if (panel != null) panel.SetActive(false); }

        void OnSell(int idx)
        {
            if (shop == null) return;
            ShopItemData item = idx >= 0 && shop.stats != null && idx < shop.stats.EquipSlots.Count
                ? shop.stats.EquipSlots[idx]?.Item : null;
            if (item == null) return;
            if (!shop.TrySell(item)) return;
            Render();
        }

        void Render()
        {
            var stats = shop != null ? shop.stats : null;
            for (int i = 0; i < rows.Length; i++)
            {
                ShopItemData item = stats != null && i < stats.EquipSlots.Count
                    ? stats.EquipSlots[i]?.Item : null;
                if (item != null)
                {
                    rowTexts[i].text = $"{i + 1}. {item.displayName}\n{StatText.Describe(item)}";
                    sellLabels[i].text = $"出售 +{ShopSystem.SellPriceOf(item)}金";
                    sellButtons[i].interactable = true;
                }
                else
                {
                    rowTexts[i].text = $"{i + 1}. 空";
                    sellLabels[i].text = "";
                    sellButtons[i].interactable = false;
                }
            }
        }

        static void SetRect(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}