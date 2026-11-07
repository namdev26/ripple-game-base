using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

internal sealed class LocalizationSheetResult
{
    internal bool Ok => Errors.Count == 0;
    internal readonly List<string> Errors = new List<string>();
    internal readonly List<string> SkippedColumns = new List<string>();
    internal readonly Dictionary<string, string> Files = new Dictionary<string, string>();
}

/// <summary>Checks a downloaded sheet and renders one file per language. Any error means no files.</summary>
internal static class LocalizationSheet
{
    private const string KeyColumn = "KEY_lang";
    private const string English = "English";
    private static readonly string[] Unsupported = { "Arabic", "Unknown" };

    internal static LocalizationSheetResult Build(string downloadedText)
    {
        var result = new LocalizationSheetResult();
        var text = downloadedText ?? string.Empty;
        if (text.TrimStart().StartsWith("<", StringComparison.Ordinal))
        {
            result.Errors.Add("Received a web page instead of CSV. Check that the Sheet is shared with anyone who has the link.");
            return result;
        }

        var rows = SheetCsv.Parse(text);
        // The studio's sheet template leaves the key and English header cells blank.
        if (rows.Count > 0 && rows[0].Length >= 2 && rows[0][0].Trim().Length == 0 && rows[0][1].Trim().Length == 0)
        {
            rows[0][0] = KeyColumn;
            rows[0][1] = English;
        }

        if (rows.Count == 0 || rows[0][0].Trim() != KeyColumn)
        {
            result.Errors.Add("The first column of the Sheet must be " + KeyColumn + ".");
            return result;
        }

        var columns = ReadColumns(rows[0], result);
        if (!columns.ContainsKey(English))
            result.Errors.Add("The Sheet has no " + English + " column.");
        if (!result.Ok)
            return result;

        var contents = columns.Keys.ToDictionary(language => language, language => new StringBuilder(KeyColumn + "," + language + "\n"));
        var seen = new HashSet<string>();
        for (var r = 1; r < rows.Count; r++)
        {
            var key = rows[r][0].Trim();
            if (key.Length == 0 || key == KeyColumn)
                continue;

            if (key.Contains(",") || key.Contains("\n"))
                result.Errors.Add("Invalid key: " + key);
            if (!seen.Add(key))
                result.Errors.Add("Duplicate key: " + key);

            var english = Cell(rows[r], columns[English]);
            var placeholders = Placeholders(english);
            foreach (var column in columns)
            {
                var value = Cell(rows[r], column.Value);
                if (string.IsNullOrWhiteSpace(value))
                    value = english;
                if (!placeholders.SetEquals(Placeholders(value)))
                    result.Errors.Add("Placeholders of " + key + " in " + column.Key + " differ from " + English + ".");

                contents[column.Key].Append(key).Append(',').Append(Render(value)).Append('\n');
            }
        }

        if (result.Ok)
            foreach (var content in contents)
                result.Files[content.Key] = content.Value.ToString();

        return result;
    }

    private static Dictionary<string, int> ReadColumns(string[] header, LocalizationSheetResult result)
    {
        var columns = new Dictionary<string, int>();
        for (var i = 1; i < header.Length; i++)
        {
            var name = header[i].Trim();
            if (name.Length == 0)
                continue;

            if (Array.IndexOf(Unsupported, name) >= 0)
                result.SkippedColumns.Add(name);
            else if (!Regex.IsMatch(name, "^[A-Za-z]+$"))
                result.Errors.Add("Invalid language column: " + name);
            else if (columns.ContainsKey(name))
                result.Errors.Add("Duplicate language column: " + name);
            else
                columns[name] = i;
        }

        return columns;
    }

    private static string Cell(string[] row, int column) => column < row.Length ? row[column] : string.Empty;

    private static HashSet<string> Placeholders(string value)
    {
        return new HashSet<string>(Regex.Matches(value, @"\{\d+\}").Cast<Match>().Select(match => match.Value));
    }

    private static string Render(string value)
    {
        var line = value.Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", string.Empty);
        var needsQuotes = line.Contains(",") || line.Contains("\"") || line != line.Trim();
        return needsQuotes ? "\"" + line.Replace("\"", "\"\"") + "\"" : line;
    }
}
