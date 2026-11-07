using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

internal sealed class LocalizationWriteReport
{
    internal readonly List<string> Written = new List<string>();
    internal readonly List<string> Unchanged = new List<string>();
    internal readonly Dictionary<string, List<string>> RemovedKeys = new Dictionary<string, List<string>>();
}

/// <summary>Writes language files as UTF-8 without BOM. A file whose content is the same stays untouched.</summary>
internal static class LocalizationFileWriter
{
    internal static LocalizationWriteReport Write(string resourcesFolder, Dictionary<string, string> files)
    {
        var report = new LocalizationWriteReport();
        Directory.CreateDirectory(resourcesFolder);
        foreach (var file in files)
        {
            var name = file.Key == "English" ? "Localization.txt" : "Localization-" + file.Key + ".txt";
            var path = Path.Combine(resourcesFolder, name);
            var old = File.Exists(path) ? File.ReadAllText(path) : null;
            if (old == file.Value)
            {
                report.Unchanged.Add(name);
                continue;
            }

            if (old != null)
            {
                var removed = Keys(old).Except(Keys(file.Value)).ToList();
                if (removed.Count > 0)
                    report.RemovedKeys[file.Key] = removed;
            }

            File.WriteAllText(path, file.Value, new UTF8Encoding(false));
            report.Written.Add(name);
        }

        return report;
    }

    private static IEnumerable<string> Keys(string content)
    {
        return SheetCsv.Parse(content).Skip(1).Select(row => row[0]).Where(key => key.Length > 0);
    }
}
