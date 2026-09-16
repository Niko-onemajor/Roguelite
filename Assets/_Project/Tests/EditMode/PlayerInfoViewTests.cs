using NUnit.Framework;
using Roguelite;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelite.Tests
{
    /// <summary>玩家详情面板(暂停/商店共用)：打开展示 属性/装备/符文，关闭隐藏。</summary>
    public class PlayerInfoViewTests
    {
        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            GameEvents.ClearAll();
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                Object.DestroyImmediate(go);
        }

        [Test]
        public void Open_Shows_Stats_Weapons_Runes()
        {
            var canvas = UIBuilder.Canvas();
            var player = new GameObject("Player", typeof(PlayerStats), typeof(CombatSystem));
            var stats = player.GetComponent<PlayerStats>();
            stats.ResetForRun();
            PlayerStats.Instance = stats;
            stats.RecordRune("测试符文", "效果描述", Color.white);

            var combat = player.GetComponent<CombatSystem>();
            var wd = ScriptableObject.CreateInstance<WeaponData>();
            wd.displayName = "测试武器";
            wd.type = WeaponType.Ranged;
            combat.Equip(wd);

            var info = canvas.AddComponent<PlayerInfoView>();
            info.combat = combat;
            info.Build(canvas.transform);

            info.Open();

            var panel = canvas.transform.Find("PlayerInfo");
            Assert.That(panel.gameObject.activeSelf, Is.True);
            Assert.That(panel.Find("Body_属性").GetComponent<Text>().text, Does.Contain("攻击力"));
            Assert.That(panel.Find("Body_装备").GetComponent<Text>().text, Does.Contain("测试武器"));
            Assert.That(panel.Find("Body_符文").GetComponent<Text>().text, Does.Contain("测试符文"));
        }

        [Test]
        public void Close_Hides_Panel()
        {
            var canvas = UIBuilder.Canvas();
            var player = new GameObject("Player", typeof(PlayerStats));
            PlayerStats.Instance = player.GetComponent<PlayerStats>();
            var info = canvas.AddComponent<PlayerInfoView>();
            info.Build(canvas.transform);

            info.Open();
            Assert.That(canvas.transform.Find("PlayerInfo").gameObject.activeSelf, Is.True);

            info.Close();

            Assert.That(canvas.transform.Find("PlayerInfo").gameObject.activeSelf, Is.False);
        }
    }
}