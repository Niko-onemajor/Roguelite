using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>玩家详情覆盖层：暂停页/商店页共用的“查看 属性/装备/符文”面板。
    /// Open 时实时读取 PlayerStats 与 CombatSystem，展示 17 项属性、已装备武器、已获取符文。</summary>
    public class PlayerInfoView : MonoBehaviour
    {
        /// <summary>由 GameBootstrap 注入：用于读取已装备武器。</summary>
        public CombatSystem combat;

        GameObject panel;
        Text statText;
        Text weaponText;
        Text runeText;

        // 装备栏 8 格(暂停页展示)：点击格子弹出效果描述覆盖层
        readonly Text[] equipLabels = new Text[PlayerStats.EquipmentSlotCount];
        GameObject detailPanel;
        Text detailText;

        public void Build(Transform parent)
        {
            panel = UIBuilder.Panel("PlayerInfo", parent, new Color(0f, 0f, 0f, 0.88f));
            panel.SetActive(false);

            var title = UIBuilder.Text("Title", panel.transform, "玩家信息", 56, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, 0.2f, 0.9f, 0.8f, 0.98f);

            BuildSection("属性", 0.03f, 0.70f, 0.38f, new Color(1f, 0.85f, 0.3f), out statText);
            BuildSection("装备", 0.42f, 0.70f, 0.7f, new Color(0.55f, 0.85f, 1f), out weaponText);
            BuildSection("符文", 0.74f, 0.70f, 0.97f, new Color(1f, 0.7f, 0.9f), out runeText);
            BuildEquipBar();
            BuildDetailPanel();

            var close = UIBuilder.Button("Close", panel.transform, "返回", Close);
            SetRect(close.GetComponent<RectTransform>(), 0.44f, 0.04f, 0.56f, 0.12f);
        }

        /// <summary>装备栏 8 格(标题下方横条)：暂停页查看当前 8 槽装备，点击格子查看效果描述。</summary>
        void BuildEquipBar()
        {
            const float width = 0.112f, gap = 0.006f;
            float left = (1f - (PlayerStats.EquipmentSlotCount * width + (PlayerStats.EquipmentSlotCount - 1) * gap)) * 0.5f;
            for (int i = 0; i < PlayerStats.EquipmentSlotCount; i++)
            {
                int idx = i;
                var go = UIBuilder.Button($"EquipSlot_{i}", panel.transform, "", () => OnEquipClicked(idx));
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(left + i * (width + gap), 0.845f);
                rt.anchorMax = new Vector2(left + i * (width + gap) + width, 0.9f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                var lbl = go.GetComponentInChildren<Text>();
                lbl.fontSize = 15;
                equipLabels[i] = lbl;
            }
        }

        /// <summary>装备效果描述覆盖层(中央框+关闭)：展示 名称/属性加成/被动主动效果/主动冷却/售价。</summary>
        void BuildDetailPanel()
        {
            detailPanel = UIBuilder.Panel("EquipDetail", panel.transform, new Color(0f, 0f, 0f, 0.8f));
            detailPanel.SetActive(false);
            SetRect(detailPanel.GetComponent<RectTransform>(), 0.42f, 0.36f, 0.7f, 0.64f);

            detailText = UIBuilder.Text("Desc", detailPanel.transform, "", 22, new Color(0.95f, 0.95f, 0.92f), TextAnchor.UpperLeft);
            var dt = detailText.rectTransform;
            dt.anchorMin = new Vector2(0.04f, 0.1f);
            dt.anchorMax = new Vector2(0.96f, 0.94f);
            dt.offsetMin = Vector2.zero;
            dt.offsetMax = Vector2.zero;

            var close = UIBuilder.Button("Close", detailPanel.transform, "关闭", () => detailPanel.SetActive(false));
            var cr = close.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0.34f, 0.02f);
            cr.anchorMax = new Vector2(0.66f, 0.08f);
            cr.offsetMin = Vector2.zero;
            cr.offsetMax = Vector2.zero;
        }

        void OnEquipClicked(int idx)
        {
            if (detailPanel == null || detailText == null || idx < 0 || idx >= PlayerStats.EquipmentSlotCount) return;
            PlayerStats stats = PlayerStats.Instance;
            ShopItemData item = stats != null && stats.EquipSlots[idx] != null ? stats.EquipSlots[idx].Item : null;
            detailText.text = item == null
                ? $"<color=#9a9a9a>{idx + 1} 号槽位为空</color>"
                : BuildEquipDesc(item);
            detailPanel.SetActive(true);
        }

        string BuildEquipDesc(ShopItemData item)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<color=#ffe08a>" + item.displayName + "</color>");
            sb.AppendLine("售价 " + item.basePrice + " 金");
            sb.AppendLine();
            sb.AppendLine(StatText.Describe(item, 1f));
            if (item.activeType != ActiveType.None)
            {
                sb.AppendLine();
                sb.AppendLine("<color=#8fd0ff>主动效果(数字键触发)：</color>");
                sb.AppendLine(ActiveName(item.activeType));
                if (item.activeCooldown > 0f)
                    sb.AppendLine("冷却 " + item.activeCooldown.ToString("0.#") + " 秒(受技能急速缩放)");
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
            if (detailPanel != null) detailPanel.SetActive(false);
        }

        void Refresh()
        {
            PlayerStats stats = PlayerStats.Instance;
            if (statText != null) statText.text = stats != null ? BuildStatsText(stats) : "暂无数据";
            if (weaponText != null) weaponText.text = BuildWeaponText();
            if (runeText != null) runeText.text = BuildRuneText(stats);
            RefreshEquipBar(stats);
        }

        /// <summary>装备栏 8 格重绘：显示 槽号+装备名(截断)，空槽显示“空”。</summary>
        void RefreshEquipBar(PlayerStats stats)
        {
            for (int i = 0; i < equipLabels.Length; i++)
            {
                Text lbl = equipLabels[i];
                if (lbl == null) continue;
                ShopItemData item = stats != null && stats.EquipSlots[i] != null ? stats.EquipSlots[i].Item : null;
                string name = item != null ? ShortName(item.displayName) : "空";
                lbl.text = (i + 1) + "\n" + name;
                lbl.color = item != null ? Color.white : new Color(0.9f, 0.9f, 0.9f, 0.55f);
            }
        }

        static string ShortName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "空";
            return name.Length <= 5 ? name : name.Substring(0, 5) + "…";
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

        string BuildWeaponText()
        {
            if (combat == null || combat.Weapons.Count == 0) return "未装备武器";
            var sb = new StringBuilder();
            foreach (var w in combat.Weapons)
            {
                if (w == null || w.Data == null) continue;
                sb.Append(w.Data.displayName).Append(" · ").Append(WTypeName(w.Data.type)).AppendLine();
                sb.Append("伤害 ").Append(w.Data.damage.ToString("0.#"))
                    .Append("  间隔 ").Append(w.Data.attackInterval.ToString("0.##")).Append("秒").AppendLine();
                sb.Append("射程 ").Append(w.Data.range.ToString("0.#"))
                    .Append("  弹速 ").Append(w.Data.speed.ToString("0.#")).AppendLine();
                sb.AppendLine();
            }
            return sb.ToString();
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

        static string WTypeName(WeaponType t) => t switch
        {
            WeaponType.Melee => "近战",
            WeaponType.AoE => "范围",
            _ => "远程"
        };

        static void SetRect(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}