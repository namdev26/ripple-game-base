using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

internal sealed class PackageUpdateStatus
{
    internal string InstalledVersion;
    /// <summary>Installed from a Git URL, so the Package Manager can move it to another tag.</summary>
    internal bool InstalledFromGit;
    internal string Source;
    internal PackageRelease Latest;
    internal string Error;

    internal bool UpdateAvailable => PackageUpdateLogic.IsNewer(InstalledVersion, Latest);
}

[InitializeOnLoad]
internal static class PackageUpdates
{
    private const string CheckedThisSession = "RippleGame.Base.UpdateChecked";

    static PackageUpdates()
    {
        // Once per Editor launch, and never on a build machine.
        if (Application.isBatchMode || SessionState.GetBool(CheckedThisSession, false)) return;
        SessionState.SetBool(CheckedThisSession, true);
        EditorApplication.delayCall += NotifyIfNewer;
    }

    internal static async Task<PackageUpdateStatus> CheckAsync()
    {
        var package = PackageInfo.FindForAssembly(typeof(PackageUpdates).Assembly);
        var status = new PackageUpdateStatus
        {
            InstalledVersion = package?.version,
            InstalledFromGit = package?.source == PackageSource.Git
        };
        status.Source = status.InstalledFromGit ? PackageUpdateLogic.SourceOf(package.packageId) : package?.repository?.url;
        if (string.IsNullOrEmpty(status.Source))
        {
            status.Error = "This package has no repository to check.";
            return status;
        }

        var remote = PackageUpdateLogic.RemoteUrl(status.Source);
        try
        {
            status.Latest = await Task.Run(() => PackageUpdateGit.FetchLatest(remote));
        }
        catch (IOException exception)
        {
            status.Error = exception.Message;
        }

        return status;
    }

    /// <param name="finished">Receives the error, or null when the package is about to reload.</param>
    internal static void Install(PackageUpdateStatus status, Action<string> finished)
    {
        var request = Client.Add(PackageUpdateLogic.InstallUrl(status.Source, status.Latest.Tag));
        EditorApplication.CallbackFunction poll = null;
        poll = () =>
        {
            if (!request.IsCompleted) return;
            EditorApplication.update -= poll;
            finished(request.Status == StatusCode.Failure ? request.Error.message : null);
        };
        EditorApplication.update += poll;
    }

    private static async void NotifyIfNewer()
    {
        var status = await CheckAsync();
        // Being offline at launch is normal, so a failed check stays quiet here. The Update page shows it.
        if (status.UpdateAvailable)
            Debug.Log("RIPPLE Game Base " + status.Latest.Tag + " is available (installed " + status.InstalledVersion + "). Open TOOL > Open Hub > Update.");
    }
}
