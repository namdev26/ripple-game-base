using System;
using UnityEditor;
using UnityEditor.Build;

[Serializable]
internal sealed class AndroidConfigurationSettings
{
    public int minimumApi;
    public int targetApi;
    public ScriptingImplementation backend;
    public ApiCompatibilityLevel compatibility;
    public Il2CppCompilerConfiguration compiler;
    public AndroidArchitecture architectures;
    public bool requireInternet;
    public bool externalWrite;
    public AndroidPreferredInstallLocation installLocation;
    public int inputHandling;

    internal void Read()
    {
        minimumApi = (int)PlayerSettings.Android.minSdkVersion;
        targetApi = (int)PlayerSettings.Android.targetSdkVersion;
        backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android);
        compatibility = PlayerSettings.GetApiCompatibilityLevel(NamedBuildTarget.Android);
        compiler = PlayerSettings.GetIl2CppCompilerConfiguration(NamedBuildTarget.Android);
        architectures = PlayerSettings.Android.targetArchitectures;
        requireInternet = PlayerSettings.Android.forceInternetPermission;
        externalWrite = PlayerSettings.Android.forceSDCardPermission;
        installLocation = PlayerSettings.Android.preferredInstallLocation;
        using (var settings = AndroidSerializedSettings.Open())
            inputHandling = AndroidSerializedSettings.Require(settings, "activeInputHandler").intValue;
    }

    internal void Recommended()
    {
        minimumApi = 26;
        targetApi = 36;
        backend = ScriptingImplementation.IL2CPP;
        compatibility = ApiCompatibilityLevel.NET_Standard;
        compiler = Il2CppCompilerConfiguration.Release;
        architectures = AndroidArchitecture.ARM64;
        requireInternet = true;
        externalWrite = false;
        installLocation = AndroidPreferredInstallLocation.Auto;
    }

    internal void Draw()
    {
        minimumApi = EditorGUILayout.IntField("Minimum API Level", minimumApi);
        targetApi = EditorGUILayout.IntField("Target API (0 = Automatic)", targetApi);
        backend = (ScriptingImplementation)EditorGUILayout.EnumPopup("Scripting Backend", backend);
        var apiIndex = compatibility == ApiCompatibilityLevel.NET_Standard ? 0 : 1;
        apiIndex = EditorGUILayout.Popup("API Compatibility", apiIndex, new[] { ".NET Standard 2.1", ".NET Framework" });
        compatibility = apiIndex == 0 ? ApiCompatibilityLevel.NET_Standard : ApiCompatibilityLevel.NET_Unity_4_8;
        compiler = (Il2CppCompilerConfiguration)EditorGUILayout.EnumPopup("C++ Compiler Configuration", compiler);
        architectures = (AndroidArchitecture)EditorGUILayout.EnumFlagsField("Target Architectures", architectures);
        requireInternet = EditorGUILayout.Toggle("Internet Access: Require", requireInternet);
        externalWrite = EditorGUILayout.Toggle("External Write Permission", externalWrite);
        installLocation = (AndroidPreferredInstallLocation)EditorGUILayout.EnumPopup("Install Location", installLocation);
        inputHandling = EditorGUILayout.Popup("Active Input Handling", inputHandling,
            new[] { "Input Manager", "Input System", "Both" });
        EditorGUILayout.HelpBox("Input Handling follows your code; changing it can require restarting Unity. " +
                                "External Write disabled means Internal storage.", MessageType.Info);
    }

    internal bool Validate(out string error)
    {
        error = null;
        if (minimumApi < 24) error = "This project's ads SDK needs Minimum API 24 or higher.";
        else if (targetApi != 0 && targetApi < minimumApi) error = "Target API must be at least Minimum API.";
        else if (architectures == AndroidArchitecture.None) error = "Select a target architecture.";
        else if ((architectures & AndroidArchitecture.ARM64) != 0 && backend != ScriptingImplementation.IL2CPP)
            error = "ARM64 requires the IL2CPP scripting backend.";
        return error == null;
    }

    internal void Apply()
    {
        PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)minimumApi;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)targetApi;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, backend);
        PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Android, compatibility);
        PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, compiler);
        PlayerSettings.Android.targetArchitectures = architectures;
        PlayerSettings.Android.forceInternetPermission = requireInternet;
        PlayerSettings.Android.forceSDCardPermission = externalWrite;
        PlayerSettings.Android.preferredInstallLocation = installLocation;
    }

    internal void ApplyInputHandling()
    {
        using (var settings = AndroidSerializedSettings.Open())
        {
            var input = AndroidSerializedSettings.Require(settings, "activeInputHandler");
            if (input.intValue == inputHandling) return;
            input.intValue = inputHandling;
            settings.ApplyModifiedProperties();
        }
    }
}
