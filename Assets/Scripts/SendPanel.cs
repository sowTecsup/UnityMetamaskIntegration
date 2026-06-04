using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nethereum.Web3;
using Nethereum.Util;

/// <summary>
/// Panel "Enviar". Soporta ambos flujos:
///   Flujo 1 — escanea QR de dirección → usuario ingresa monto → envía.
///   Flujo 2 — escanea QR EIP-681 con monto → monto pre-cargado → usuario confirma y envía.
/// </summary>
public class SendPanel : MonoBehaviour
{
    [Header("Referencias de escena")]
    [SerializeField] private QRScannerUI scanner;

    [Header("Botones")]
    [SerializeField] private Button scanButton;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button closeButton;

    [Header("Campos")]
    [SerializeField] private TMP_InputField toAddressInput;
    [SerializeField] private TMP_InputField amountInput;

    [Header("Estado")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text txHashText;

    [Header("Panel")]
    [SerializeField] private GameObject panel;

    private void Awake()
    {
        scanButton.onClick.AddListener(StartScan);
        sendButton.onClick.AddListener(() => _ = SendTransaction());
        closeButton.onClick.AddListener(Hide);
        panel.SetActive(false);
    }

    public void Show()
    {
        panel.SetActive(true);
        ClearFields();
    }

    public void Hide()
    {
        scanner.StopScanning();
        panel.SetActive(false);
    }

    private void ClearFields()
    {
        toAddressInput.text = string.Empty;
        amountInput.text = string.Empty;
        statusText.text = string.Empty;
        txHashText.text = string.Empty;
        SetInteractable(true);
    }

    // ── Escáner ──────────────────────────────────────────────────────────────

    private void StartScan()
    {
        statusText.text = "Apunta la cámara al QR...";
        scanner.StartScanning(OnQRDecoded);
    }

    private void OnQRDecoded(string raw)
    {
        if (!EIP681.TryParse(raw, out string address, out decimal? etherAmount))
        {
            statusText.text = "QR no reconocido. Intenta de nuevo.";
            return;
        }

        toAddressInput.text = address;

        if (etherAmount.HasValue)
        {
            // Flujo 2: monto fijo desde la solicitud
            amountInput.text = etherAmount.Value.ToString("G");
            amountInput.interactable = false;
            statusText.text = $"Solicitud de {etherAmount.Value} ETH escaneada.";
        }
        else
        {
            // Flujo 1: solo dirección, usuario elige monto
            amountInput.interactable = true;
            statusText.text = "Dirección cargada. Ingresa el monto a enviar.";
        }
    }

    // ── Transferencia ─────────────────────────────────────────────────────────

    private async Task SendTransaction()
    {
        if (!SimpleWalletLogin.Instance.IsLoggedIn)
        {
            statusText.text = "Debes iniciar sesión primero.";
            return;
        }

        string toAddress = toAddressInput.text.Trim();
        if (string.IsNullOrEmpty(toAddress))
        {
            statusText.text = "Escanea un QR o ingresa una dirección destino.";
            return;
        }

        string rawAmount = amountInput.text.Trim().Replace(",", ".");
        if (!decimal.TryParse(rawAmount, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal etherAmount) || etherAmount <= 0)
        {
            statusText.text = "Monto inválido (ej: 0.01)";
            return;
        }

        SetInteractable(false);
        statusText.text = "Enviando transacción...";
        txHashText.text = string.Empty;

        try
        {
            var web3 = SimpleWalletLogin.Instance.Web3;
            var receipt = await web3.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(toAddress, etherAmount);

            statusText.text = "✓ Transferencia confirmada";
            txHashText.text = $"Tx: {receipt.TransactionHash}";
        }
        catch (Exception ex)
        {
            statusText.text = "Error: " + ex.Message;
            Debug.LogError("SendPanel error: " + ex);
        }
        finally
        {
            SetInteractable(true);
        }
    }

    private void SetInteractable(bool value)
    {
        scanButton.interactable = value;
        sendButton.interactable = value;
        toAddressInput.interactable = value;
        // amountInput interactivity managed separately (locked when EIP-681 has amount)
    }
}
