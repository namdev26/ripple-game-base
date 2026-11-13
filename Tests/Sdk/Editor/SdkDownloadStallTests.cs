using NUnit.Framework;

namespace RippleGame.Sdk.Editor.Tests
{
    public sealed class SdkDownloadStallTests
    {
        [Test]
        public void IsStalled_NoNewBytesForLongEnough_IsTrue() =>
            Assert.That(UnityWebRequestSdkDownloader.IsStalled(100, 100, 10, 70, 60), Is.True);

        [Test]
        public void IsStalled_NoNewBytesButNotLongEnough_IsFalse() =>
            Assert.That(UnityWebRequestSdkDownloader.IsStalled(100, 100, 10, 69.9, 60), Is.False);

        [Test]
        public void IsStalled_BytesGrew_IsFalseEvenAfterLongTime() =>
            Assert.That(UnityWebRequestSdkDownloader.IsStalled(100, 101, 10, 500, 60), Is.False);

        [Test]
        public void IsStalled_NothingDownloadedYetAndWaitedTooLong_IsTrue() =>
            Assert.That(UnityWebRequestSdkDownloader.IsStalled(0, 0, 0, 60, 60), Is.True);
    }
}
