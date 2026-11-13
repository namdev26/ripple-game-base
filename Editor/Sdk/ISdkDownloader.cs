using System.Threading.Tasks;

namespace RippleGame.Sdk.Editor
{
    internal interface ISdkDownloader
    {
        /// <summary>Downloads <paramref name="url"/> into the file at <paramref name="destinationPath"/>. Throws on failure.</summary>
        Task DownloadAsync(string url, string destinationPath);
    }
}
