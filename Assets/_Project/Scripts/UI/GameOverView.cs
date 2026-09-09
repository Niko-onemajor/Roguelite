using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>结算视图：胜/负 + 击杀/波数 + 重新开始（重载当前场景）。</summary>
    public class GameOverView : MonoBehaviour
    {
        GameObject panel;
        Text titleText;
        Text statsText;

        public void Build(Transform parent)
        {
            panel = UIBuilder.Panel("GameOver", parent, new Color(0f, 0f, 0f, 0.75f));
            panel.SetActive(false);

            var titleGo = UIBuilder.Text("Title", panel.transform, "", 72, Color.white, TextAnchor.MiddleCenter);
            titleGo.rectTransform.anchorMin = new Vector2(0.2f, 0.6f);
            titleGo.rectTransform.anchorMax = new Vector2(0.8f, 0.8f);
            titleGo.rectTransform.offsetMin = Vector2.zero;
            titleGo.rectTransform.offsetMax = Vector2.zero;
            titleText = titleGo;

            var statsGo = UIBuilder.Text("Stats", panel.transform, "", 38, Color.white, TextAnchor.MiddleCenter);
            statsGo.rectTransform.anchorMin = new Vector2(0.2f, 0.42f);
            statsGo.rectTransform.anchorMax = new Vector2(0.8f, 0.56f);
            statsGo.rectTransform.offsetMin = Vector2.zero;
            statsGo.rectTransform.offsetMax = Vector2.zero;
            statsText = statsGo;

            var btn = UIBuilder.Button("Restart", panel.transform, "重新开始", Restart);
            var btnRt = btn.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.4f, 0.2f);
            btnRt.anchorMax = new Vector2(0.6f, 0.32f);
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;
        }

        #region Unity Lifecycle
        void OnEnable() => GameEvents.GameEnded += OnGameEnded;

        void OnDisable() => GameEvents.GameEnded -= OnGameEnded;
        #endregion

        #region Event Handlers
        void OnGameEnded(bool victory, int wavesCleared, int kills)
        {
            if (panel == null) return;
            panel.SetActive(true);
            titleText.text = victory ? "胜利！" : "失败";
            titleText.color = victory ? new Color(1f, 0.85f, 0.3f) : new Color(0.9f, 0.3f, 0.3f);
            statsText.text = $"击杀：{kills}    波数：{wavesCleared}";
        }

        static void Restart()
        {
            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name);
        }
        #endregion
    }
}