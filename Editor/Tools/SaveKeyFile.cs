using System.IO;

/// <summary>Writes the project-owned C# file holding the game's save key.</summary>
internal static class SaveKeyFile
{
    internal const string FileName = "GameSaveKey.cs";

    internal static string Render(string key) =>
        "// Encryption key for this game's save files. Created by TOOL Hub.\n" +
        "// Never change it after release: existing saves could no longer be read.\n" +
        "internal static class GameSaveKey\n" +
        "{\n" +
        "    public const string Value = \"" + key + "\";\n" +
        "}\n";

    /// <summary>Returns the created path. Throws IOException instead of replacing an existing key.</summary>
    internal static string Write(string directory, string key)
    {
        var path = Path.Combine(directory, FileName);
        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
        using (var writer = new StreamWriter(stream))
            writer.Write(Render(key));
        return path;
    }
}
