using System;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>通关脚本波(默认 20 波)后的选择面板：结算(胜利结束) 或 无尽模式(继续刷波，难度递增)。</summary>
    public class EndlessChoiceView : MonoBehaviour
    {
        GameObject panel;
        bool _subscribed;

        public void Build(Transform parent)
        {
            panel = UIBuilder.Panel("EndlessChoice", parent, new Color(0f, 0f, 0f, 0.7f));
            panel.SetActive(false);

            var title = UIBuilder.Text("Title", panel.transform, "20 波已通关！", 56, Color.white, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0.2f, 0.62f);
            title.rectTransform.anchorMax = new Vector2(0.8f, 0.75f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            var sub = UIBuilder.Text("Sub", panel.transform, "选择结算，或开启无尽模式继续挑战", 30, new Color(0.85f, 0.85f, 0.85f), TextAnchor.MiddleCenter);
            sub.rectTransform.anchorMin = new Vector2(0.2f, 0.5f);
            sub.rectTransform.anchorMax = new Vector2(0.8f, 0.58f);
            sub.rectTransform.offsetMin = Vector2.zero;
            sub.rectTransform.offsetMax = Vector2.zero;

            BuildButton("Settle", "结算", new Vector2(0.22f, 0.2f), new Vector2(0.48f, 0.4f), Settle);
            BuildButton("Endless", "无尽模式", new Vector2(0.52f, 0.2f), new Vector2(0.78f, 0.4f), EnterEndless);

            if (!_subscribed)
            {
                _subscribed = true;
                GameEvents.EndlessChoiceOffered += OnOffered;
            }
        }

        void BuildButton(string name, string text, Vector2 anchorMin, Vector2 anchorMax, Action onClick)
        {
            var btn = UIBuilder.Button(name, panel.transform, text, onClick);
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        void Settle()
        {
            GameEvents.RaiseEndlessChosen(false);
            panel.SetActive(false);
        }

        void EnterEndless()
        {
            GameEvents.RaiseEndlessChosen(true);
            panel.SetActive(false);
        }

        #region Unity Lifecycle
        void OnDisable()
        {
            if (!_subscribed) return;
            _subscribed = false;
            GameEvents.EndlessChoiceOffered -= OnOffered;
        }
        #endregion

        #region Event Handlers
        void OnOffered()
        {
            if (panel == null) return;
            panel.SetActive(true);
        }
        #endregion
    }
}