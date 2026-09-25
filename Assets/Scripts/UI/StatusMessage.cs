using TMPro;
using UnityEngine;

// Texto global para mostrar mensajes de estado y errores al usuario.
public class StatusMessage : MonoBehaviour
{
    private static StatusMessage _instance;

    [SerializeField]
    private TMP_Text _label;

    [SerializeField]
    private Color _infoColor = TerminalTheme.Green;
    [SerializeField]
    private Color _errorColor = TerminalTheme.Error;

    private void Reset()
    {
        _label = GetComponent<TMP_Text>();
    }

    void Awake()
    {
        _instance = this;
        _label.text = "";
    }

    public static void Show(string message, bool isError = false)
    {
        if (isError) Debug.LogWarning(message); else Debug.Log(message);

        if (_instance == null || _instance._label == null) return;
        _instance._label.color = isError ? _instance._errorColor : _instance._infoColor;
        _instance._label.text = message.ToUpperInvariant();
    }

    public static void Clear()
    {
        if (_instance != null && _instance._label != null) _instance._label.text = "";
    }
}
