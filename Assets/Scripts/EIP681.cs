using System;
using System.Numerics;
using Nethereum.Util;

/// <summary>
/// Builds and parses EIP-681 ethereum: payment URIs.
/// Format: ethereum:ADDRESS@CHAIN_ID?value=WEI
/// </summary>
public static class EIP681
{
    // Sepolia testnet chain ID
    public const long SepoliaChainId = 11155111;

    /// <summary>
    /// Builds a simple address URI (flow 1: receiver shares address, sender picks amount).
    /// </summary>
    public static string BuildAddressUri(string address)
    {
        return $"ethereum:{address}@{SepoliaChainId}";
    }

    /// <summary>
    /// Builds a payment request URI with a fixed ETH amount (flow 2: receiver requests exact amount).
    /// </summary>
    public static string BuildRequestUri(string address, decimal etherAmount)
    {
        var wei = UnitConversion.Convert.ToWei(etherAmount);
        return $"ethereum:{address}@{SepoliaChainId}?value={wei}";
    }

    /// <summary>
    /// Parses an ethereum: URI. Returns false if the string is not a valid EIP-681 URI.
    /// etherAmount will be null when no ?value= parameter is present.
    /// </summary>
    public static bool TryParse(string uri, out string address, out decimal? etherAmount)
    {
        address = null;
        etherAmount = null;

        if (string.IsNullOrWhiteSpace(uri)) return false;

        // Accept both raw addresses (0x...) and ethereum: URIs
        if (uri.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && !uri.Contains("?"))
        {
            address = uri.Trim();
            return true;
        }

        if (!uri.StartsWith("ethereum:", StringComparison.OrdinalIgnoreCase)) return false;

        // Strip scheme
        string rest = uri.Substring("ethereum:".Length);

        // Split path and query
        string path = rest;
        string query = string.Empty;
        int queryStart = rest.IndexOf('?');
        if (queryStart >= 0)
        {
            path = rest.Substring(0, queryStart);
            query = rest.Substring(queryStart + 1);
        }

        // Strip optional @chainId from path
        int atSign = path.IndexOf('@');
        address = atSign >= 0 ? path.Substring(0, atSign) : path;
        address = address.Trim();

        if (string.IsNullOrEmpty(address)) return false;

        // Parse ?value=WEI
        if (!string.IsNullOrEmpty(query))
        {
            foreach (var param in query.Split('&'))
            {
                int eq = param.IndexOf('=');
                if (eq < 0) continue;
                string key = param.Substring(0, eq).Trim();
                string val = param.Substring(eq + 1).Trim();
                if (key.Equals("value", StringComparison.OrdinalIgnoreCase))
                {
                    if (BigInteger.TryParse(val, out var wei))
                        etherAmount = UnitConversion.Convert.FromWei(wei);
                }
            }
        }

        return true;
    }
}
