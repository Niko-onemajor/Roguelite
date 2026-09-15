using NUnit.Framework;
using UnityEngine;

namespace Roguelite.Tests
{
    public class ArenaBoundsTests
    {
        [Test]
        public void Clamp_Inside_Returns_Original()
        {
            Vector2 pos = new Vector2(3f, -4f); // 距心 5 < 半边长
            Assert.That(ArenaBounds.Clamp(pos), Is.EqualTo(pos));
        }

        [Test]
        public void Clamp_Outside_Clamps_To_Square_Boundary()
        {
            Vector2 pos = new Vector2(30f, 4f); // x 远超边界
            Vector2 clamped = ArenaBounds.Clamp(pos);
            Assert.That(Mathf.Abs(clamped.x), Is.LessThanOrEqualTo(ArenaBounds.Radius + 0.01f));
            Assert.That(clamped.y, Is.EqualTo(4f).Within(0.001f)); // 方形仅夹取越界轴，另一轴不变
        }

        [Test]
        public void Clamp_Clamps_Both_Axes()
        {
            Vector2 pos = new Vector2(-25f, 20f);
            Vector2 clamped = ArenaBounds.Clamp(pos);
            Assert.That(Mathf.Abs(clamped.x), Is.LessThanOrEqualTo(ArenaBounds.Radius + 0.01f));
            Assert.That(Mathf.Abs(clamped.y), Is.LessThanOrEqualTo(ArenaBounds.Radius + 0.01f));
            Assert.That(clamped.x, Is.LessThan(0f), "负 x 应被钳到负半区");
        }

        [Test]
        public void RandomRingPosition_Always_On_Square_SpawnRing()
        {
            float ring = ArenaBounds.Radius - 2f; // 18，随 Radius 联动
            for (int i = 0; i < 200; i++)
            {
                Vector2 p = ArenaBounds.RandomRingPosition();
                // 方形刷怪环：|x| 或 |y| 恰为 ring，另一轴在 [-ring, ring]
                float mx = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y));
                Assert.That(mx, Is.InRange(ring - 0.1f, ring + 0.1f), "随机点应落在方形刷怪环上");
                Assert.That(Mathf.Min(Mathf.Abs(p.x), Mathf.Abs(p.y)),
                    Is.LessThanOrEqualTo(ring + 0.1f));
            }
        }

        [Test]
        public void Contains_Square_Region()
        {
            Assert.That(ArenaBounds.Contains(new Vector2(0.95f * ArenaBounds.Radius, 0.95f * ArenaBounds.Radius)), Is.True);  // 角落
            Assert.That(ArenaBounds.Contains(new Vector2(0f, ArenaBounds.Radius)), Is.True);     // 边沿
            Assert.That(ArenaBounds.Contains(new Vector2(1.2f * ArenaBounds.Radius, 0f)), Is.False);    // 超界
            Assert.That(ArenaBounds.Contains(new Vector2(0f, -1.1f * ArenaBounds.Radius)), Is.False);   // 超界
        }
    }
}