using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;

[Serializable]
internal sealed class AndroidIdentitySettings
{
    public string companyName;
    public string productName;
    public string packageName;
    public bool overridePackageName;

    internal void Read()
    {
        companyName = PlayerSettings.companyName;
        productName = PlayerSettings.productName;
        packageName = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        using (var settings = AndroidSerializedSettings.Open())
            overridePackageName = AndroidSerializedSettings.Require(settings, "overrideDefaultApplicationIdentifier").boolValue;
    }

    internal void Draw()
    {
        companyName = EditorGUILayout.TextField("Company Name", companyName);
        productName = EditorGUILayout.TextField("Product Name", productName);
        overridePackageName = EditorGUILayout.Toggle("Override Package Name", overridePackageName);
        using (new EditorGUI.DisabledScope(!overridePackageName))
            packageName = EditorGUILayout.TextField("Package Name", packageName);
    }

    internal bool Validate(out string error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(companyName) || string.IsNullOrWhiteSpace(productName))
            error = "Company Name and Product Name cannot be empty.";
        else if (overridePackageName && !IsPackageNameValid(packageName))
            error = "Use a package name such as com.studio.game, with at least two valid name segments.";
        return error == null;
    }

    internal static bool IsPackageNameValid(string value) => !string.IsNullOrWhiteSpace(value) &&
        Regex.IsMatch(value, @"\A[A-Za-z][A-Za-z0-9_]*(\.[A-Za-z][A-Za-z0-9_]*)+\z");

    internal void Apply()
    {
        PlayerSettings.companyName = companyName.Trim();
        PlayerSettings.productName = productName.Trim();
        if (overridePackageName) PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, packageName);
        using (var settings = AndroidSerializedSettings.Open())
        {
            AndroidSerializedSettings.Require(settings, "overrideDefaultApplicationIdentifier").boolValue = overridePackageName;
            settings.ApplyModifiedProperties();
        }
    }
}
