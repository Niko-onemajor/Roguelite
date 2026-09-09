using System;
using NUnit.Framework;
using Roguelite;

namespace Roguelite.Tests
{
    public class DamageUtilitiesTests
    {
        [Test]
        public void RollCrit_Chance1_AlwaysTrue_Chance0_AlwaysFalse()
        {
            var rng = new Random(123);
            Assert.That(DamageUtilities.RollCrit(1f, rng), Is.True);
            Assert.That(DamageUtilities.RollCrit(0f, rng), Is.False);
        }

        [Test]
        public void RollCrit_Deterministic_WithSameSeed()
        {
            var a = new Random(42);
            var b = new Random(42);
            for (int i = 0; i < 20; i++)
                Assert.That(DamageUtilities.RollCrit(0.5f, a), Is.EqualTo(DamageUtilities.RollCrit(0.5f, b)));
        }

        [Test]
        public void ComputeCrit_Multiplies_Only_When_Crit()
        {
            Assert.That(DamageUtilities.ComputeCrit(10f, true, 2f), Is.EqualTo(20f));
            Assert.That(DamageUtilities.ComputeCrit(10f, false, 2f), Is.EqualTo(10f));
        }
    }
}