using UnityEngine;
using UnityEngine.InputSystem;

// Canasta del jugador: se mueve con el mouse, con el dedo o con las flechas / A-D.
public class PlayerCatcher : MonoBehaviour
{
    [SerializeField]
    private float _maxSpeed = 20f;
    [SerializeField]
    private float _keyboardSpeed = 11f;

    private Vector2 _size;
    private float _minX;
    private float _maxX;
    private float _targetX;
    private Camera _camera;

    public void Setup(Vector2 size, float y, float minX, float maxX)
    {
        _camera = Camera.main;
        _size = size;
        _minX = minX + size.x / 2f;
        _maxX = maxX - size.x / 2f;
        _targetX = (minX + maxX) / 2f;

        transform.localScale = new Vector3(size.x, size.y, 1f);
        transform.position = new Vector3(_targetX, y, 0f);
    }

    void Update()
    {
        if (CatchGameManager.Instance == null || !CatchGameManager.Instance.IsPlaying) return;

        ReadPointer();
        ReadKeyboard();

        Vector3 position = transform.position;
        _targetX = Mathf.Clamp(_targetX, _minX, _maxX);
        position.x = Mathf.MoveTowards(position.x, _targetX, _maxSpeed * Time.deltaTime);
        transform.position = position;
    }

    private void ReadPointer()
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
        {
            _targetX = ScreenToWorldX(touchscreen.primaryTouch.position.ReadValue());
            return;
        }

        // El mouse solo manda cuando se mueve, así no pelea con el teclado.
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.delta.ReadValue().sqrMagnitude > 0.01f)
        {
            _targetX = ScreenToWorldX(mouse.position.ReadValue());
        }
    }

    private void ReadKeyboard()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        float direction = 0f;
        if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) direction -= 1f;
        if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) direction += 1f;

        if (direction != 0f)
        {
            _targetX = transform.position.x + direction * _keyboardSpeed * Time.deltaTime;
        }
    }

    private float ScreenToWorldX(Vector2 screenPosition)
    {
        if (_camera == null) _camera = Camera.main;
        return _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -_camera.transform.position.z)).x;
    }

    public bool Overlaps(Vector3 point, float radius)
    {
        Vector3 position = transform.position;
        return Mathf.Abs(point.x - position.x) <= _size.x / 2f + radius
            && Mathf.Abs(point.y - position.y) <= _size.y / 2f + radius;
    }
}
