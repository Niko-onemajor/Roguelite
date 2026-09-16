using NUnit.Framework;
using Roguelite;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelite.Tests
{
    /// <summary>暂停面板：展开即自动暂停、继续恢复、结算游戏结束本局。</summary>
    public class PauseViewTests
    {
        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            GameEvents.ClearAll();
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                Object.DestroyImmediate(go);
        }

        static Button PauseBtn(Transform canvas) =>
            canvas.Find("Pause").GetComponent<Button>();

        static Button FindBtn(Transform canvas, string path) =>
            canvas.Find(path).GetComponent<Button>();

        [Test]
        public void OpenPanel_AutoPauses_And_Shows_Menu()
        {
            var canvas = UIBuilder.Canvas();
            var pv = canvas.AddComponent<PauseView>();
            pv.Build(canvas.transform);

            PauseBtn(canvas.transform).onClick.Invoke();

            Assert.That(Time.timeScale, Is.EqualTo(0f)); // 展开即自动暂停
            Assert.That(canvas.transform.Find("PauseMenu").gameObject.activeSelf, Is.True);
        }

        [Test]
        public void Resume_Restores_Time_And_Hides_Menu()
        {
            var canvas = UIBuilder.Canvas();
            var pv = canvas.AddComponent<PauseView>();
            pv.Build(canvas.transform);
            PauseBtn(canvas.transform).onClick.Invoke();
            Time.timeScale = 0f; // 确保以暂停态进入

            FindBtn(canvas.transform, "PauseMenu/Resume").onClick.Invoke();

            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(canvas.transform.Find("PauseMenu").gameObject.activeSelf, Is.False);
        }

        [Test]
        public void SettleButton_Ends_Game_With_Defeat()
        {
            var canvas = UIBuilder.Canvas();
            var stats = new GameObject("stats").AddComponent<PlayerStats>();
            stats.ResetForRun();
            PlayerStats.Instance = stats;

            var wave = canvas.AddComponent<WaveManager>();
            typeof(WaveManager).GetField("stats", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(wave, stats); // 免走 BeginRun 协程，直接注入统计数据

            var pv = canvas.AddComponent<PauseView>();
            pv.wave = wave;
            pv.Build(canvas.transform);
            PauseBtn(canvas.transform).onClick.Invoke(); // 打开(已暂停)

            bool ended = false;
            GameEvents.GameEnded += (v, wc, k) => ended = true;

            FindBtn(canvas.transform, "PauseMenu/Settle").onClick.Invoke();

            Assert.That(ended, Is.True);
            Assert.That(wave.State, Is.EqualTo(WaveState.GameOver));
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 结算后恢复时间
            Assert.That(canvas.transform.Find("PauseMenu").gameObject.activeSelf, Is.False); // 暂停面板自动收起
        }
    }
}