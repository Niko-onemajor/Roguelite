using NUnit.Framework;
using Roguelite;

namespace Roguelite.Tests
{
    public class SmokeTests
    {
        [Test]
        public void Framework_Is_Loaded()
        {
            Assert.That(typeof(GameEvents), Is.Not.Null);
        }
    }
}