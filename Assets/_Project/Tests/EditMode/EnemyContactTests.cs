using System.Reflection;
using NUnit.Framework;
using Roguelite;
using UnityEngine;

namespace Roguelite.Tests
{
    /// <summary>敌人撞击玩家：触发伤害后立即消失（不掉金币/不记击杀），远程不触发。</summary>
    public class EnemyContactTests
    {
        static readonly PropertyInfo ControllerInstance =
            typeof(PlayerController).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            GameEvents.ClearAll();
            PoolManager.ClearAll();
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                Object.DestroyImmediate(go);
        }

        static EnemyData Data()
        {
            var d = ScriptableObject.CreateInstance<EnemyData>();
            d.type = EnemyType.Chaser;
            d.maxHP = 100f;
            d.contactDamage = 5f;
            d.goldMin = 1;
            d.goldMax = 1;
            d.scale = 1f;
            return d;
        }

        [Test]
        public void Contact_Damages_Player_And_Vanishes_Without_Gold_Or_Kill()
        {
            var player = new GameObject("Player", typeof(PlayerStats), typeof(CircleCollider2D), typeof(PlayerController));
            var stats = player.GetComponent<PlayerStats>();
            stats.ResetForRun();
            PlayerStats.Instance = stats;
            ControllerInstance.SetValue(null, player.GetComponent<PlayerController>());

            var enemy = EnemyFactory.Spawn(Data(), player.transform.position); // 与玩家重叠
            float hpBefore = stats.CurrentHP;
            int goldBefore = stats.Gold;
            Assert.That(enemy, Is.Not.Null);

            enemy.TryContactDamage();

            Assert.That(stats.CurrentHP, Is.EqualTo(hpBefore - 5f).Within(0.001f)); // 吃到接触伤害
            Assert.That(enemy.gameObject.activeSelf, Is.False);                     // 撞击后立即消失
            Assert.That(stats.Kills, Is.EqualTo(0));                                // 不记击杀
            Assert.That(stats.Gold, Is.EqualTo(goldBefore));                        // 不掉金币(防贴脸刷钱)
        }

        [Test]
        public void No_Contact_When_Far_From_Player()
        {
            var player = new GameObject("Player", typeof(PlayerStats), typeof(CircleCollider2D), typeof(PlayerController));
            var stats = player.GetComponent<PlayerStats>();
            stats.ResetForRun();
            PlayerStats.Instance = stats;
            ControllerInstance.SetValue(null, player.GetComponent<PlayerController>());

            var enemy = EnemyFactory.Spawn(Data(), new Vector3(100f, 0f, 0f));

            enemy.TryContactDamage();

            Assert.That(enemy.gameObject.activeSelf, Is.True); // 未接触：保持存活
            Assert.That(stats.CurrentHP, Is.EqualTo(stats.maxHP).Within(0.001f));
        }
    }
}