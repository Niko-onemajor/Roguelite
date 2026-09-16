using UnityEngine;
using UnityEngine.UI;

namespace Roguelite
{
    /// <summary>伤害数字类型：决定颜色与字号。</summary>
    public enum DamageKind
    {
        Physical,   // 物理(普攻/挥砍)：金色
        Magic,      // 魔法(技能)：蓝色
        True,       // 真实伤害：橙色
        PlayerHit,  // 玩家受击：红色
    }

    /// <summary>战斗伤害飘字：世界坐标→Canvas 局部坐标，创建上飘淡出的数字。
    /// Canvas 懒查找并缓存；无 UI 环境(EditMode 测试)静默跳过，不影响逻辑。</summary>
    public static class DamagePopup
    {
        static Canvas _canvas;

        public static void Spawn(Vector3 worldPos, float amount, DamageKind kind)
        {
            Canvas c = ResolveCanvas();
            if (c == null || amount <= 0f) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 sp = cam.WorldToScreenPoint(worldPos);
            if (sp.z < 0f) return; // 相机背后不显示

            RectTransform canvasRt = c.GetComponent<RectTransform>();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, sp, null, out Vector2 local)) return;

            (Color color, int size) = Style(kind);
            var go = new GameObject("DmgText");
            go.transform.SetParent(c.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = local + new Vector2(Random.Range(-14f, 14f), Random.Range(0f, 22f));
            rt.sizeDelta = new Vector2(320f, 90f);
            var t = go.AddComponent<Text>();
            t.font = UIBuilder.Font();
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            var fd = go.AddComponent<FloatingDamage>();
            fd.Init(Mathf.RoundToInt(amount).ToString(), color, size);
        }

        static (Color, int) Style(DamageKind kind) => kind switch
        {
            DamageKind.Magic => (new Color(0.48f, 0.64f, 1f), 34),
            DamageKind.True => (new Color(1f, 0.7f, 0.36f), 36),
            DamageKind.PlayerHit => (new Color(1f, 0.35f, 0.35f), 32),
            _ => (new Color(1f, 0.82f, 0.45f), 34), // 物理: 金色
        };

        static Canvas ResolveCanvas()
        {
            if (_canvas == null)
                _canvas = Object.FindFirstObjectByType<Canvas>();
            return _canvas;
        }
    }

    /// <summary>单个飘字：上飘 + 淡出后自毁。</summary>
    public class FloatingDamage : MonoBehaviour
    {
        Text _label;
        Color _color;
        float _age;
        const float Life = 0.85f;
        const float Rise = 55f; // 上飘速度(逻辑像素/秒)

        public void Init(string content, Color color, int size)
        {
            _label = GetComponent<Text>();
            _label.text = content;
            _label.fontSize = size;
            _label.color = color;
            _color = color;
        }

        void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / Life);
            transform.localPosition += new Vector3(0f, Rise * Time.deltaTime, 0f);
            if (_label != null) _label.color = new Color(_color.r, _color.g, _color.b, 1f - t);
            if (t >= 1f) Destroy(gameObject);
        }
    }
}