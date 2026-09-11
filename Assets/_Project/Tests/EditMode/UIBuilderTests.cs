using NUnit.Framework;
using Roguelite;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelite.Tests
{
    /// <summary>回归测试：Graphic 是 [DisallowMultipleComponent]，
    /// 已有 Image 的面板再 AddComponent&lt;Image&gt;() 会返回 null 导致血条空引用。</summary>
    public class UIBuilderTests
    {
        [Test]
        public void AddFilledBar_OnPanelWithExistingImage_ReusesIt()
        {
            var go = UIBuilder.Panel("bar", null);
            var original = go.GetComponent<Image>();
            Assert.That(original, Is.Not.Null);

            var fill = UIBuilder.AddFilledBar(go, Color.red);
            Assert.That(fill, Is.Not.Null);
            Assert.That(fill, Is.SameAs(original), "应复用已有 Image，而不是再 AddComponent");
            Assert.That(fill.type, Is.EqualTo(Image.Type.Filled));
            Assert.That(fill.color == Color.red, Is.True);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void AddFilledBar_OnEmptyObject_AddsImage()
        {
            var go = new GameObject("bare");
            var fill = UIBuilder.AddFilledBar(go, Color.red);
            Assert.That(fill, Is.Not.Null);
            Assert.That(fill.type, Is.EqualTo(Image.Type.Filled));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Build_FullHud_NoThrow()
        {
            var root = new GameObject("root", typeof(RectTransform));
            var hud = root.AddComponent<HudView>();
            Assert.DoesNotThrow(() => hud.Build(root.transform));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Panel_StretchesToFillParent()
        {
            var parent = new GameObject("parent", typeof(RectTransform));
            parent.GetComponent<RectTransform>().sizeDelta = new Vector2(1000f, 800f);

            var panel = UIBuilder.Panel("p", parent.transform);
            var rt = panel.GetComponent<RectTransform>();
            Assert.That(rt.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rt.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rt.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(rt.offsetMax, Is.EqualTo(Vector2.zero));

            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(parent);
        }
    }
}