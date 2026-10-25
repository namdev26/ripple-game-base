using System;

internal static class PngAlphaReader
{
    internal static bool HasAlpha(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 33 || bytes[0] != 137 || bytes[1] != 80 ||
            bytes[2] != 78 || bytes[3] != 71) return false;
        var colorType = bytes[25];
        if (colorType == 4 || colorType == 6) return true;

        // PNGs without an alpha channel can still declare transparency with a tRNS chunk.
        for (var offset = 8; offset <= bytes.Length - 12;)
        {
            var length = ((long)bytes[offset] << 24) | ((long)bytes[offset + 1] << 16) |
                         ((long)bytes[offset + 2] << 8) | bytes[offset + 3];
            if (length > bytes.Length - offset - 12) return false;
            if (bytes[offset + 4] == 't' && bytes[offset + 5] == 'R' &&
                bytes[offset + 6] == 'N' && bytes[offset + 7] == 'S') return true;
            offset += (int)length + 12;
        }
        return false;
    }
}
