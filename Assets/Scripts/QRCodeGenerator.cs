using UnityEngine;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;
using ZXing.QrCode;

public static class QRCodeGenerator
{
    public static Texture2D Generate(string content, int size = 256)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                ErrorCorrection = ZXing.QrCode.Internal.ErrorCorrectionLevel.M,
                Margin = 1,
                Width = size,
                Height = size
            }
        };

        PixelData pixelData = writer.Write(content);

        var texture = new Texture2D(pixelData.Width, pixelData.Height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        Color32[] colors = new Color32[pixelData.Pixels.Length / 4];
        for (int i = 0; i < colors.Length; i++)
        {
            int offset = i * 4;
            colors[i] = new Color32(
                pixelData.Pixels[offset],
                pixelData.Pixels[offset + 1],
                pixelData.Pixels[offset + 2],
                pixelData.Pixels[offset + 3]
            );
        }

        texture.SetPixels32(colors);
        texture.Apply();
        return texture;
    }
}
