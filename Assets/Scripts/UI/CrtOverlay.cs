using UnityEngine;
using UnityEngine.UI;

// Líneas de barrido tipo monitor CRT sobre toda la pantalla.
[RequireComponent(typeof(RawImage))]
public class CrtOverlay : MonoBehaviour
{
    [SerializeField]
    private int _lineSpacing = 4;
    [SerializeField, Range(0f, 1f)]
    private float _lineAlpha = 0.18f;

    private RawImage _image;

    void Awake()
    {
        _image = GetComponent<RawImage>();
        _image.raycastTarget = false;

        var texture = new Texture2D(1, _lineSpacing, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };
        for (int y = 0; y < _lineSpacing; y++)
        {
            texture.SetPixel(0, y, new Color(0f, 0f, 0f, y == 0 ? _lineAlpha : 0f));
        }
        texture.Apply();
        _image.texture = texture;
        _image.color = Color.white;
    }

    void Update()
    {
        // Repite la textura según la altura real de la pantalla para que cada línea mida lo mismo.
        _image.uvRect = new Rect(0f, 0f, 1f, Screen.height / (float)_lineSpacing);
    }
}
