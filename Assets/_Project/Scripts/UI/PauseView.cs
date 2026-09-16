using UnityEngine;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>左上角暂停按钮 + 菜单面板：点击展开即自动暂停(Time.timeScale=0，无需再手动点“暂停”按钮)。
    /// 面板提供 查看属性/装备/符文(PlayerInfoView) / 结算游戏(结束本局) / 继续游戏。</summary>
    public class PauseView : MonoBehaviour
    {
        /// <summary>由 GameBootstrap 注入：玩家详情面板(属性/装备/符文)。</summary>
        public PlayerInfoView info;
        /// <summary>由 GameBootstrap 注入：用于“结算游戏”立即结束本局。</summary>
        public WaveManager wave;

        GameObject panel;
        Button pauseBtn;
        bool _inputBeforePause = true;
        bool _subscribed;

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
            SetRect(title.rectTransform, 0.2f, 0.8f, 0.8f, 0.92f);

            var viewBtn = UIBuilder.Button("ViewInfo", panel.transform, "查看属性 / 装备 / 符文", OpenInfo);
            SetRect(viewBtn.GetComponent<RectTransform>(), 0.2f, 0.5f, 0.8f, 0.62f);

            var settle = UIBuilder.Button("Settle", panel.transform, "结算游戏", Settle);
            SetRect(settle.GetComponent<RectTransform>(), 0.2f, 0.32f, 0.8f, 0.44f);

            var resume = UIBuilder.Button("Resume", panel.transform, "继续游戏", Resume);
            SetRect(resume.GetComponent<RectTransform>(), 0.2f, 0.14f, 0.8f, 0.26f);

            if (!_subscribed)
            {
                _subscribed = true;
                GameEvents.GameEnded += OnGameEnded;
            }
        }

        #region Unity Lifecycle
        void OnDisable()
        {
            if (!_subscribed) return;
            _subscribed = false;
            GameEvents.GameEnded -= OnGameEnded;
        }
        #endregion

        /// <summary>展开菜单即自动暂停：冻结全局时间并锁玩家输入(记录恢复值)。</summary>
        void OpenPanel()
        {
            if (panel == null) return;
            _inputBeforePause = PlayerController.Instance == null
                || PlayerController.Instance.inputEnabled;
            Time.timeScale = 0f;
            if (PlayerController.Instance != null) PlayerController.Instance.inputEnabled = false;
            panel.SetActive(true);
            pauseBtn.interactable = false;
        }

        void OpenInfo()
        {
            if (info != null) info.Open();
        }

        /// <summary>结算游戏：以失败(手动结束)结算当前进度，弹出结算面板。</summary>
        void Settle()
        {
            if (wave == null) return;
            wave.EndRunNow(false);
        }

        void Resume()
        {
            Time.timeScale = 1f;
            if (PlayerController.Instance != null)
                PlayerController.Instance.inputEnabled = _inputBeforePause;
            panel.SetActive(false);
            pauseBtn.interactable = true;
            if (info != null) info.Close();
        }

        /// <summary>游戏结算(无论来源)后恢复时间并收起暂停面板，避免残留冻结。</summary>
        void OnGameEnded(bool victory, int wavesCleared, int kills)
        {
            Time.timeScale = 1f;
            if (panel != null) panel.SetActive(false);
            if (info != null) info.Close();
            if (pauseBtn != null) pauseBtn.interactable = true;
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