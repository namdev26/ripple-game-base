using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace RippleGame.Sdk.Editor
{
    /// <summary>Downloads straight to disk with <see cref="DownloadHandlerFile"/>; never buffers the tarball in memory.</summary>
    internal sealed class UnityWebRequestSdkDownloader : ISdkDownloader
    {
        public async Task DownloadAsync(string url, string destinationPath)
        {
            using (var request = UnityWebRequest.Get(url))
            {
                request.downloadHandler = new DownloadHandlerFile(destinationPath) { removeFileOnAbort = true };

                var done = new TaskCompletionSource<bool>();
                var operation = request.SendWebRequest();
                operation.completed += _ => done.TrySetResult(true);
                if (operation.isDone) done.TrySetResult(true);
                await done.Task;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    TryDelete(destinationPath);
                    throw new InvalidOperationException(
                        $"Download failed ({request.result}, HTTP {request.responseCode}) for {url}: {request.error}");
                }
            }
        }

        static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
