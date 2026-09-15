using NUnit.Framework;
using UnityEngine;

namespace Roguelite.Tests
{
    public class PickupFactoryTests
    {
        GameObject statsGo;
        PlayerStats stats;

        [SetUp]
        public void SetUp()
        {
            statsGo = new GameObject("stats");
            stats = statsGo.AddComponent<PlayerStats>();
            stats.ResetForRun();
            PlayerStats.Instance = stats; // batchmode 不触发 OnEnable，手动注入
        }

        [TearDown]
        public void TearDown()
        {
            // 清理场上金币与登记表
            PickupFactory.BankAll();
            if (PlayerStats.Instance == stats) PlayerStats.Instance = null;
            Object.DestroyImmediate(statsGo);
            GameEvents.ClearAll();
        }

        [Test]
        public void Spawn_Registers_Active_Pickup()
        {
            // batchmode EditMode 不触发 OnEnable，需显式注册
            Pickup p = PickupFactory.Spawn(Vector3.zero, 5);
            PickupFactory.Register(p);
            Assert.That(PickupFactory.Active, Does.Contain(p));
            Assert.That(p.Gold, Is.EqualTo(5));
        }

        [Test]
        public void BankAll_Sums_Gold_Into_Stats_And_Unregisters()
        {
            int before = stats.Gold;
            Pickup p1 = PickupFactory.Spawn(Vector3.one, 7);
            Pickup p2 = PickupFactory.Spawn(Vector3.one * 2f, 13);
            PickupFactory.Register(p1);
            PickupFactory.Register(p2);

            PickupFactory.BankAll();

            Assert.That(stats.Gold, Is.EqualTo(before + 20));
            Assert.That(PickupFactory.Active.Count, Is.Zero);
        }

        [Test]
        public void BankAll_With_No_Active_Is_Idempotent()
        {
            int before = stats.Gold;
            PickupFactory.BankAll();
            Assert.That(stats.Gold, Is.EqualTo(before)); // 不报错、不误加
        }
    }
}