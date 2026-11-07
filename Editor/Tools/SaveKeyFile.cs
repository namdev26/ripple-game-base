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

    /// <summary>Like Write, but also refuses when the game already has a key in any folder under assetsRoot.</summary>
    internal static string WriteNew(string assetsRoot, string directory, string key)
    {
        var existing = Directory.GetFiles(assetsRoot, FileName, SearchOption.AllDirectories);
        if (existing.Length > 0)
            throw new IOException("This game already has a save key: " + existing[0].Replace('\\', '/'));

        return Write(directory, key);
    }

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
