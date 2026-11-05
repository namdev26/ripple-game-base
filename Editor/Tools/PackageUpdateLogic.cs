using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

internal sealed class PackageRelease
{
    internal string Tag { get; }
    internal Version Version { get; }

    internal PackageRelease(string tag, Version version)
    {
        Tag = tag;
        Version = version;
    }
}

internal static class PackageUpdateLogic
{
    // Only final releases: v1.2.3 or 1.2.3. Prereleases and other tags never count as an update.
    private static readonly Regex ReleaseTag = new Regex(@"\trefs/tags/(v?(\d+\.\d+\.\d+))\s*$", RegexOptions.CultureInvariant);

    internal static PackageRelease ParseLatest(string lsRemoteOutput)
    {
        PackageRelease latest = null;
        foreach (var line in lsRemoteOutput.Split('\n'))
        {
            var match = ReleaseTag.Match(line);
            if (!match.Success || !Version.TryParse(match.Groups[2].Value, out var version)) continue;
            if (latest == null || version > latest.Version) latest = new PackageRelease(match.Groups[1].Value, version);
        }

        return latest;
    }

    internal static bool IsNewer(string installedVersion, PackageRelease release)
    {
        return release != null && Version.TryParse(installedVersion, out var installed) && release.Version > installed;
    }

    internal static string SourceOf(string packageId) => packageId.Substring(packageId.IndexOf('@') + 1);

    internal static string RemoteUrl(string source)
    {
        var url = WithoutRevision(source);
        var query = url.IndexOf('?');
        if (query >= 0) url = url.Substring(0, query);
        return url.StartsWith("git+", StringComparison.Ordinal) ? url.Substring(4) : url;
    }

    internal static string InstallUrl(string source, string tag) => WithoutRevision(source) + "#" + tag;

    internal static string ChangelogUrl(string source, string tag)
    {
        var url = RemoteUrl(source);
        if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return null;
        if (url.EndsWith(".git", StringComparison.OrdinalIgnoreCase)) url = url.Substring(0, url.Length - 4);
        return url + "/blob/" + tag + "/CHANGELOG.md";
    }

    private static string WithoutRevision(string source)
    {
        var revision = source.IndexOf('#');
        return revision < 0 ? source : source.Substring(0, revision);
    }
}

internal static class PackageUpdateGit
{
    private const int TimeoutMilliseconds = 20000;

    /// <summary>Reads the newest release tag. Blocks on the network, so call it off the main thread.</summary>
    /// <exception cref="IOException">Git is missing, times out or cannot read the repository.</exception>
    internal static PackageRelease FetchLatest(string remoteUrl)
    {
        var info = new ProcessStartInfo("git", "ls-remote --tags --refs -- \"" + remoteUrl.Replace("\"", "\\\"") + "\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        // A private repository without credentials must fail, not wait on a prompt nobody can see.
        info.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";
        info.EnvironmentVariables["GCM_INTERACTIVE"] = "never";

        try
        {
            using (var process = Process.Start(info))
            {
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(TimeoutMilliseconds))
                {
                    process.Kill();
                    throw new IOException("Git did not answer in time.");
                }

                if (process.ExitCode != 0) throw new IOException(error.Result.Trim());
                return PackageUpdateLogic.ParseLatest(output.Result);
            }
        }
        catch (Win32Exception exception)
        {
            throw new IOException("Git is not available: " + exception.Message);
        }
    }
}
