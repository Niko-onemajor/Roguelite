using NUnit.Framework;
using Roguelite;
using UnityEngine;

namespace Roguelite.Tests
{
    public class PoolManagerTests
    {
        [Test]
        public void Spawn_Returns_Active_And_Reuses_After_Return()
        {
            var proto = new GameObject("proto");
            proto.AddComponent<Poolable>();
            proto.SetActive(false);

            var a = PoolManager.Spawn(proto, Vector3.one, Quaternion.identity);
            Assert.That(a, Is.Not.SameAs(proto));
            Assert.That(a.activeSelf, Is.True);

            PoolManager.Return(a);
            Assert.That(a.activeSelf, Is.False);

            var b = PoolManager.Spawn(proto, Vector3.zero, Quaternion.identity);
            Assert.That(b, Is.SameAs(a), "回收后应复用同一实例");

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(proto);
        }
    }
}