using NUnit.Framework;
using Roguelite;
using UnityEngine;

namespace Roguelite.Tests
{
    public class PlayerStatsTests
    {
        PlayerStats stats;
        GameObject go;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("PlayerStats");
            stats = go.AddComponent<PlayerStats>();
            stats.ResetForRun();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(go);
            if (PlayerStats.Instance == stats) PlayerStats.Instance = null;
        }

        [Test]
        public void TakeDamage_Clamps_At_Zero_And_Raises()
        {
            bool raised = false;
            System.Action<float, float> handler = (c, m) => raised = true;
            GameEvents.HPChanged += handler;
            try
            {
                stats.TakeDamage(9999f);
                Assert.That(stats.CurrentHP, Is.EqualTo(0f));
                Assert.That(raised, Is.True);
            }
            finally
            {
                GameEvents.HPChanged -= handler;
            }
        }

        [Test]
        public void ApplyBonus_MaxHP_Increases_Max_And_Heals_Current()
        {
            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.statType = StatType.MaxHP;
            item.addValue = 20f;
            float before = stats.CurrentHP;
            stats.ApplyBonus(item);
            Assert.That(stats.maxHP, Is.EqualTo(140f)); // 默认 120 + 20
            Assert.That(stats.CurrentHP, Is.EqualTo(before + 20f));
        }

        [Test]
        public void ApplyBonus_AttackSpeed_Multiplies_Interval()
        {
            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.statType = StatType.AttackSpeed;
            item.addValue = 0.85f;
            float before = stats.attackInterval;
            stats.ApplyBonus(item);
            Assert.That(stats.attackInterval, Is.EqualTo(before * 0.85f).Within(0.0001f));
        }

        [Test]
        public void ApplyBonus_With_Multiplier_Scales_Additive_Value()
        {
            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.statType = StatType.Damage;
            item.addValue = 6f;
            float before = stats.damage;
            stats.ApplyBonus(item, 1.5f); // 金卡
            Assert.That(stats.damage, Is.EqualTo(before + 9f).Within(0.0001f));
        }

        [Test]
        public void ApplyBonus_AttackSpeed_With_Multiplier_Scales_Exponentially()
        {
            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.statType = StatType.AttackSpeed;
            item.addValue = 0.85f;
            float before = stats.attackInterval;
            stats.ApplyBonus(item, 2f); // 彩卡
            Assert.That(stats.attackInterval, Is.EqualTo(before * Mathf.Pow(0.85f, 2f)).Within(0.0001f));
        }
    }
}