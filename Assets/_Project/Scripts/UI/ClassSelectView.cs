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
        readonly Text[] labels = new Text[4];       // 职业名称(顶部大标题)
        readonly Text[] descTexts = new Text[4];    // 定位简介(名称下方)
        readonly Text[] bodyTexts = new Text[4];    // 属性+技能(卡片主体)

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

            // 4 列并排卡片：横向一行展示全部职业便于对比
            for (int i = 0; i < cards.Length; i++)
            {
                float x0 = 0.02f + i * 0.247f;
                float x1 = x0 + 0.225f;
                float y0 = 0.12f;
                float y1 = 0.84f;

                var card = UIBuilder.Button("ClassCard_" + i, panel.transform, "", null);
                var rt = card.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(x0, y0);
                rt.anchorMax = new Vector2(x1, y1);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                var label = card.GetComponentInChildren<Text>(true);
                // 职业名称：卡片顶部居中大标题
                var lRt = label.rectTransform;
                lRt.anchorMin = new Vector2(0.03f, 0.84f);
                lRt.anchorMax = new Vector2(0.97f, 0.98f);
                lRt.offsetMin = Vector2.zero;
                lRt.offsetMax = Vector2.zero;
                label.fontSize = 40;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = new Color(1f, 0.87f, 0.45f);

                // 定位简介：名称下方短句
                var descText = UIBuilder.Text("ClassDesc_" + i, card.transform, "", 20, new Color(0.8f, 0.84f, 0.92f), TextAnchor.MiddleCenter);
                SetRect(descText.rectTransform, 0.05f, 0.76f, 0.95f, 0.84f);

                // 属性+技能：卡片主体(名称/简介/主体分区排版，避免文字挤成一团)
                var body = UIBuilder.Text("ClassBody_" + i, card.transform, "", 18, new Color(0.93f, 0.93f, 0.95f), TextAnchor.UpperLeft);
                SetRect(body.rectTransform, 0.05f, 0.05f, 0.95f, 0.75f);

                int idx = i;
                card.GetComponent<Button>().onClick.AddListener(() => Choose(idx));
                cards[i] = card;
                labels[i] = label;
                descTexts[i] = descText;
                bodyTexts[i] = body;
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
                if (!has) continue;
                ClassData c = system.classes[i];
                labels[i].text = c.Name;
                descTexts[i].text = c.Desc;
                bodyTexts[i].text = CardText(c);
            }
        }

        static void SetRect(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static string CardText(ClassData c)
        {
            return $"攻击 {c.damage:0.#} · 法强 {c.abilityPower:0.#}\n" +
                   $"生命 {c.maxHP:0.#}\n" +
                   $"攻速 {c.attackInterval:0.##}秒/发 · 移速 {c.moveSpeed:0.#}\n" +
                   $"护甲/魔抗 {c.armor:0.#}/{c.magicResist:0.#}\n" +
                   $"法力 {c.maxMana:0.#} · 急速 {c.abilityHaste:0.#}\n" +
                   $"武器 {(c.Weapon == WeaponType.Melee ? "近战挥砍" : "远程飞弹")}\n\n" +
                   SkillText(c.QSkill, "E") + "\n" + SkillText(c.RSkill, "R");
        }

        /// <summary>技能行：键位(E基础/R大招)+名称+冷却耗蓝一行，技能描述独立一行(自动换行)。</summary>
        static string SkillText(ClassSkillData s, string key)
        {
            if (s == null) return $"<color=#ff9a7a>[{key}]</color> 无";
            string color = key == "E" ? "#ffd66b" : "#ff8a6b";
            return $"<color={color}>[{key}] {s.Name}</color>　冷却 {s.Cooldown:0.#}s · 耗蓝 {s.ManaCost:0.#}\n{s.Desc}";
        }
        #endregion
    }
}