using System.Collections.Generic;
using System.Text;

/// <summary>Reads CSV: quoted fields may contain commas, line breaks and doubled quotes.</summary>
internal static class SheetCsv
{
    internal static List<string[]> Parse(string csv)
    {
        var rows = new List<string[]>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        var text = csv.TrimStart('﻿');

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c != '"')
                    field.Append(c);
                else if (i + 1 < text.Length && text[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else
                    quoted = false;
            }
            else if (c == '"')
                quoted = true;
            else if (c == ',')
                EndField(row, field);
            else if (c == '\n')
                EndRow(rows, row, field);
            else if (c != '\r')
                field.Append(c);
        }

        if (field.Length > 0 || row.Count > 0)
            EndRow(rows, row, field);

        return rows;
    }

    private static void EndField(List<string> row, StringBuilder field)
    {
        row.Add(field.ToString());
        field.Clear();
    }

    private static void EndRow(List<string[]> rows, List<string> row, StringBuilder field)
    {
        EndField(row, field);
        rows.Add(row.ToArray());
        row.Clear();
    }
}
