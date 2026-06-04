using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// MetaMask Embedded Wallets / Web3Auth
using Web3AuthSDK; // Ajusta este namespace al del paquete real instalado en tu proyecto

// Nethereum
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Util;
using Nethereum.StandardTokenEIP20;

public class SimpleWalletLogin : MonoBehaviour
{
    public static SimpleWalletLogin Instance { get; private set; }

    public Nethereum.Web3.Web3 Web3 => web3;
    public Account Account => account;
    public string WalletAddress => walletAddress;
    public bool IsLoggedIn => web3 != null && !string.IsNullOrEmpty(walletAddress);

    public event Action OnWalletStateChanged;

    [Header("Web3Auth / MetaMask Embedded Wallets")]
    [SerializeField] private string clientId = "TU_CLIENT_ID";
    [SerializeField] private string redirectScheme = "torusapp://com.torus.Web3AuthUnity/auth";

    [Header("Network")]
    [SerializeField] private string rpcUrl = "https://rpc.sepolia.org";
    [SerializeField] private string chainName = "Sepolia ETH";

    [Header("UI")]
    [SerializeField] private Button loginButton;
    [SerializeField] private Button logoutButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text addressText;
    [SerializeField] private Transform tokenListRoot;
    [SerializeField] private TokenRowUI tokenRowPrefab;

    private Web3Auth web3Auth;
    private Web3 web3;
    private Account account;
    private string walletAddress;

    [Serializable]
    public class TokenConfig
    {
        public string displayName;
        public string symbol;
        public string contractAddress;
    }

    [Header("Tracked Tokens")]
    [SerializeField]
    private List<TokenConfig> trackedTokens = new List<TokenConfig>()
    {
        new TokenConfig
        {
            displayName = "USD Coin",
            symbol = "USDC",
            contractAddress = "0x0000000000000000000000000000000000000000" // reemplazar
        },
        new TokenConfig
        {
            displayName = "Tether USD",
            symbol = "USDT",
            contractAddress = "0x0000000000000000000000000000000000000000" // reemplazar
        }
    };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        web3Auth = GetComponent<Web3Auth>();

        web3Auth.setOptions(new Web3AuthOptions()
        {
            clientId = clientId,
            network = Web3Auth.Network.SAPPHIRE_DEVNET, // para pruebas
            redirectUrl = new Uri(redirectScheme),
            sessionTime = 86400 // 1 d�a
        });

        web3Auth.onLogin += OnLogin;
        web3Auth.onLogout += OnLogout;

        loginButton.onClick.AddListener(LoginGoogle);
        logoutButton.onClick.AddListener(Logout);
        refreshButton.onClick.AddListener(() => _ = RefreshBalances());

        SetLoggedOutState();
    }

    public void LoginGoogle()
    {
        statusText.text = "Abriendo login...";

        var options = new LoginParams()
        {
            loginProvider = Provider.GOOGLE,
            curve = Curve.SECP256K1
        };

        web3Auth.login(options);
    }

    public void Logout()
    {
        web3Auth.logout();
    }

    private async void OnLogin(Web3AuthResponse response)
    {
        try
        {
            if (!string.IsNullOrEmpty(response.error))
            {
                statusText.text = "Login error: " + response.error;
                return;
            }

            string privateKey = response.privKey;

            if (string.IsNullOrEmpty(privateKey))
            {
                statusText.text = "No se recibi� private key EVM.";
                return;
            }

            account = new Account(privateKey);
            walletAddress = account.Address;

            web3 = new Web3(account, rpcUrl);

            addressText.text = walletAddress;
            statusText.text = $"Login correcto en {chainName}";

            OnWalletStateChanged?.Invoke();
            await RefreshBalances();
        }
        catch (Exception ex)
        {
            statusText.text = "Error post-login: " + ex.Message;
        }
    }

    private void OnLogout()
    {
        SetLoggedOutState();
        statusText.text = "Sesión cerrada";
        OnWalletStateChanged?.Invoke();
    }

    private void SetLoggedOutState()
    {
        account = null;
        web3 = null;
        walletAddress = string.Empty;

        addressText.text = "-";
        ClearTokenRows();
    }

    private async Task RefreshBalances()
    {
        if (web3 == null || string.IsNullOrEmpty(walletAddress))
        {
            statusText.text = "Primero inicia sesi�n.";
            return;
        }

        statusText.text = "Consultando balances...";
        ClearTokenRows();

        try
        {
            // Balance nativo
            var nativeBalanceWei = await web3.Eth.GetBalance.SendRequestAsync(walletAddress);
            decimal nativeBalance = UnitConversion.Convert.FromWei(nativeBalanceWei.Value);

            CreateRow(chainName, "ETH", nativeBalance.ToString("N6"));

            // ERC20
            foreach (var token in trackedTokens)
            {
                if (string.IsNullOrWhiteSpace(token.contractAddress) ||
                    token.contractAddress == "0x0000000000000000000000000000000000000000")
                {
                    continue;
                }

                try
                {
                    var erc20 = new StandardTokenService(web3, token.contractAddress);

                    BigInteger rawBalance = await erc20.BalanceOfQueryAsync(walletAddress);
                    string symbol = await erc20.SymbolQueryAsync();
                    byte decimals = await erc20.DecimalsQueryAsync();

                    decimal humanBalance = UnitConversion.Convert.FromWei(rawBalance, (int)decimals);

                    CreateRow(token.displayName, symbol, humanBalance.ToString("N6"));
                }
                catch (Exception tokenEx)
                {
                    CreateRow(token.displayName, token.symbol, "Error");
                    Debug.LogWarning($"Error consultando {token.displayName}: {tokenEx.Message}");
                }
            }

            statusText.text = "Balances actualizados";
        }
        catch (Exception ex)
        {
            statusText.text = "Error al consultar balances: " + ex.Message;
        }
    }

    private void CreateRow(string name, string symbol, string balance)
    {
        var row = Instantiate(tokenRowPrefab, tokenListRoot);
        row.SetData(name, symbol, balance);
    }

    private void ClearTokenRows()
    {
        for (int i = tokenListRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(tokenListRoot.GetChild(i).gameObject);
        }
    }
}