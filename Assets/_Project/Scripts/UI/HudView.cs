using UnityEngine;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>HUD：血条 / 金币 / 波次，订阅 GameEvents 自动刷新。</summary>
    public class HudView : MonoBehaviour
    {
        Image hpFill;
        Text hpText;
        Text goldText;
        Text waveText;

        public void Build(Transform parent)
        {
            var bar = UIBuilder.Panel("HP_Bar", parent);
            var barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.03f, 0.94f);
            barRt.anchorMax = new Vector2(0.42f, 0.985f);
            barRt.offsetMin = Vector2.zero;
            barRt.offsetMax = Vector2.zero;
            hpFill = UIBuilder.AddFilledBar(bar, new Color(0.8f, 0.25f, 0.25f));
            hpText = UIBuilder.AddText(bar, "HP 100/100", 26, Color.white, TextAnchor.MiddleCenter);

            var goldGo = UIBuilder.Text("Gold", parent, "金币 0", 28, Color.yellow, TextAnchor.MiddleLeft);
            goldGo.rectTransform.anchorMin = new Vector2(0.03f, 0.88f);
            goldGo.rectTransform.anchorMax = new Vector2(0.42f, 0.94f);
            goldText = goldGo;

            var waveGo = UIBuilder.Text("Wave", parent, "第 1/5 波", 30, Color.white, TextAnchor.MiddleRight);
            waveGo.rectTransform.anchorMin = new Vector2(0.55f, 0.94f);
            waveGo.rectTransform.anchorMax = new Vector2(0.97f, 0.985f);
            waveText = waveGo;
        }

        #region Unity Lifecycle
        void OnEnable()
        {
            GameEvents.HPChanged += OnHPChanged;
            GameEvents.GoldChanged += OnGoldChanged;
            GameEvents.WaveChanged += OnWaveChanged;
        }

        void OnDisable()
        {
            GameEvents.HPChanged -= OnHPChanged;
            GameEvents.GoldChanged -= OnGoldChanged;
            GameEvents.WaveChanged -= OnWaveChanged;
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

        void OnWaveChanged(int index, int total)
        {
            if (waveText != null) waveText.text = $"第 {index}/{total} 波";
        }
        #endregion
    }
}