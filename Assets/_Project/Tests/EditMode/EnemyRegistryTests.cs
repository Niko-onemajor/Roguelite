using NUnit.Framework;
using Roguelite;
using UnityEngine;

namespace Roguelite.Tests
{
    public class EnemyRegistryTests
    {
        class FakeEnemy : Enemy
        {
            protected override void Behavior(float dt, PlayerController player) { }
        }

        GameObject go1;
        GameObject go2;

        [SetUp]
        public void SetUp()
        {
            EnemyRegistry.Clear();
            go1 = new GameObject("e", typeof(BoxCollider2D));
            go2 = new GameObject("e2", typeof(BoxCollider2D));
        }

        [TearDown]
        public void TearDown()
        {
            EnemyRegistry.Clear();
            if (go1 != null) Object.DestroyImmediate(go1);
            if (go2 != null) Object.DestroyImmediate(go2);
        }

        // 注：batchmode 的 EditMode 测试不触发 MonoBehaviour 生命周期回调（无播放循环），
        // 因此这里直接显式调用 Register/Unregister 验证注册表纯逻辑；
        // 「激活即自动注册」的生命周期契约由 Task 13 的 PlayMode 冒烟验收覆盖。
        [Test]
        public void Register_Unregister_Updates_AliveCount()
        {
            var e1 = go1.AddComponent<FakeEnemy>();
            var e2 = go2.AddComponent<FakeEnemy>();
            EnemyRegistry.Register(e1);
            EnemyRegistry.Register(e2);
            Assert.That(EnemyRegistry.AliveCount, Is.EqualTo(2));

            EnemyRegistry.Unregister(e1);
            Assert.That(EnemyRegistry.AliveCount, Is.EqualTo(1));
            EnemyRegistry.Unregister(e2);
            Assert.That(EnemyRegistry.AliveCount, Is.EqualTo(0));
        }

        [Test]
        public void Nearest_Returns_Closest_Within_Range()
        {
            var near = go1.AddComponent<FakeEnemy>();
            var far = go2.AddComponent<FakeEnemy>();
            near.transform.position = new Vector3(1, 0, 0);
            far.transform.position = new Vector3(5, 0, 0);
            EnemyRegistry.Register(near);
            EnemyRegistry.Register(far);

            Assert.That(EnemyRegistry.Nearest(Vector2.zero, 2f), Is.SameAs(near));
            Assert.That(EnemyRegistry.Nearest(Vector2.zero, 0.5f), Is.Null);
        }
    }
}