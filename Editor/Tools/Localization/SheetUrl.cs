using System;
using System.Text.RegularExpressions;

internal static class SheetUrl
{
    internal static string ToCsvExport(string input)
    {
        var url = (input ?? string.Empty).Trim();
        var id = Regex.Match(url, @"spreadsheets/d/([^/?#]+)");
        if (!id.Success)
            throw new ArgumentException("This link has no Spreadsheet ID. Copy the link of the Google Sheet.");

        if (url.Contains("export?format=csv"))
            return url;

        var gid = Regex.Match(url, @"gid=(\d+)");
        return "https://docs.google.com/spreadsheets/d/" + id.Groups[1].Value +
               "/export?format=csv&gid=" + (gid.Success ? gid.Groups[1].Value : "0");
    }
}
