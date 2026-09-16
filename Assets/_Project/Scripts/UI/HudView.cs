using UnityEngine;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>HUD：血条 / 法力条 / 金币 / 自动入库 / 波次 / 底部主动栏(拖拽交换)，订阅 GameEvents 自动刷新。</summary>
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

        // 底部装备栏(数字键 1-8)：显示所有装备，主动装备带冷却读秒；鼠标拖拽槽位交换绑定(目标有货则交换，空槽则移入)
        readonly Text[] activeLabels = new Text[PlayerStats.EquipmentSlotCount];
        readonly GameObject[] activeButtons = new GameObject[PlayerStats.EquipmentSlotCount];

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

            BuildActiveBar(parent);
        }

        /// <summary>底部 1-8 装备栏：8 等宽槽。显示 数字键+装备名(被动装备)；主动装备冷却中显示剩余秒并置灰。
        /// 支持拖拽：按住槽位拖到其他槽松手，目标槽有装备则交换，空槽则移入，拖到栏外不变化。</summary>
        void BuildActiveBar(Transform parent)
        {
            const float width = 0.103f, gap = 0.012f; // 8 槽等宽居中：总宽 8*0.103+7*0.012=0.908
            float left = (1f - (PlayerStats.EquipmentSlotCount * width + (PlayerStats.EquipmentSlotCount - 1) * gap)) * 0.5f;
            for (int i = 0; i < PlayerStats.EquipmentSlotCount; i++)
            {
                int idx = i;
                string digit = (i + 1).ToString();
                var go = UIBuilder.Button($"ActiveSlot_{i}", parent, digit, null);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(left + i * (width + gap), 0.015f);
                rt.anchorMax = new Vector2(left + i * (width + gap) + width, 0.08f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                var label = go.GetComponentInChildren<Text>();
                label.fontSize = 20;
                activeButtons[i] = go;
                activeLabels[i] = label;

                var drag = go.AddComponent<ActiveSlotDrag>();
                drag.SlotIndex = i;
                drag.OnDragStart = OnActiveDragStart;
                drag.HitTest = HitActiveSlot;
                drag.OnDrop = OnActiveSlotDrop;
            }
            RefreshActiveBar();
        }

        void OnActiveDragStart(int idx)
        {
            // 拖动反馈：源槽变半透明灰(松手后重绘恢复)
            if (idx < 0 || idx >= activeButtons.Length || activeButtons[idx] == null) return;
            var img = activeButtons[idx].GetComponent<Image>();
            if (img != null) img.color *= new Color(0.7f, 0.7f, 0.7f, 0.75f);
        }

        /// <summary>拖拽松手：目标槽有装备则交换，空槽则移入；目标为 -1(栏外)或同槽不处理。</summary>
        void OnActiveSlotDrop(int src, int target)
        {
            PlayerStats stats = PlayerStats.Instance;
            if (stats == null || src < 0 || target < 0 || src == target) { RefreshActiveBar(); return; }
            stats.SwapActiveSlots(src, target); // 交换(空槽视为与空位交换，即移入)
        }

        /// <summary>屏幕坐标 → 主动槽位索引；不在任何槽内返回 -1。</summary>
        int HitActiveSlot(Vector2 screenPos)
        {
            for (int i = 0; i < activeButtons.Length; i++)
            {
                if (activeButtons[i] == null) continue;
                var rt = activeButtons[i].GetComponent<RectTransform>();
                if (RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos)) return i;
            }
            return -1;
        }

        /// <summary>装备栏重绘：装备名/空位与冷却剩余。</summary>
        void RefreshActiveBar()
        {
            var stats = PlayerStats.Instance;
            for (int i = 0; i < activeLabels.Length; i++)
            {
                Text label = activeLabels[i];
                if (label == null) continue;
                string digit = (i + 1).ToString();
                ActiveSlot slot = stats != null ? stats.EquipSlots[i] : null;
                bool has = slot != null && slot.Item != null;
                if (has && slot.Remaining > 0f)
                {
                    label.text = $"{digit}\n{slot.Remaining:0.0}s";
                    label.color = new Color(0.7f, 0.7f, 0.7f, 0.9f);
                }
                else
                {
                    label.text = $"{digit}\n" + (has ? slot.Item.displayName : "空");
                    label.color = has ? Color.white : new Color(0.9f, 0.9f, 0.9f, 0.55f);
                }
            }
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
            GameEvents.ActiveSlotsChanged += RefreshActiveBar;
        }

        void OnDisable()
        {
            GameEvents.HPChanged -= OnHPChanged;
            GameEvents.ManaChanged -= OnManaChanged;
            GameEvents.GoldChanged -= OnGoldChanged;
            GameEvents.GoldBanked -= OnGoldBanked;
            GameEvents.WaveChanged -= OnWaveChanged;
            GameEvents.CombatTimeChanged -= OnCombatTimeChanged;
            GameEvents.ActiveSlotsChanged -= RefreshActiveBar;
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
        }

        void OnCombatTimeChanged(float remaining)
        {
            if (timerText == null) return;
            timerText.text = remaining > 0f ? Mathf.CeilToInt(remaining).ToString() : "";
        }
        #endregion
    }
}