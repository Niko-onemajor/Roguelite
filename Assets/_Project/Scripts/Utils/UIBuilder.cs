using System;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>运行时构建 UGUI（legacy Text，内置字体），避免 TMP/图集资源依赖。</summary>
    public static class UIBuilder
    {
        static Font _font;

        public static Font Font()
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _font;
        }

        public static GameObject Canvas(string name = "UI_Canvas")
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var cs = go.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            return go;
        }

        /// <summary>创建带背景色的面板节点，返回其 GameObject。</summary>
        public static GameObject Panel(string name, Transform parent, Color? color = null)
        {
            var go = Rect(name, parent);
            var img = go.AddComponent<Image>();
            img.color = color ?? new Color(0f, 0f, 0f, 0.45f);
            return go;
        }

        public static Text AddText(GameObject go, string content, int size, Color color, TextAnchor anchor)
        {
            var t = go.AddComponent<Text>();
            t.font = Font();
            t.text = content;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.raycastTarget = false;
            return t;
        }

        public static Text Text(string name, Transform parent, string content, int size, Color color, TextAnchor anchor)
        {
            var go = Rect(name, parent);
            return AddText(go, content, size, color, anchor);
        }

        /// <summary>把 Image 设为水平填充条，用于血条。</summary>
        public static Image AddFilledBar(GameObject go, Color color)
        {
            var img = go.AddComponent<Image>();
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = 0;
            img.fillAmount = 1f;
            img.color = color;
            return img;
        }

        public static GameObject Button(string name, Transform parent, string label, Action onClick)
        {
            var go = Rect(name, parent);
            var img = go.AddComponent<Image>();
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = new Color(0.3f, 0.5f, 0.9f, 1f);
            colors.highlightedColor = new Color(0.45f, 0.65f, 1f, 1f);
            colors.pressedColor = new Color(0.2f, 0.35f, 0.7f, 1f);
            btn.colors = colors;
            AddText(go, label, 30, Color.white, TextAnchor.MiddleCenter);
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return go;
        }

        static GameObject Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}