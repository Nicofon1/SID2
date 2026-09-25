using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Opción de menú estilo terminal: texto verde que se invierte (fondo verde, texto negro)
// al pasar el mouse o al seleccionarla con el teclado.
public class TerminalButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField]
    private Image _background;
    [SerializeField]
    private TMP_Text _label;

    private bool _hovered;
    private bool _selected;

    void OnEnable()
    {
        _hovered = false;
        _selected = false;
        Refresh();
    }

    public void OnPointerEnter(PointerEventData eventData) { _hovered = true; Refresh(); }
    public void OnPointerExit(PointerEventData eventData) { _hovered = false; Refresh(); }
    public void OnSelect(BaseEventData eventData) { _selected = true; Refresh(); }
    public void OnDeselect(BaseEventData eventData) { _selected = false; Refresh(); }

    private void Refresh()
    {
        bool active = _hovered || _selected;
        if (_background != null) _background.color = active ? TerminalTheme.Green : Color.clear;
        if (_label != null) _label.color = active ? TerminalTheme.Background : TerminalTheme.Green;
    }
}
