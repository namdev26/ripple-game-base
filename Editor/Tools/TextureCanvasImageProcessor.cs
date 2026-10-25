using System;
using UnityEngine;

internal static class TextureCanvasImageProcessor
{
    internal static byte[] Resize(byte[] bytes, bool png)
    {
        Texture2D source = null;
        Texture2D result = null;
        try
        {
            source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(source, bytes, false))
                throw new ArgumentException("Unity could not decode this image.");
            var width = TextureCanvasPadding.RoundUp(source.width);
            var height = TextureCanvasPadding.RoundUp(source.height);
            if (width == source.width && height == source.height) return null;

            var pixels = TextureCanvasPadding.Pad(source.GetPixels32(), source.width, source.height,
                png && PngAlphaReader.HasAlpha(bytes));
            result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            result.SetPixels32(pixels);
            result.Apply(false, false);
            var encoded = png ? ImageConversion.EncodeToPNG(result) : ImageConversion.EncodeToJPG(result, 95);
            if (encoded == null || encoded.Length == 0)
                throw new InvalidOperationException("Unity could not encode the resized image.");
            return encoded;
        }
        finally
        {
            if (result != null) UnityEngine.Object.DestroyImmediate(result);
            if (source != null) UnityEngine.Object.DestroyImmediate(source);
        }
    }
}
