using System;
using System.IO;
using RippleGame.Economy;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

public static class RippleBaseSmoke
{
    public static void Run()
    {
        try
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(CurrencyWallet).Assembly);
            HubProof.Require(package != null && package.name == "com.ripplegame.base", "combined package identity");
            var expectationPath = Path.Combine(Directory.GetCurrentDirectory(), "ExpectedVersion.txt");
            var expected = File.ReadAllText(expectationPath).Trim();
            HubProof.Require(package.version == expected, "package revision " + expected);
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "RequireGit.txt")))
                HubProof.Require(package.source == PackageSource.Git, "actual Git dependency install");
            HubProof.Run();
            ConsumerProof.Run();
            var result = "RIPPLE_BASE_NATIVE_PASS version=" + package.version + " source=" + package.source;
            Debug.Log(result);
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "BaseSmokeResult.txt"), result);
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }
}
