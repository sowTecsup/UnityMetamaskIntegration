using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class QRSceneSetup
{
    [MenuItem("Tools/Setup QR UI Scene")]
    static void SetupScene()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1;
        }

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem));
        }

        // ── Scanner Panel ──────────────────────────────────────────────
        GameObject scannerPanel = GetOrCreatePanel(canvas, "ScannerPanel");
        EnsureComponent<Image>(scannerPanel).color = new Color(0, 0, 0, 0.9f);
        RawImage cameraPreview = GetOrCreateRawImage(scannerPanel, "CameraPreview");
        SetFullStretch(cameraPreview.rectTransform);
        QRScannerUI scannerUI = EnsureComponent<QRScannerUI>(scannerPanel);
        SetField(scannerUI, "cameraPreview", cameraPreview);
        SetField(scannerUI, "scannerPanel", scannerPanel);
        scannerPanel.SetActive(false);
        Debug.Log("ScannerPanel listo.");

        // ── Send Panel ─────────────────────────────────────────────────
        GameObject sendPanel = GetOrCreatePanel(canvas, "SendPanel");
        EnsureComponent<Image>(sendPanel).color = new Color(1, 1, 1, 0.95f);
        SendPanel send = EnsureComponent<SendPanel>(sendPanel);
        SetField(send, "scanner", scannerUI);

        TMP_InputField toInput = GetOrCreateInputField(sendPanel, "ToAddressInput", "Direcci\u00f3n destino (0x...)");
        TMP_InputField amtInput = GetOrCreateInputField(sendPanel, "AmountInput", "Monto en ETH");
        Button scanBtn = GetOrCreateButton(sendPanel, "ScanButton", "Escanear QR");
        Button sendBtn = GetOrCreateButton(sendPanel, "SendButton", "Enviar");
        Button closeBtn = GetOrCreateButton(sendPanel, "CloseSendButton", "Cerrar");
        TMP_Text statusTxt = GetOrCreateLabel(sendPanel, "SendStatusText", "");
        TMP_Text txHashTxt = GetOrCreateLabel(sendPanel, "TxHashText", "");

        SetField(send, "scanButton", scanBtn);
        SetField(send, "sendButton", sendBtn);
        SetField(send, "closeButton", closeBtn);
        SetField(send, "toAddressInput", toInput);
        SetField(send, "amountInput", amtInput);
        SetField(send, "statusText", statusTxt);
        SetField(send, "txHashText", txHashTxt);
        SetField(send, "panel", sendPanel);

        RectTransform sendRt = sendPanel.GetComponent<RectTransform>();
        sendRt.anchorMin = new Vector2(0.5f, 0.5f);
        sendRt.anchorMax = new Vector2(0.5f, 0.5f);
        sendRt.sizeDelta = new Vector2(600, 500);
        sendRt.anchoredPosition = Vector2.zero;
        sendPanel.SetActive(false);
        Debug.Log("SendPanel listo.");

        // ── Receive Panel ──────────────────────────────────────────────
        GameObject recvPanel = GetOrCreatePanel(canvas, "ReceivePanel");
        EnsureComponent<Image>(recvPanel).color = new Color(1, 1, 1, 0.95f);
        ReceivePanel recv = EnsureComponent<ReceivePanel>(recvPanel);

        RawImage addrQR = GetOrCreateRawImage(recvPanel, "AddressQRImage");
        TMP_Text addrLabel = GetOrCreateLabel(recvPanel, "AddressLabel", "0x...");
        TMP_InputField recvAmtInput = GetOrCreateInputField(recvPanel, "RequestAmountInput", "Monto en ETH");
        Button genBtn = GetOrCreateButton(recvPanel, "GenerateRequestButton", "Generar QR con monto");
        RawImage reqQR = GetOrCreateRawImage(recvPanel, "RequestQRImage");
        TMP_Text reqUriLabel = GetOrCreateLabel(recvPanel, "RequestUriLabel", "");

        SetField(recv, "addressQRImage", addrQR);
        SetField(recv, "addressLabel", addrLabel);
        SetField(recv, "amountInput", recvAmtInput);
        SetField(recv, "generateRequestButton", genBtn);
        SetField(recv, "requestQRImage", reqQR);
        SetField(recv, "requestUriLabel", reqUriLabel);
        SetField(recv, "panel", recvPanel);

        RectTransform recvRt = recvPanel.GetComponent<RectTransform>();
        recvRt.anchorMin = new Vector2(0.5f, 0.5f);
        recvRt.anchorMax = new Vector2(0.5f, 0.5f);
        recvRt.sizeDelta = new Vector2(600, 700);
        recvRt.anchoredPosition = Vector2.zero;
        recvPanel.SetActive(false);
        Debug.Log("ReceivePanel listo.");

        // ── Botones "Enviar" y "Recibir" en el Canvas principal ────────
        Button openSendBtn = GetOrCreateCanvasButton(canvas, "OpenSendButton", "Enviar ETH",
            new Vector2(-110, -250), new Vector2(180, 40));
        Button openRecvBtn = GetOrCreateCanvasButton(canvas, "OpenReceiveButton", "Recibir ETH",
            new Vector2(110, -250), new Vector2(180, 40));

        EditorUtility.SetDirty(canvas.gameObject);
        Debug.Log("\\n=== QR UI Setup completado ===");
        Debug.Log("IMPORTANTE: Asigna los botones 'Enviar ETH' y 'Recibir ETH' en el Inspector del WalletManager");
        Debug.Log("o conecta manualmente send.Show() y recv.Show() a esos botones desde el c\u00f3digo.");
    }

    // ── Helpers ────────────────────────────────────────────────────────

    static GameObject GetOrCreatePanel(Canvas canvas, string name)
    {
        Transform t = FindChild(canvas.transform, name);
        if (t != null) return t.gameObject;

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        return go;
    }

    static RawImage GetOrCreateRawImage(GameObject parent, string name)
    {
        Transform t = FindChild(parent.transform, name);
        if (t != null) return t.GetComponent<RawImage>();

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        RawImage ri = go.AddComponent<RawImage>();
        ri.raycastTarget = false;
        return ri;
    }

    static TMP_Text GetOrCreateLabel(GameObject parent, string name, string text)
    {
        Transform t = FindChild(parent.transform, name);
        if (t != null) return t.GetComponent<TMP_Text>();

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.color = Color.black;
        tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
        tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
        go.AddComponent<CanvasRenderer>();
        return tmp;
    }

    static TMP_InputField GetOrCreateInputField(GameObject parent, string name, string placeholder)
    {
        Transform t = FindChild(parent.transform, name);
        if (t != null) return t.GetComponent<TMP_InputField>();

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        go.layer = 5;

        Image bg = go.AddComponent<Image>();
        bg.color = new Color(1, 1, 1, 1);
        bg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
        bg.type = Image.Type.Sliced;

        GameObject textArea = new GameObject("TextArea", typeof(RectTransform));
        textArea.transform.SetParent(go.transform, false);
        RectTransform taRt = textArea.GetComponent<RectTransform>();
        SetFullStretch(taRt);
        taRt.sizeDelta = new Vector2(-12, -8);

        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(textArea.transform, false);
        TMP_Text textTmp = textGO.AddComponent<TextMeshProUGUI>();
        textTmp.text = "";
        textTmp.fontSize = 24;
        textTmp.color = Color.black;
        textTmp.horizontalAlignment = HorizontalAlignmentOptions.Left;
        textTmp.verticalAlignment = VerticalAlignmentOptions.Middle;
        textGO.AddComponent<CanvasRenderer>();
        SetFullStretch(textGO.GetComponent<RectTransform>());

        GameObject placeholderGO = new GameObject("Placeholder", typeof(RectTransform));
        placeholderGO.transform.SetParent(textArea.transform, false);
        TMP_Text phTmp = placeholderGO.AddComponent<TextMeshProUGUI>();
        phTmp.text = placeholder;
        phTmp.fontSize = 24;
        phTmp.color = new Color(0.5f, 0.5f, 0.5f, 1);
        phTmp.horizontalAlignment = HorizontalAlignmentOptions.Left;
        phTmp.verticalAlignment = VerticalAlignmentOptions.Middle;
        placeholderGO.AddComponent<CanvasRenderer>();
        SetFullStretch(placeholderGO.GetComponent<RectTransform>());

        TMP_InputField input = go.AddComponent<TMP_InputField>();
        input.textViewport = taRt;
        input.textComponent = textTmp;
        input.placeholder = phTmp;

        return input;
    }

    static Button GetOrCreateButton(GameObject parent, string name, string label)
    {
        Transform t = FindChild(parent.transform, name);
        if (t != null) return t.GetComponent<Button>();

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        go.layer = 5;

        Image img = go.AddComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(go.transform, false);
        TMP_Text labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = 24;
        labelTmp.color = Color.white;
        labelTmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
        labelTmp.verticalAlignment = VerticalAlignmentOptions.Middle;
        labelGO.AddComponent<CanvasRenderer>();
        SetFullStretch(labelGO.GetComponent<RectTransform>());

        return btn;
    }

    static Button GetOrCreateCanvasButton(Canvas canvas, string name, string label, Vector2 pos, Vector2 size)
    {
        Transform t = FindChild(canvas.transform, name);
        if (t != null) return t.GetComponent<Button>();

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        go.layer = 5;

        Image img = go.AddComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(go.transform, false);
        TMP_Text labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = 24;
        labelTmp.color = Color.white;
        labelTmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
        labelTmp.verticalAlignment = VerticalAlignmentOptions.Middle;
        labelGO.AddComponent<CanvasRenderer>();
        SetFullStretch(labelGO.GetComponent<RectTransform>());

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        return btn;
    }

    static void SetFullStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    static Transform FindChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
            if (child.name == name) return child;
        return null;
    }

    static T EnsureComponent<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null) comp = go.AddComponent<T>();
        return comp;
    }

    static void SetField<T>(object obj, string fieldName, T value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (field != null)
        {
            field.SetValue(obj, value);
            var mb = obj as MonoBehaviour;
            if (mb != null) EditorUtility.SetDirty(mb);
        }
        else
        {
            Debug.LogWarning($"Campo '{fieldName}' no encontrado en {obj.GetType().Name}");
        }
    }
}
