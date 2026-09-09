using System.Collections.Generic;
using NUnit.Framework;
using Roguelite;
using UnityEngine;

namespace Roguelite.Tests
{
    public class DamageSystemTests
    {
        class TestEnemy : Enemy
        {
            protected override void Behavior(float dt, PlayerController player) { }
        }

        readonly List<GameObject> objects = new List<GameObject>();

        [SetUp] public void SetUp() => EnemyRegistry.Clear();
        [TearDown]
        public void TearDown()
        {
            EnemyRegistry.Clear();
            foreach (var o in objects) if (o != null) Object.DestroyImmediate(o);
            objects.Clear();
        }

        Enemy MakeEnemy(Vector3 pos, float hp)
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            data.maxHP = hp;
            var go = new GameObject("e", typeof(BoxCollider2D));
            go.transform.position = pos;
            var e = go.AddComponent<TestEnemy>();
            e.Init(data);
            // batchmode EditMode 不触发 OnEnable，须显式注册
            EnemyRegistry.Register(e);
            objects.Add(go);
            return e;
        }

        [Test]
        public void RadiusHit_Damages_Only_Enemies_In_Radius()
        {
            MakeEnemy(new Vector3(2, 0, 0), 100f);
            MakeEnemy(new Vector3(5, 0, 0), 100f);

            DamageSystem.RadiusHit(Vector2.zero, 3f, 10f, 0f);

            var list = EnemyRegistry.All;
            Assert.That(list[0].Health, Is.EqualTo(90f));
            Assert.That(list[1].Health, Is.EqualTo(100f));
        }

        [Test]
        public void MeleeHit_Hits_Only_Enemies_Within_Arc()
        {
            MakeEnemy(new Vector3(0, 1, 0), 100f); // 正前方
            MakeEnemy(new Vector3(1, 0, 0), 100f); // 正侧方 90°

            DamageSystem.MeleeHit(Vector2.zero, Vector2.up, 2f, 45f, 10f, 0f);

            var list = EnemyRegistry.All;
            Assert.That(list[0].Health, Is.EqualTo(90f));
            Assert.That(list[1].Health, Is.EqualTo(100f));
        }
    }
}