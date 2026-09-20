using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>玩家详情覆盖层：暂停页/商店页共用的“查看 属性/符文”面板。
    /// 装备不再单独列出（商店内嵌装备栏可点击查看效果与出售）。
    /// Open 时实时读取 PlayerStats，展示 17 项属性与已获取符文。</summary>
    public class PlayerInfoView : MonoBehaviour
    {
        GameObject panel;
        Text statText;
        Text runeText;

        public void Build(Transform parent)
        {
            panel = UIBuilder.Panel("PlayerInfo", parent, new Color(0f, 0f, 0f, 0.88f));
            panel.SetActive(false);

            var title = UIBuilder.Text("Title", panel.transform, "玩家信息", 56, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, 0.2f, 0.9f, 0.8f, 0.98f);

            BuildSection("属性", 0.03f, 0.70f, 0.48f, new Color(1f, 0.85f, 0.3f), out statText);
            BuildSection("符文", 0.52f, 0.70f, 0.97f, new Color(1f, 0.7f, 0.9f), out runeText);

            var close = UIBuilder.Button("Close", panel.transform, "返回", Close);
            SetRect(close.GetComponent<RectTransform>(), 0.4f, 0.04f, 0.6f, 0.13f);
            close.GetComponentInChildren<Text>(true).fontSize = 26;
        }

        void BuildSection(string header, float x0, float y0, float x1, Color headerColor, out Text body)
        {
            var h = UIBuilder.Text("Header_" + header, panel.transform, header, 30, headerColor, TextAnchor.MiddleLeft);
            SetRect(h.rectTransform, x0, y0 + 0.06f, x1, y0 + 0.12f);
            var b = UIBuilder.Text("Body_" + header, panel.transform, "", 20, new Color(0.92f, 0.92f, 0.92f), TextAnchor.UpperLeft);
            // 正文区：从标题下方一直延伸到屏幕下沿(0.24)，容纳多行文本
            b.rectTransform.anchorMin = new Vector2(x0, 0.24f);
            b.rectTransform.anchorMax = new Vector2(x1, y0 + 0.06f);
            b.rectTransform.offsetMin = Vector2.zero;
            b.rectTransform.offsetMax = Vector2.zero;
            body = b;
        }

        public void Open()
        {
            Refresh();
            if (panel != null) panel.SetActive(true);
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
        }

        void Refresh()
        {
            PlayerStats stats = PlayerStats.Instance;
            if (statText != null) statText.text = stats != null ? BuildStatsText(stats) : "暂无数据";
            if (runeText != null) runeText.text = BuildRuneText(stats);
        }

        static string BuildStatsText(PlayerStats s)
        {
            var sb = new StringBuilder();
            Line(sb, "攻击力", s.TotalDamage.ToString("0.#"));
            Line(sb, "法术强度", s.abilityPower.ToString("0.#"));
            Line(sb, "攻速(发/秒)", (1f / Mathf.Max(0.001f, s.attackInterval)).ToString("0.##"));
            Line(sb, "暴击几率", (s.critChance * 100f).ToString("0.#") + "%");
            Line(sb, "暴击伤害", "×" + s.critMultiplier.ToString("0.#"));
            Line(sb, "护甲穿透", s.armorPen.ToString("0.#"));
            Line(sb, "法术穿透", s.magicPen.ToString("0.#"));
            Line(sb, "全能吸血", (s.omnivamp * 100f).ToString("0.#") + "%");
            Line(sb, "生命值", s.CurrentHP.ToString("0") + " / " + s.maxHP.ToString("0"));
            Line(sb, "生命回复", s.hpRegen.ToString("0.#"));
            Line(sb, "护甲", s.armor.ToString("0.#"));
            Line(sb, "魔抗", s.magicResist.ToString("0.#"));
            Line(sb, "治疗护盾", "+" + (s.healShieldPower * 100f).ToString("0.#") + "%");
            Line(sb, "技能急速", s.abilityHaste.ToString("0.#"));
            Line(sb, "移速", s.moveSpeed.ToString("0.#"));
            Line(sb, "攻击距离", s.range.ToString("0.#"));
            Line(sb, "体型", "×" + s.size.ToString("0.##"));
            Line(sb, "法力", s.maxMana > 0f ? s.mana.ToString("0.#") + " / " + s.maxMana.ToString("0.#") : s.mana.ToString("0.#"));
            Line(sb, "韧性", (s.tenacity * 100f).ToString("0.#") + "%");
            return sb.ToString();
        }

        static void Line(StringBuilder sb, string label, string value)
        {
            sb.Append(label).Append('\t').Append(value).AppendLine();
        }

        static string BuildRuneText(PlayerStats stats)
        {
            if (stats == null || stats.Runes.Count == 0) return "暂无符文";
            var sb = new StringBuilder();
            for (int i = 0; i < stats.Runes.Count; i++)
            {
                var r = stats.Runes[i];
                if (i > 0) sb.AppendLine();
                sb.Append(i + 1).Append(". ").Append(r.Name).AppendLine().Append(r.Desc);
                sb.AppendLine();
            }
            return sb.ToString();
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