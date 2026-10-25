using System;
using UnityEngine;

internal static class TextureCanvasPadding
{
    internal static int RoundUp(int value)
    {
        if (value < 1) throw new ArgumentOutOfRangeException(nameof(value));
        return checked((value + 3) / 4 * 4);
    }

    internal static Color32[] Pad(Color32[] source, int width, int height, bool transparent)
    {
        if (source == null || source.Length != checked(width * height))
            throw new ArgumentException("Pixel count does not match the source dimensions.");
        var newWidth = RoundUp(width);
        var newHeight = RoundUp(height);
        var pixels = new Color32[checked(newWidth * newHeight)];
        if (!transparent)
            for (var i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, 255);

        // Unity stores rows bottom-up. Shift content upwards to keep its original top-left anchor.
        var bottomPadding = newHeight - height;
        for (var y = 0; y < height; y++)
            Array.Copy(source, y * width, pixels, (y + bottomPadding) * newWidth, width);
        return pixels;
    }
}
