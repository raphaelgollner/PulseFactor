using NUnit.Framework;

namespace PulseFactor.Tests
{
    public class SanityTests
    {
        [Test]
        public void Runtime_assembly_is_referenced()
        {
            Assert.IsNotNull(typeof(PulseFactor.GameConfig));
        }
    }
}
