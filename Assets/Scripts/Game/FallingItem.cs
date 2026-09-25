using UnityEngine;

public enum FallingItemType
{
    Star,
    GoldenStar,
    Bomb
}

// Objeto que cae desde arriba. Avisa al CatchGameManager si fue atrapado o si se perdió.
public class FallingItem : MonoBehaviour
{
    private CatchGameManager _game;
    private float _speed;
    private float _radius;
    private float _spin;

    public FallingItemType Type { get; private set; }

    public void Init(CatchGameManager game, FallingItemType type, float speed, float radius)
    {
        _game = game;
        Type = type;
        _speed = speed;
        _radius = radius;
        _spin = Random.Range(-180f, 180f);
        transform.localScale = Vector3.one * radius * 2f;
    }

    void Update()
    {
        if (_game == null || !_game.IsPlaying) return;

        transform.position += Vector3.down * _speed * Time.deltaTime;
        transform.Rotate(0f, 0f, _spin * Time.deltaTime);

        if (_game.Player != null && _game.Player.Overlaps(transform.position, _radius))
        {
            _game.HandleCaught(this);
        }
        else if (transform.position.y < _game.BottomY - _radius)
        {
            _game.HandleMissed(this);
        }
    }
}
