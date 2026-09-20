using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>HUD：血条 / 法力条 / 金币 / 自动入库 / 波次 / 底部主动栏(拖拽交换)，订阅 GameEvents 自动刷新。
    /// 底部中央 4×2 装备格：图标占满格子带金色全包边框，点击展开装备效果查看。</summary>
    public class HudView : MonoBehaviour
    {
        Image hpFill;
        Text hpText;
        Image manaFill;
        Text manaText;
        Text goldText;
        Text bankText;
        Text waveText;
        Text timerText;

        // ── 游戏画面装备栏(4×2)：图标占满格子+金色全包边框，点击查看装备效果 ──
        sealed class EquipCellUI
        {
            public GameObject root;
            public Button button;
            public Text label;    // 槽号(左上角)
            public Image icon;
        }
        readonly EquipCellUI[] equipCells = new EquipCellUI[PlayerStats.EquipmentSlotCount];
        GameObject equipPopup;
        Text equipPopupBody;

        public void Build(Transform parent)
        {
            // 血条
            var bar = UIBuilder.Panel("HP_Bar", parent);
            var barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.075f, 0.945f);
            barRt.anchorMax = new Vector2(0.42f, 0.985f);
            barRt.offsetMin = Vector2.zero;
            barRt.offsetMax = Vector2.zero;
            hpFill = UIBuilder.AddFilledBar(bar, new Color(0.8f, 0.25f, 0.25f));
            hpText = UIBuilder.AddText(bar, "HP 100/100", 26, Color.white, TextAnchor.MiddleCenter);

            // 法力条(蓝条)：血条正下方
            var manaBar = UIBuilder.Panel("Mana_Bar", parent);
            var manaRt = manaBar.GetComponent<RectTransform>();
            manaRt.anchorMin = new Vector2(0.075f, 0.902f);
            manaRt.anchorMax = new Vector2(0.42f, 0.944f);
            manaRt.offsetMin = Vector2.zero;
            manaRt.offsetMax = Vector2.zero;
            manaFill = UIBuilder.AddFilledBar(manaBar, new Color(0.25f, 0.5f, 1f));
            manaText = UIBuilder.AddText(manaBar, "MP 0/0", 22, Color.white, TextAnchor.MiddleCenter);

            // 金币与自动入库(整体下移，让位给法力条)
            var goldGo = UIBuilder.Text("Gold", parent, "金币 0", 28, Color.yellow, TextAnchor.MiddleLeft);
            goldGo.rectTransform.anchorMin = new Vector2(0.075f, 0.87f);
            goldGo.rectTransform.anchorMax = new Vector2(0.42f, 0.905f);
            goldText = goldGo;

            // 自动入库金币(回合末未拾取自动结算)：并列放在金币下方
            var bankGo = UIBuilder.Text("Bank", parent, "自动入库 0", 24, new Color(1f, 0.8f, 0.3f, 0.9f), TextAnchor.MiddleLeft);
            bankGo.rectTransform.anchorMin = new Vector2(0.075f, 0.825f);
            bankGo.rectTransform.anchorMax = new Vector2(0.42f, 0.87f);
            bankText = bankGo;

            var waveGo = UIBuilder.Text("Wave", parent, "第 1/5 波", 30, Color.white, TextAnchor.MiddleRight);
            waveGo.rectTransform.anchorMin = new Vector2(0.55f, 0.94f);
            waveGo.rectTransform.anchorMax = new Vector2(0.97f, 0.985f);
            waveText = waveGo;

            // 顶部居中：回合倒计时
            var timerGo = UIBuilder.Text("Timer", parent, "", 56, new Color(1f, 0.9f, 0.4f), TextAnchor.MiddleCenter);
            timerGo.rectTransform.anchorMin = new Vector2(0.42f, 0.92f);
            timerGo.rectTransform.anchorMax = new Vector2(0.58f, 1.0f);
            timerGo.rectTransform.offsetMin = Vector2.zero;
            timerGo.rectTransform.offsetMax = Vector2.zero;
            timerText = timerGo;

            BuildSkillBar(parent);
            BuildEquipmentBar(parent);
            RenderEquipment();
        }

        /// <summary>右下角职业技能栏(E 基础 / R 大招)。正方形技能格：边长 150 设计像素(1920×1080)。</summary>
        void BuildSkillBar(Transform parent)
        {
            const float cellPx = 150f;   // 正方形格边长(px)
            const float gapPx = 26f;     // 两格间距(px)
            float cellW = cellPx / 1920f; // 格宽锚点
            float cellH = cellPx / 1080f; // 格高锚点
            float totalW = cellW * 2f + gapPx / 1920f;
            float x0 = 1f - totalW;
            float y0 = 0.012f;
            var skill = UIBuilder.Panel("SkillBar", parent);
            var rt = skill.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(1f, y0 + cellH);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            if (GetComponent<SkillBarView>() == null)
                gameObject.AddComponent<SkillBarView>().Build(skill.transform);
        }

        /// <summary>底部中央 4×2 装备格：每格 深色玻璃底衬 + 金色全包边框 + 图标占满格子。
        /// 点击有装备的格子展开"装备详情"覆盖层查看效果。</summary>
        void BuildEquipmentBar(Transform parent)
        {
            const float cellW = 0.062f, gapX = 0.008f, cellH = 0.062f, gapY = 0.016f, startX = 0.365f, startY = 0.028f;
            for (int i = 0; i < equipCells.Length; i++)
            {
                int idx = i;
                int col = i % 4, row = i / 4;
                float x0 = startX + col * (cellW + gapX);
                float y0 = startY + row * (cellH + gapY);

                var go = UIBuilder.Button("EquipCell_" + i, parent, "", null);
                SetRect(go.GetComponent<RectTransform>(), x0, y0, x0 + cellW, y0 + cellH);

                var btn = go.GetComponent<Button>();
                btn.onClick.AddListener(() => OnEquipCellClicked(idx));

                // 深色玻璃底衬 + 金色全包边框(与商店出售格同款质感)
                var fc = btn.colors;
                fc.normalColor = new Color(0.1f, 0.1f, 0.14f, 0.95f);
                fc.highlightedColor = new Color(0.22f, 0.22f, 0.3f, 0.95f);
                fc.pressedColor = new Color(0.06f, 0.06f, 0.09f, 0.95f);
                btn.colors = fc;
                if (btn.targetGraphic != null) btn.targetGraphic.color = fc.normalColor;
                var frame = go.AddComponent<Outline>();
                frame.effectColor = new Color(1f, 0.82f, 0.3f, 0.9f);
                frame.effectDistance = new Vector2(2.5f, -2.5f);

                // 图标占满格子
                var iconGo = new GameObject("Icon", typeof(Image));
                iconGo.transform.SetParent(go.transform, false);
                SetRect(iconGo.transform as RectTransform, 0f, 0f, 1f, 1f);
                var iconImg = iconGo.GetComponent<Image>();
                iconImg.raycastTarget = false;

                // 槽号(左上角，置于图标之上)
                var label = go.GetComponentInChildren<Text>(true);
                SetRect(label.rectTransform, 0.02f, 0.82f, 0.98f, 0.98f);
                label.fontSize = 11;
                label.alignment = TextAnchor.UpperLeft;
                label.transform.SetAsLastSibling();

                equipCells[i] = new EquipCellUI { root = go, button = btn, label = label, icon = iconImg };
            }
            BuildEquipPopup(parent);
        }

        /// <summary>装备详情覆盖层：展示 名称/属性/被动/主动效果，关闭后回到战斗画面。</summary>
        void BuildEquipPopup(Transform parent)
        {
            equipPopup = UIBuilder.Panel("EquipDetail", parent, new Color(0f, 0f, 0f, 0.85f));
            equipPopup.SetActive(false);
            SetRect(equipPopup.GetComponent<RectTransform>(), 0.3f, 0.28f, 0.7f, 0.72f);

            var title = UIBuilder.Text("EquipDetailTitle", equipPopup.transform, "装备详情", 40, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, 0.15f, 0.86f, 0.85f, 0.96f);

            equipPopupBody = UIBuilder.Text("EquipDetailBody", equipPopup.transform, "", 24, new Color(0.95f, 0.95f, 0.92f), TextAnchor.UpperLeft);
            SetRect(equipPopupBody.rectTransform, 0.06f, 0.12f, 0.94f, 0.82f);

            var close = UIBuilder.Button("Close", equipPopup.transform, "返回", () => equipPopup.SetActive(false));
            SetRect(close.GetComponent<RectTransform>(), 0.35f, 0.03f, 0.65f, 0.11f);
            close.GetComponentInChildren<Text>(true).fontSize = 24;
        }

        /// <summary>重绘 4×2 装备格：只显示 槽号 与装备图标(效果在点击详情中查看)。波次开始/装备变化后调用。</summary>
        void RenderEquipment()
        {
            PlayerStats stats = PlayerStats.Instance;
            for (int i = 0; i < equipCells.Length; i++)
            {
                EquipCellUI c = equipCells[i];
                if (c == null || c.root == null) continue;
                ShopItemData item = stats != null && i < stats.EquipSlots.Count ? stats.EquipSlots[i].Item : null;
                bool has = item != null;
                c.root.SetActive(true);
                c.button.interactable = has;
                c.label.text = (i + 1).ToString();
                c.label.color = has ? Color.white : new Color(0.9f, 0.9f, 0.9f, 0.5f);
                c.icon.enabled = has && item.IconSprite != null;
                c.icon.sprite = has ? item.IconSprite : null;
            }
        }

        void OnEquipCellClicked(int idx)
        {
            PlayerStats stats = PlayerStats.Instance;
            if (stats == null || idx < 0 || idx >= stats.EquipSlots.Count || equipPopup == null) return;
            ShopItemData item = stats.EquipSlots[idx].Item;
            if (item == null) return;
            equipPopupBody.text = BuildEquipDesc(item);
            equipPopup.SetActive(true);
        }

        /// <summary>装备详情文本：名称/属性加成/被动效果/主动效果。</summary>
        static string BuildEquipDesc(ShopItemData item)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<color=#ffe08a>" + item.displayName + "</color>");
            sb.AppendLine();
            sb.AppendLine(StatText.Describe(item, 1f));
            if (item.activeType != ActiveType.None)
            {
                sb.AppendLine();
                sb.Append("<color=#8fd0ff>主动效果：</color>").Append(ActiveName(item.activeType));
                if (item.activeCooldown > 0f)
                    sb.AppendLine(" (冷却 " + item.activeCooldown.ToString("0.#") + " 秒)");
                else
                    sb.AppendLine();
            }
            return sb.ToString();
        }

        static string ActiveName(ActiveType t) => t switch
        {
            ActiveType.Redemption => "救赎：治疗自身并对半径内敌人造成魔法伤害",
            ActiveType.ManaMeld => "法力具现：消耗法力转化为治疗效果与护盾",
            ActiveType.MoveBurst => "舒瑞娅的狂想曲：短暂提升移动速度",
            ActiveType.AoeBlast => "兰顿之兆：对周围敌人造成魔法伤害",
            ActiveType.Cleanse => "米凯尔的祝福：净化并治疗最大生命30%",
            _ => ""
        };

        static void SetRect(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        #region Unity Lifecycle
        void OnEnable()
        {
            GameEvents.HPChanged += OnHPChanged;
            GameEvents.ManaChanged += OnManaChanged;
            GameEvents.GoldChanged += OnGoldChanged;
            GameEvents.GoldBanked += OnGoldBanked;
            GameEvents.WaveChanged += OnWaveChanged;
            GameEvents.CombatTimeChanged += OnCombatTimeChanged;
            RenderEquipment();
        }

        void OnDisable()
        {
            GameEvents.HPChanged -= OnHPChanged;
            GameEvents.ManaChanged -= OnManaChanged;
            GameEvents.GoldChanged -= OnGoldChanged;
            GameEvents.GoldBanked -= OnGoldBanked;
            GameEvents.WaveChanged -= OnWaveChanged;
            GameEvents.CombatTimeChanged -= OnCombatTimeChanged;
        }
        #endregion

        #region Event Handlers
        void OnHPChanged(float cur, float max)
        {
            if (hpFill == null || hpText == null) return;
            hpFill.fillAmount = max > 0f ? Mathf.Clamp01(cur / max) : 0f;
            hpText.text = $"HP {(int)cur}/{(int)max}";
        }

        void OnManaChanged(float cur, float max)
        {
            if (manaFill == null || manaText == null) return;
            manaFill.fillAmount = max > 0f ? Mathf.Clamp01(cur / max) : 0f;
            manaText.text = $"MP {(int)cur}/{(int)max}";
        }

        void OnGoldChanged(int gold)
        {
            if (goldText != null) goldText.text = "金币 " + gold;
        }

        void OnGoldBanked(int banked)
        {
            if (bankText != null) bankText.text = "自动入库 " + banked;
        }

        void OnWaveChanged(int index, int total)
        {
            if (waveText == null) return;
            waveText.text = total < 0 ? $"第 {index}/∞ 波" : $"第 {index}/{total} 波";
            RenderEquipment(); // 波次开始(商店购买/出售后)同步装备格
        }

        void OnCombatTimeChanged(float remaining)
        {
            if (timerText == null) return;
            timerText.text = remaining > 0f ? Mathf.CeilToInt(remaining).ToString() : "";
        }
        #endregion
    }
}