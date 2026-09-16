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
        }

        /// <summary>右下角职业技能栏(E 基础 / R 大招)。</summary>
        void BuildSkillBar(Transform parent)
        {
            // 技能栏由 SkillBarView 组件负责(含冷却读秒与点击查看描述)
            var skill = UIBuilder.Panel("SkillBar", parent);
            var rt = skill.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.86f, 0.01f);
            rt.anchorMax = new Vector2(0.99f, 0.09f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            if (GetComponent<SkillBarView>() == null)
                gameObject.AddComponent<SkillBarView>().Build(skill.transform);
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
        }

        void OnCombatTimeChanged(float remaining)
        {
            if (timerText == null) return;
            timerText.text = remaining > 0f ? Mathf.CeilToInt(remaining).ToString() : "";
        }
        #endregion
    }
}