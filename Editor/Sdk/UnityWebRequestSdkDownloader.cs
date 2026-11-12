using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine.Networking;

namespace RippleGame.Sdk.Editor
{
    /// <summary>
    /// Downloads straight to disk with <see cref="DownloadHandlerFile"/>; never buffers the tarball in memory.
    /// The whole request times out after <see cref="TimeoutSeconds"/>, and it is aborted when no byte arrives
    /// for <see cref="StallSeconds"/>, so a stalled server can never leave the caller waiting forever.
    /// </summary>
    internal sealed class UnityWebRequestSdkDownloader : ISdkDownloader
    {
        internal const int TimeoutSeconds = 600;
        internal const double StallSeconds = 60;

        /// <summary>True when no new bytes arrived for at least <paramref name="stallSeconds"/>.</summary>
        internal static bool IsStalled(ulong lastBytes, ulong currentBytes, double lastProgressTime, double now, double stallSeconds)
        {
            return currentBytes <= lastBytes && now - lastProgressTime >= stallSeconds;
        }

        public async Task DownloadAsync(string url, string destinationPath)
        {
            using (var request = UnityWebRequest.Get(url))
            {
                request.downloadHandler = new DownloadHandlerFile(destinationPath) { removeFileOnAbort = true };
                request.timeout = TimeoutSeconds;

                var done = new TaskCompletionSource<bool>();
                var operation = request.SendWebRequest();
                operation.completed += _ => done.TrySetResult(true);

                var stalled = false;
                ulong lastBytes = 0;
                var lastProgressTime = EditorApplication.timeSinceStartup;
                EditorApplication.CallbackFunction watchdog = () =>
                {
                    if (done.Task.IsCompleted) return;
                    var now = EditorApplication.timeSinceStartup;
                    var bytes = request.downloadedBytes;
                    if (bytes > lastBytes)
                    {
                        lastBytes = bytes;
                        lastProgressTime = now;
                    }
                    else if (IsStalled(lastBytes, bytes, lastProgressTime, now, StallSeconds))
                    {
                        stalled = true;
                        request.Abort();
                        done.TrySetResult(true);
                    }
                };
                EditorApplication.update += watchdog;
                try
                {
                    if (operation.isDone) done.TrySetResult(true);
                    await done.Task;
                }
                finally
                {
                    EditorApplication.update -= watchdog;
                }

                if (stalled || request.result != UnityWebRequest.Result.Success)
                {
                    TryDelete(destinationPath);
                    if (stalled)
                        throw new TimeoutException($"Download stalled (no data for {StallSeconds:0} seconds) for {url}.");
                    var error = request.error ?? string.Empty;
                    var timedOut = error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0
                                   || error.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0;
                    throw new InvalidOperationException(
                        $"Download {(timedOut ? "timed out" : "failed")} ({request.result}, HTTP {request.responseCode}) for {url}: {error}");
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
