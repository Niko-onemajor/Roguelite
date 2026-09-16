using UnityEngine;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>技能 HUD(E 基础技能 / R 大招)：显示键位+技能名与冷却读秒(置灰)。
    /// 点击格子展开技能描述覆盖层：名称/定位/描述/伤害构成/实际冷却/耗蓝。冷却由 SkillCooldownChanged 事件驱动刷新。</summary>
    public class SkillBarView : MonoBehaviour
    {
        readonly Text[] labels = new Text[2];       // 键位+技能名
        readonly Text[] cdTexts = new Text[2];      // 冷却大字倒计时(居中覆盖)
        readonly Image[] cdMasks = new Image[2];    // 冷却遮罩:fillAmount=剩余比例, 从上往下盖, 转好=0
        GameObject detailPanel;
        Text detailText;
        int detailSlot = -1; // 当前展开的技能槽(-1=无)
        bool _subscribed;

        public void Build(Transform parent)
        {
            const float width = 0.09f, gap = 0.016f;
            for (int i = 0; i < 2; i++)
            {
                int idx = i;
                string key = i == 0 ? "E" : "R";
                var go = UIBuilder.Button($"Skill_{key}", parent, key, () => ToggleDetail(idx));
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(i * (width + gap), 0f);
                rt.anchorMax = new Vector2(i * (width + gap) + width, 1f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                var lbl = go.GetComponentInChildren<Text>();
                lbl.fontSize = 16;
                lbl.alignment = TextAnchor.MiddleCenter;
                labels[i] = lbl;

                // 冷却大字倒计时(独立 Text 覆盖格内, 冷却时显示剩余秒数)
                var cdText = UIBuilder.Text("CdText_" + key, go.transform, "", 42,
                    new Color(1f, 0.96f, 0.35f), TextAnchor.MiddleCenter);
                var ct = cdText.rectTransform;
                ct.anchorMin = Vector2.zero;
                ct.anchorMax = Vector2.one;
                ct.offsetMin = Vector2.zero;
                ct.offsetMax = Vector2.zero;
                cdText.gameObject.SetActive(false);
                cdTexts[i] = cdText;

                // 冷却遮罩(背景之上、文字之下)：fillAmount 从上往下盖, 冷却比例
                var maskGo = new GameObject("CD", typeof(Image));
                var maskTransform = maskGo.transform as RectTransform;
                maskTransform.SetParent(go.transform, false);
                maskTransform.anchorMin = Vector2.zero;
                maskTransform.anchorMax = Vector2.one;
                maskTransform.offsetMin = Vector2.zero;
                maskTransform.offsetMax = Vector2.zero;
                Image mask = maskGo.GetComponent<Image>();
                mask.color = new Color(0f, 0f, 0f, 0.55f);
                mask.type = Image.Type.Filled;
                mask.fillMethod = Image.FillMethod.Vertical;
                mask.fillOrigin = (int)Image.OriginVertical.Top; // 剩余越多覆盖越满
                maskGo.transform.SetAsFirstSibling(); // 位于技能名之下, 保持可读
                cdMasks[i] = mask;
            }

            // 描述覆盖层：以 Canvas 为父(全屏)，中央弹出，点击格子切换开合
            Transform root = parent.parent ?? parent;
            detailPanel = UIBuilder.Panel("SkillDetail", root, new Color(0f, 0f, 0f, 0.82f));
            var dp = detailPanel.GetComponent<RectTransform>();
            dp.anchorMin = new Vector2(0.29f, 0.36f);
            dp.anchorMax = new Vector2(0.71f, 0.62f);
            dp.offsetMin = Vector2.zero;
            dp.offsetMax = Vector2.zero;
            detailPanel.SetActive(false);

            var title = UIBuilder.Text("Title", detailPanel.transform, "技能详情", 34, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
            var tr = title.rectTransform;
            tr.anchorMin = new Vector2(0f, 0.84f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;

            detailText = UIBuilder.Text("Desc", detailPanel.transform, "", 22, new Color(0.95f, 0.95f, 0.92f), TextAnchor.UpperLeft);
            var dt = detailText.rectTransform;
            dt.anchorMin = new Vector2(0.03f, 0.08f);
            dt.anchorMax = new Vector2(0.97f, 0.8f);
            dt.offsetMin = Vector2.zero;
            dt.offsetMax = Vector2.zero;

            var close = UIBuilder.Button("Close", detailPanel.transform, "关闭", () => detailPanel.SetActive(false));
            var cr = close.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0.42f, 0.02f);
            cr.anchorMax = new Vector2(0.58f, 0.08f);
            cr.offsetMin = Vector2.zero;
            cr.offsetMax = Vector2.zero;

            Refresh();
            Subscribe();
        }

        void ToggleDetail(int idx)
        {
            if (detailPanel == null) return;
            if (detailPanel.activeSelf && detailSlot == idx)
            {
                detailPanel.SetActive(false);
                return;
            }
            detailSlot = idx;
            RenderDetail(idx);
            detailPanel.SetActive(true);
        }

        void RenderDetail(int idx)
        {
            if (detailText == null) return;
            detailText.text = BuildDesc(idx);
        }

        static string BuildDesc(int idx)
        {
            PlayerStats s = PlayerStats.Instance;
            if (s == null) return "暂无数据";
            ClassSkillData sk = idx == 0 ? s.QSkill : s.RSkill;
            string key = idx == 0 ? "E" : "R";
            string type = idx == 0 ? "基础技能" : "终极技能";
            if (sk == null) return $"{key} 技能未解锁";
            if (string.IsNullOrEmpty(sk.Name)) return $"{key} 技能未解锁";

            string eff = "";
            if (sk.BaseDamage > 0f) eff += $"基础 {sk.BaseDamage:0.#}";
            if (sk.AdRatio > 0f) eff += string.IsNullOrEmpty(eff) ? $"攻击力×{sk.AdRatio:0.#}" : " + 攻击力×" + sk.AdRatio.ToString("0.#");
            if (sk.ApRatio > 0f) eff += string.IsNullOrEmpty(eff) ? $"法强×{sk.ApRatio:0.#}" : " + 法强×" + sk.ApRatio.ToString("0.#");
            if (!string.IsNullOrEmpty(eff)) eff = "伤害构成：" + eff;
            float cd = sk.Cooldown > 0f ? sk.Cooldown * s.HasteCooldownScale : 0f;
            return $"{sk.Name} · {type}\n<color=#cfd6e4>{sk.Desc}</color>\n\n{eff}\n冷却 {cd:0.#} 秒 (受技能急速缩放)\n耗蓝 {sk.ManaCost:0.#}";
        }

        #region Unity Lifecycle
        void OnEnable()
        {
            if (_subscribed) return;
            _subscribed = true;
            GameEvents.SkillCooldownChanged += Refresh;
        }

        void OnDisable()
        {
            if (!_subscribed) return;
            _subscribed = false;
            GameEvents.SkillCooldownChanged -= Refresh;
        }
        #endregion

        void Subscribe() => OnEnable();

        /// <summary>重绘 E/R 格子：冷却中技能名置灰+渐变遮罩, 格中央显示大字倒计时(整秒); 就绪恢复并隐藏倒计时。</summary>
        void Refresh()
        {
            PlayerStats s = PlayerStats.Instance;
            for (int i = 0; i < labels.Length; i++)
            {
                Text lbl = labels[i];
                if (lbl == null) continue;
                string key = i == 0 ? "E" : "R";
                ClassSkillData sk = i == 0 ? (s != null ? s.QSkill : null) : (s != null ? s.RSkill : null);
                float remaining = i == 0 ? (s != null ? s.QCooldown : 0f) : (s != null ? s.RCooldown : 0f);
                string name = sk != null && !string.IsNullOrEmpty(sk.Name) ? sk.Name : "未解锁";
                bool cooling = remaining > 0.01f && sk != null;
                if (cooling)
                {
                    int total = Mathf.Max(1, Mathf.RoundToInt(sk.Cooldown * s.HasteCooldownScale));
                    if (cdMasks[i] != null) cdMasks[i].fillAmount = Mathf.Clamp01(remaining / total);
                    lbl.text = key + "\n" + name;
                    lbl.color = new Color(0.75f, 0.75f, 0.75f, 0.9f);
                    if (cdTexts[i] != null)
                    {
                        cdTexts[i].text = Mathf.CeilToInt(remaining).ToString();
                        cdTexts[i].gameObject.SetActive(true);
                    }
                }
                else
                {
                    if (cdMasks[i] != null) cdMasks[i].fillAmount = 0f;
                    lbl.text = key + "\n" + name;
                    lbl.color = Color.white;
                    if (cdTexts[i] != null) cdTexts[i].gameObject.SetActive(false);
                }
            }
        }
    }
}