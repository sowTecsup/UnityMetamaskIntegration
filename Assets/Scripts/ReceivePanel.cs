using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel "Recibir". Muestra dos modos:
///   Flujo 1 — QR solo con la dirección: el que envía elige el monto.
///   Flujo 2 — QR con monto fijo (EIP-681): el que envía ve el monto pre-cargado.
/// </summary>
public class ReceivePanel : MonoBehaviour
{
    [Header("Sección: solo dirección (Flujo 1)")]
    [SerializeField] private RawImage addressQRImage;
    [SerializeField] private TMP_Text addressLabel;

    [Header("Sección: solicitar monto fijo (Flujo 2)")]
    [SerializeField] private TMP_InputField amountInput;
    [SerializeField] private Button generateRequestButton;
    [SerializeField] private RawImage requestQRImage;
    [SerializeField] private TMP_Text requestUriLabel;

    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [SerializeField] private int qrSize = 256;

    private void Awake()
    {
        generateRequestButton.onClick.AddListener(GenerateRequestQR);
        panel.SetActive(false);
    }

    private void OnEnable()
    {
        if (SimpleWalletLogin.Instance != null)
            SimpleWalletLogin.Instance.OnWalletStateChanged += Refresh;
    }

    private void OnDisable()
    {
        if (SimpleWalletLogin.Instance != null)
            SimpleWalletLogin.Instance.OnWalletStateChanged -= Refresh;
    }

    public void Show()
    {
        panel.SetActive(true);
        Refresh();
    }

    public void Hide() => panel.SetActive(false);

    private void Refresh()
    {
        if (!SimpleWalletLogin.Instance.IsLoggedIn) return;

        string address = SimpleWalletLogin.Instance.WalletAddress;
        addressLabel.text = address;

        // Flujo 1: QR de solo dirección
        string addressUri = EIP681.BuildAddressUri(address);
        addressQRImage.texture = QRCodeGenerator.Generate(addressUri, qrSize);

        // Limpia QR de solicitud hasta que el usuario genere uno nuevo
        requestQRImage.texture = null;
        requestUriLabel.text = string.Empty;
    }

    private void GenerateRequestQR()
    {
        if (!SimpleWalletLogin.Instance.IsLoggedIn)
        {
            requestUriLabel.text = "Debes iniciar sesión primero.";
            return;
        }

        string raw = amountInput.text.Trim().Replace(",", ".");
        if (!decimal.TryParse(raw, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal amount) || amount <= 0)
        {
            requestUriLabel.text = "Ingresa un monto válido (ej: 0.01)";
            return;
        }

        string address = SimpleWalletLogin.Instance.WalletAddress;
        string uri = EIP681.BuildRequestUri(address, amount);

        // Flujo 2: QR con monto fijo
        requestQRImage.texture = QRCodeGenerator.Generate(uri, qrSize);
        requestUriLabel.text = uri;
    }
}
