using NUnit.Framework;

namespace RippleGame.Ads.Tests
{
    public sealed class AdsLoadRetryTests
    {
        [Test]
        public void Delay_RampsExponentially_ThenStaysAtMaxDelayForever()
        {
            // 3 ramp attempts from 2 s, capped at 30 s.
            Assert.That(AdsLoadRetry.DelaySeconds(1, 3, 2f, 30f), Is.EqualTo(2f));
            Assert.That(AdsLoadRetry.DelaySeconds(2, 3, 2f, 30f), Is.EqualTo(4f));
            Assert.That(AdsLoadRetry.DelaySeconds(3, 3, 2f, 30f), Is.EqualTo(8f));
            Assert.That(AdsLoadRetry.DelaySeconds(4, 3, 2f, 30f), Is.EqualTo(30f), "after the ramp: max delay, never stop");
            Assert.That(AdsLoadRetry.DelaySeconds(1000, 3, 2f, 30f), Is.EqualTo(30f));
        }

        [Test]
        public void Delay_RampIsCappedByMaxDelay()
        {
            Assert.That(AdsLoadRetry.DelaySeconds(5, 10, 2f, 10f), Is.EqualTo(10f));
            Assert.That(AdsLoadRetry.DelaySeconds(60, 100, 2f, 10f), Is.EqualTo(10f), "no overflow for large attempts");
        }

        [Test]
        public void Delay_ZeroAttempts_UsesMaxDelayAtOnce_AndNeverBusyLoops()
        {
            Assert.That(AdsLoadRetry.DelaySeconds(1, 0, 2f, 30f), Is.EqualTo(30f));
            Assert.That(AdsLoadRetry.DelaySeconds(1, 0, 0f, 0f), Is.GreaterThan(0f));
        }
    }
}
