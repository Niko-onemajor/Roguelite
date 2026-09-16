using UnityEngine;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>HUD：血条 / 金币 / 自动入库 / 波次，订阅 GameEvents 自动刷新。</summary>
    public class HudView : MonoBehaviour
    {
        Image hpFill;
        Text hpText;
        Text goldText;
        Text bankText;
        Text waveText;
        Text timerText;

        // 底部主动装备栏(数字键 1-0)：显示冷却，点击两槽交换绑定
        readonly Text[] activeLabels = new Text[PlayerStats.ActiveSlotCount];
        readonly GameObject[] activeButtons = new GameObject[PlayerStats.ActiveSlotCount];
        int selectedSlot = -1;

        public void Build(Transform parent)
        {
            var bar = UIBuilder.Panel("HP_Bar", parent);
            var barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.075f, 0.94f);
            barRt.anchorMax = new Vector2(0.42f, 0.985f);
            barRt.offsetMin = Vector2.zero;
            barRt.offsetMax = Vector2.zero;
            hpFill = UIBuilder.AddFilledBar(bar, new Color(0.8f, 0.25f, 0.25f));
            hpText = UIBuilder.AddText(bar, "HP 100/100", 26, Color.white, TextAnchor.MiddleCenter);

            var goldGo = UIBuilder.Text("Gold", parent, "金币 0", 28, Color.yellow, TextAnchor.MiddleLeft);
            goldGo.rectTransform.anchorMin = new Vector2(0.075f, 0.88f);
            goldGo.rectTransform.anchorMax = new Vector2(0.42f, 0.94f);
            goldText = goldGo;

            // 自动入库金币(回合末未拾取自动结算)：并列放在金币下方
            var bankGo = UIBuilder.Text("Bank", parent, "自动入库 0", 24, new Color(1f, 0.8f, 0.3f, 0.9f), TextAnchor.MiddleLeft);
            bankGo.rectTransform.anchorMin = new Vector2(0.075f, 0.825f);
            bankGo.rectTransform.anchorMax = new Vector2(0.42f, 0.88f);
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

        /// <summary>底部 1-0 主动装备栏：10 等宽槽。显示 数字键+装备名；冷却中显示剩余秒并置灰。
        /// 点击选中(金框高亮)，再点另一槽交换二者的键位绑定，点同一槽取消选中。</summary>
        void BuildActiveBar(Transform parent)
        {
            const float width = 0.086f, gap = 0.012f, left = 0.016f;
            for (int i = 0; i < PlayerStats.ActiveSlotCount; i++)
            {
                int idx = i;
                string digit = i < 9 ? (i + 1).ToString() : "0";
                var go = UIBuilder.Button($"ActiveSlot_{i}", parent, digit, () => OnActiveSlotClicked(idx));
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(left + i * (width + gap), 0.015f);
                rt.anchorMax = new Vector2(left + i * (width + gap) + width, 0.08f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                var label = go.GetComponentInChildren<Text>();
                label.fontSize = 20;
                activeButtons[i] = go;
                activeLabels[i] = label;
            }
            RefreshActiveBar();
        }

        void OnActiveSlotClicked(int idx)
        {
            if (PlayerStats.Instance == null) return;
            if (selectedSlot < 0)
            {
                selectedSlot = idx;
                HighlightActiveSlot(idx, true);
            }
            else if (selectedSlot == idx)
            {
                selectedSlot = -1;
                HighlightActiveSlot(idx, false);
            }
            else
            {
                PlayerStats.Instance.SwapActiveSlots(selectedSlot, idx); // 交换后 ActiveSlotsChanged 触发重绘
                int prev = selectedSlot;
                selectedSlot = -1;
                HighlightActiveSlot(prev, false);
            }
        }

        void HighlightActiveSlot(int idx, bool on)
        {
            if (idx < 0 || idx >= activeButtons.Length || activeButtons[idx] == null) return;
            var img = activeButtons[idx].GetComponent<Image>();
            if (img != null) img.color = on ? new Color(0.95f, 0.8f, 0.25f) : new Color(0.3f, 0.5f, 0.9f);
        }

        /// <summary>主动栏重绘：装备名/空位与冷却剩余。选中高亮不随重绘重置。</summary>
        void RefreshActiveBar()
        {
            var stats = PlayerStats.Instance;
            for (int i = 0; i < activeLabels.Length; i++)
            {
                Text label = activeLabels[i];
                if (label == null) continue;
                string digit = i < 9 ? (i + 1).ToString() : "0";
                ActiveSlot slot = stats != null ? stats.ActiveSlots[i] : null;
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
            GameEvents.GoldChanged += OnGoldChanged;
            GameEvents.GoldBanked += OnGoldBanked;
            GameEvents.WaveChanged += OnWaveChanged;
            GameEvents.CombatTimeChanged += OnCombatTimeChanged;
            GameEvents.ActiveSlotsChanged += RefreshActiveBar;
        }

        void OnDisable()
        {
            GameEvents.HPChanged -= OnHPChanged;
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