using System;
using System.IO;
using UnityEditor;

[Serializable]
internal sealed class AndroidPublishingSettings
{
    public bool customKeystore;
    public string keystorePath;
    public string alias;
    public bool minifyRelease;
    [NonSerialized] private string keystorePassword = "";
    [NonSerialized] private string aliasPassword = "";

    internal void Read()
    {
        customKeystore = PlayerSettings.Android.useCustomKeystore;
        keystorePath = PlayerSettings.Android.keystoreName;
        alias = PlayerSettings.Android.keyaliasName;
        minifyRelease = PlayerSettings.Android.minifyRelease;
        ClearPasswords();
    }

    internal void Draw()
    {
        customKeystore = EditorGUILayout.Toggle("Custom Keystore", customKeystore);
        using (new EditorGUI.DisabledScope(!customKeystore))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                keystorePath = EditorGUILayout.TextField("Keystore File", keystorePath);
                if (UnityEngine.GUILayout.Button("Browse", UnityEngine.GUILayout.Width(65)))
                {
                    var chosen = EditorUtility.OpenFilePanelWithFilters("Select Release Keystore", "",
                        new[] { "Keystore", "keystore,jks", "All files", "*" });
                    if (!string.IsNullOrEmpty(chosen)) keystorePath = chosen;
                }
            }
            alias = EditorGUILayout.TextField("Project Key / Alias", alias);
            keystorePassword = EditorGUILayout.PasswordField("Keystore Password", keystorePassword);
            aliasPassword = EditorGUILayout.PasswordField("Key Password", aliasPassword);
        }
        EditorGUILayout.HelpBox("Blank password fields keep Unity's current session passwords. Passwords are not " +
                                "saved by this tool and are cleared when this window closes or reloads.", UnityEditor.MessageType.Info);
        minifyRelease = EditorGUILayout.Toggle("Minify Release", minifyRelease);
    }

    internal bool Validate(out string error)
    {
        error = null;
        if (customKeystore && (string.IsNullOrWhiteSpace(keystorePath) || !File.Exists(keystorePath)))
            error = "Choose an existing release keystore file.";
        else if (customKeystore && string.IsNullOrWhiteSpace(alias))
            error = "Enter the release key alias.";
        return error == null;
    }

    internal void Apply()
    {
        PlayerSettings.Android.useCustomKeystore = customKeystore;
        if (customKeystore)
        {
            PlayerSettings.Android.keystoreName = keystorePath;
            PlayerSettings.Android.keyaliasName = alias.Trim();
            if (!string.IsNullOrEmpty(keystorePassword)) PlayerSettings.Android.keystorePass = keystorePassword;
            if (!string.IsNullOrEmpty(aliasPassword)) PlayerSettings.Android.keyaliasPass = aliasPassword;
        }
        PlayerSettings.Android.minifyRelease = minifyRelease;
        ClearPasswords();
    }

    internal void ClearPasswords() => keystorePassword = aliasPassword = "";
}
