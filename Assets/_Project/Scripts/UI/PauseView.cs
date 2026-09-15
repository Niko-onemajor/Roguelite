using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>左上角暂停按钮 + 菜单面板：
    /// 可 继续游戏 / 暂停游戏，并查看本局已获取的符文(每回合锻体选择的卡牌效果)。
    /// 暂停用 Time.timeScale=0 冻结全局(UI 按钮仍可点击)。</summary>
    public class PauseView : MonoBehaviour
    {
        GameObject panel;
        Text runeText;
        Button pauseBtn;
        Button resumeBtn;
        Button pauseToggleBtn;
        Text pauseToggleLabel;
        bool _paused;
        bool _inputBeforePause = true;

        public void Build(Transform parent)
        {
            // 左上角暂停按钮
            var btnGo = UIBuilder.Button("Pause", parent, "暂停", OpenPanel);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.005f, 0.94f);
            btnRt.anchorMax = new Vector2(0.065f, 0.985f);
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;
            var btnLabel = btnGo.GetComponentInChildren<Text>(true);
            btnLabel.fontSize = 24;
            pauseBtn = btnGo.GetComponent<Button>();

            // 菜单面板(先隐藏)
            panel = UIBuilder.Panel("PauseMenu", parent, new Color(0f, 0f, 0f, 0.85f));
            panel.SetActive(false);

            var title = UIBuilder.Text("Title", panel.transform, "游戏菜单", 56, Color.white, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0.2f, 0.86f);
            title.rectTransform.anchorMax = new Vector2(0.8f, 0.95f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            var runeTitle = UIBuilder.Text("RuneTitle", panel.transform,
                "符文（每回合选择的卡牌效果）", 32, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleLeft);
            runeTitle.rectTransform.anchorMin = new Vector2(0.1f, 0.74f);
            runeTitle.rectTransform.anchorMax = new Vector2(0.9f, 0.84f);
            runeTitle.rectTransform.offsetMin = Vector2.zero;
            runeTitle.rectTransform.offsetMax = Vector2.zero;

            var runeGo = UIBuilder.Text("Runes", panel.transform, "暂无符文", 28,
                new Color(0.9f, 0.9f, 0.9f), TextAnchor.UpperLeft);
            runeGo.rectTransform.anchorMin = new Vector2(0.1f, 0.34f);
            runeGo.rectTransform.anchorMax = new Vector2(0.9f, 0.74f);
            runeGo.rectTransform.offsetMin = Vector2.zero;
            runeGo.rectTransform.offsetMax = Vector2.zero;
            runeText = runeGo;

            // 按钮行：暂停 / 继续
            var pauseToggle = UIBuilder.Button("TogglePause", panel.transform, "暂停游戏", TogglePause);
            var pRt = pauseToggle.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.2f, 0.16f);
            pRt.anchorMax = new Vector2(0.46f, 0.28f);
            pRt.offsetMin = Vector2.zero;
            pRt.offsetMax = Vector2.zero;
            pauseToggleBtn = pauseToggle.GetComponent<Button>();
            pauseToggleLabel = pauseToggle.GetComponentInChildren<Text>(true);
            pauseToggleLabel.fontSize = 30;

            var resume = UIBuilder.Button("Resume", panel.transform, "继续游戏", Resume);
            var rRt = resume.GetComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0.54f, 0.16f);
            rRt.anchorMax = new Vector2(0.8f, 0.28f);
            rRt.offsetMin = Vector2.zero;
            rRt.offsetMax = Vector2.zero;
            resumeBtn = resume.GetComponent<Button>();
        }

        void OpenPanel()
        {
            if (panel == null) return;
            _inputBeforePause = PlayerController.Instance == null
                || PlayerController.Instance.inputEnabled;
            RefreshRunes();
            panel.SetActive(true);
            pauseBtn.interactable = false;
        }

        void TogglePause()
        {
            _paused = !_paused;
            if (_paused)
            {
                Time.timeScale = 0f;
                if (PlayerController.Instance != null) PlayerController.Instance.inputEnabled = false;
                pauseToggleLabel.text = "已暂停";
                pauseToggleBtn.interactable = false;
            }
            else
            {
                Time.timeScale = 1f;
                if (PlayerController.Instance != null)
                    PlayerController.Instance.inputEnabled = _inputBeforePause;
                pauseToggleLabel.text = "暂停游戏";
                pauseToggleBtn.interactable = true;
            }
        }

        void Resume()
        {
            if (_paused)
            {
                Time.timeScale = 1f;
                _paused = false;
                if (PlayerController.Instance != null)
                    PlayerController.Instance.inputEnabled = _inputBeforePause;
                pauseToggleLabel.text = "暂停游戏";
                pauseToggleBtn.interactable = true;
            }
            panel.SetActive(false);
            pauseBtn.interactable = true;
        }

        void RefreshRunes()
        {
            var stats = PlayerStats.Instance;
            if (runeText == null) return;
            if (stats == null || stats.Runes.Count == 0)
            {
                runeText.text = "暂无符文";
                return;
            }
            var sb = new StringBuilder();
            for (int i = 0; i < stats.Runes.Count; i++)
            {
                var r = stats.Runes[i];
                if (i > 0) sb.AppendLine();
                sb.Append(i + 1).Append(". ").Append(r.Name).Append(" — ").Append(r.Desc);
            }
            runeText.text = sb.ToString();
        }
    }
}