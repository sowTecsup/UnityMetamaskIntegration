using System.Collections.Generic;
using UnityEngine;
using ZXing;
using ZXing.QrCode;
using ZXing.QrCode.Internal;
using ZXing.Common;

/// <summary>
/// Generates QR code textures using the ZXing.Net core encoder.
/// Does NOT depend on any platform-specific ZXing renderer — works in Editor and on devices.
/// Requires ZXing.Net.dll in Assets/Packages/ZXing.Net/.
/// </summary>
public static class QRCodeGenerator
{
    /// <summary>
    /// Encodes content as a QR code and returns a new Texture2D of the given pixel size.
    /// </summary>
    public static Texture2D Generate(string content, int size = 256)
    {
        var hints = new Dictionary<EncodeHintType, object>
        {
            { EncodeHintType.ERROR_CORRECTION, ErrorCorrectionLevel.M },
            { EncodeHintType.MARGIN, 1 },
            { EncodeHintType.CHARACTER_SET, "UTF-8" }
        };

        var writer = new QRCodeWriter();
        BitMatrix matrix = writer.encode(content, BarcodeFormat.QR_CODE, size, size, hints);

        int width = matrix.Width;
        int height = matrix.Height;

        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.filterMode = FilterMode.Point;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // ZXing y=0 is top; Unity y=0 is bottom — flip vertically
                texture.SetPixel(x, height - 1 - y, matrix[x, y] ? Color.black : Color.white);
            }
        }

        texture.Apply();
        return texture;
    }
}
