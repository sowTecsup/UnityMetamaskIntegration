using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ZXing;

/// <summary>
/// Activates the device camera (or first available webcam in Editor) and decodes QR codes
/// using ZXing.Net. Call StartScanning(callback) / StopScanning() from external UI code.
/// Requires a RawImage in the scene to display the camera feed.
/// </summary>
public class QRScannerUI : MonoBehaviour
{
    [SerializeField] private RawImage cameraPreview;
    [SerializeField] private GameObject scannerPanel;

    // How many frames to skip between decode attempts (higher = less CPU usage)
    [SerializeField] private int decodeInterval = 10;

    private WebCamTexture _webCamTexture;
    private Action<string> _onDecoded;
    private bool _scanning;
    private int _frameCounter;

    public bool IsScanning => _scanning;

    /// <summary>Opens the scanner panel and starts decoding. Callback fires once when a QR is found.</summary>
    public void StartScanning(Action<string> onDecoded)
    {
        _onDecoded = onDecoded;
        _scanning = true;
        scannerPanel.SetActive(true);
        StartCoroutine(InitCamera());
    }

    /// <summary>Stops the scanner and hides the panel.</summary>
    public void StopScanning()
    {
        _scanning = false;
        StopAllCoroutines();
        if (_webCamTexture != null && _webCamTexture.isPlaying)
            _webCamTexture.Stop();
        scannerPanel.SetActive(false);
    }

    private IEnumerator InitCamera()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            Debug.LogWarning("QRScanner: camera permission denied.");
            StopScanning();
            yield break;
        }

        // Prefer rear camera
        WebCamDevice selectedDevice = default;
        bool found = false;
        foreach (var device in WebCamTexture.devices)
        {
            if (!device.isFrontFacing) { selectedDevice = device; found = true; break; }
        }
        if (!found && WebCamTexture.devices.Length > 0)
        {
            selectedDevice = WebCamTexture.devices[0];
            found = true;
        }

        if (!found)
        {
            Debug.LogWarning("QRScanner: no camera found.");
            StopScanning();
            yield break;
        }

        _webCamTexture = new WebCamTexture(selectedDevice.name, 1280, 720, 30);
        cameraPreview.texture = _webCamTexture;
        _webCamTexture.Play();

        // Wait until camera is actually producing frames
        while (_webCamTexture.width < 16)
            yield return null;

        StartCoroutine(ScanLoop());
    }

    private IEnumerator ScanLoop()
    {
        var reader = new BarcodeReaderGeneric
        {
            AutoRotate = true,
            Options = new ZXing.Common.DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new[] { BarcodeFormat.QR_CODE }
            }
        };

        while (_scanning)
        {
            _frameCounter++;
            if (_frameCounter >= decodeInterval)
            {
                _frameCounter = 0;
                TryDecode(reader);
            }
            yield return null;
        }
    }

    private void TryDecode(BarcodeReaderGeneric reader)
    {
        if (_webCamTexture == null || !_webCamTexture.isPlaying) return;

        Color32[] pixels = _webCamTexture.GetPixels32();

        byte[] rawBytes = new byte[pixels.Length * 4];
        for (int i = 0; i < pixels.Length; i++)
        {
            int offset = i * 4;
            rawBytes[offset] = pixels[i].r;
            rawBytes[offset + 1] = pixels[i].g;
            rawBytes[offset + 2] = pixels[i].b;
            rawBytes[offset + 3] = pixels[i].a;
        }

        var result = reader.Decode(rawBytes, _webCamTexture.width, _webCamTexture.height,
            RGBLuminanceSource.BitmapFormat.RGBA32);

        if (result != null && !string.IsNullOrEmpty(result.Text))
        {
            string decoded = result.Text;
            StopScanning();
            _onDecoded?.Invoke(decoded);
        }
    }

    private void OnDestroy()
    {
        if (_webCamTexture != null && _webCamTexture.isPlaying)
            _webCamTexture.Stop();
    }
}
