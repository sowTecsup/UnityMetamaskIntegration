using TMPro;
using UnityEngine;

public class TokenRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text symbolText;
    [SerializeField] private TMP_Text balanceText;

    public void SetData(string tokenName, string symbol, string balance)
    {
        nameText.text = tokenName;
        symbolText.text = symbol;
        balanceText.text = balance;
    }
}

