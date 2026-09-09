using NUnit.Framework;

namespace Roguelite.Tests
{
    public class SmokeTests
    {
        [Test]
        public void Framework_Is_Loaded()
        {
            Assert.That(2 + 2, Is.EqualTo(4));
        }
    }
}