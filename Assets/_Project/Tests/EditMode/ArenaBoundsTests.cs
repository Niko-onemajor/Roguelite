using NUnit.Framework;
using UnityEngine;

namespace Roguelite.Tests
{
    public class ArenaBoundsTests
    {
        [Test]
        public void Clamp_Inside_Returns_Original()
        {
            Vector2 pos = new Vector2(3f, -4f); // 距心 5 < 半径
            Assert.That(ArenaBounds.Clamp(pos), Is.EqualTo(pos));
        }

        [Test]
        public void Clamp_Outside_Projects_To_Boundary()
        {
            Vector2 pos = new Vector2(30f, 0f); // 远超边界
            Vector2 clamped = ArenaBounds.Clamp(pos);
            Assert.That(clamped.magnitude, Is.LessThanOrEqualTo(ArenaBounds.Radius + 0.01f));
            Assert.That(clamped.y, Is.EqualTo(0f).Within(0.001f)); // 沿径向投影
        }

        [Test]
        public void RandomRingPosition_Always_On_SpawnRing()
        {
            for (int i = 0; i < 100; i++)
            {
                Vector2 p = ArenaBounds.RandomRingPosition();
                Assert.That(p.magnitude, Is.InRange(8.9f, 9.1f), "随机点应落在刷怪环(半径9)附近");
            }
        }
    }
}