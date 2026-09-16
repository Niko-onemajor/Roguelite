using UnityEngine;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>开局职业选择视图：4 张职业卡(2×2)，展示名称/定位/基础属性/Q基础技能/R大招，
    /// 点击即选职业并关闭(选完由 GameBootstrap 应用职业 → 开始第 1 波)。</summary>
    public class ClassSelectView : MonoBehaviour
    {
        /// <summary>由 GameBootstrap 注入。</summary>
        public ClassSelectSystem selector;

        GameObject panel;
        bool _subscribed;
        readonly GameObject[] cards = new GameObject[4];
        readonly Text[] labels = new Text[4];

        public void Build(Transform parent)
        {
            panel = UIBuilder.Panel("ClassSelect", parent);
            panel.SetActive(false);
            if (!_subscribed)
            {
                _subscribed = true;
                GameEvents.ClassOffer += OnClassOffered;
            }

            var title = UIBuilder.Text("ClassTitle", panel.transform, "选择职业", 72, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0.2f, 0.9f);
            title.rectTransform.anchorMax = new Vector2(0.8f, 0.99f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            var hint = UIBuilder.Text("ClassHint", panel.transform, "决定基础属性与 E(基础技能)/R(大招)，点击选择", 26,
                new Color(0.85f, 0.85f, 0.85f), TextAnchor.MiddleCenter);
            hint.rectTransform.anchorMin = new Vector2(0.2f, 0.85f);
            hint.rectTransform.anchorMax = new Vector2(0.8f, 0.9f);
            hint.rectTransform.offsetMin = Vector2.zero;
            hint.rectTransform.offsetMax = Vector2.zero;

            // 2×2 卡片：左上/右上/左下/右下
            for (int i = 0; i < cards.Length; i++)
            {
                float x0 = 0.07f + (i % 2) * 0.45f;
                float x1 = x0 + 0.41f;
                float y0 = i < 2 ? 0.44f : 0.14f;
                float y1 = i < 2 ? 0.84f : 0.42f;

                var card = UIBuilder.Button("ClassCard_" + i, panel.transform, "", null);
                var rt = card.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(x0, y0);
                rt.anchorMax = new Vector2(x1, y1);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                var label = card.GetComponentInChildren<Text>(true);
                label.fontSize = 24;
                label.alignment = TextAnchor.UpperLeft;

                int idx = i;
                card.GetComponent<Button>().onClick.AddListener(() => Choose(idx));
                cards[i] = card;
                labels[i] = label;
            }
        }

        void Choose(int idx)
        {
            if (selector == null || !selector.IsAwaitingChoice) return;
            selector.Choose(idx);
            if (panel != null) panel.SetActive(false);
        }

        #region Unity Lifecycle
        void OnDisable()
        {
            if (!_subscribed) return;
            _subscribed = false;
            GameEvents.ClassOffer -= OnClassOffered;
        }
        #endregion

        #region Event Handlers
        void OnClassOffered(ClassSelectSystem system)
        {
            selector = system;
            if (panel == null) return;
            panel.SetActive(true);
            if (system.classes == null) return;
            for (int i = 0; i < cards.Length; i++)
            {
                bool has = i < system.classes.Length;
                cards[i].SetActive(has);
                if (has) labels[i].text = CardText(system.classes[i]);
            }
        }

        static string CardText(ClassData c)
        {
            return $"{c.Name}　【{c.Desc}】\n" +
                   $"攻击 {c.damage:0.#} · 法强 {c.abilityPower:0.#} · 生命 {c.maxHP:0.#}\n" +
                   $"攻速 {c.attackInterval:0.##}秒/发 · 移速 {c.moveSpeed:0.#} · 护甲/魔抗 {c.armor:0.#}/{c.magicResist:0.#}\n" +
                   $"法力 {c.maxMana:0.#} · 技能急速 {c.abilityHaste:0.#} · 武器 {(c.Weapon == WeaponType.Melee ? "近战挥砍" : "远程飞弹")}\n\n" +
                   $"[Q] {SkillLine(c.QSkill)}\n[R] {SkillLine(c.RSkill)}";
        }

        static string SkillLine(ClassSkillData s)
        {
            if (s == null) return "无";
            return $"{s.Name}　{s.Desc}　冷却 {s.Cooldown:0.#}s 耗蓝 {s.ManaCost:0.#}";
        }
        #endregion
    }
}