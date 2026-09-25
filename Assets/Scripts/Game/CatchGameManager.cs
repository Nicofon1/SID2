using System.Collections.Generic;
using TMPro;
using UnityEngine;

// "Atrapa Estrellas": atrapa las estrellas que caen, esquiva las bombas.
// Estrella = +10, estrella dorada = +50. Pierdes una vida si dejas caer una
// estrella o si atrapas una bomba. La velocidad aumenta con el tiempo.
public class CatchGameManager : MonoBehaviour
{
    public static CatchGameManager Instance { get; private set; }

    [Header("HUD")]
    [SerializeField]
    private TMP_Text _scoreLabel;
    [SerializeField]
    private TMP_Text _livesLabel;

    [Header("Game Over")]
    [SerializeField]
    private TMP_Text _finalScoreLabel;
    [SerializeField]
    private TMP_Text _recordLabel;

    [Header("Dificultad")]
    [SerializeField]
    private int _startingLives = 3;
    [SerializeField]
    private float _startSpawnInterval = 1.1f;
    [SerializeField]
    private float _minSpawnInterval = 0.35f;
    [SerializeField]
    private float _startFallSpeed = 3f;
    [SerializeField]
    private float _maxFallSpeed = 10f;
    [SerializeField]
    private float _secondsToMaxDifficulty = 90f;

    [Header("Puntos")]
    [SerializeField]
    private int _starPoints = 10;
    [SerializeField]
    private int _goldenStarPoints = 50;

    [Header("Colores")]
    [SerializeField]
    private Color _playerColor = TerminalTheme.Green;
    [SerializeField]
    private Color _starColor = TerminalTheme.Green;
    [SerializeField]
    private Color _goldenStarColor = TerminalTheme.PaleGreen;
    [SerializeField]
    private Color _bombColor = TerminalTheme.Error;

    private readonly List<FallingItem> _items = new List<FallingItem>();
    private int _score;
    private int _lives;
    private float _elapsed;
    private float _spawnTimer;
    private float _minX;
    private float _maxX;
    private float _topY;

    public bool IsPlaying { get; private set; }
    public PlayerCatcher Player { get; private set; }
    public float BottomY { get; private set; }

    private float Difficulty => Mathf.Clamp01(_elapsed / _secondsToMaxDifficulty);

    void Awake()
    {
        Instance = this;
    }

    public void StartGame()
    {
        ClearItems();
        ComputeBounds();
        EnsurePlayer();

        _score = 0;
        _lives = _startingLives;
        _elapsed = 0f;
        _spawnTimer = 0.6f;
        UpdateHud();

        IsPlaying = true;
        UIManager.Instance.Show(AppScreen.Game);
    }

    public void EndGame()
    {
        if (!IsPlaying) return;
        StopGame();

        UIManager.Instance.Show(AppScreen.GameOver);
        _finalScoreLabel.text = _score.ToString();
        _recordLabel.text = "GUARDANDO...";

        int finalScore = _score;
        ScoreService.SubmitScore(finalScore, (success, isNewRecord, best) =>
        {
            if (_recordLabel == null) return;
            if (!success)
            {
                _recordLabel.text = "NO SE PUDO GUARDAR";
            }
            else if (isNewRecord && finalScore > 0)
            {
                _recordLabel.text = "NUEVO RÉCORD";
            }
            else
            {
                _recordLabel.text = "RÉCORD : " + best;
            }
        });
    }

    public void StopGame()
    {
        IsPlaying = false;
        ClearItems();
        if (Player != null) Player.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!IsPlaying) return;

        _elapsed += Time.deltaTime;
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            SpawnItem();
            _spawnTimer = Mathf.Lerp(_startSpawnInterval, _minSpawnInterval, Difficulty);
        }
    }

    public void HandleCaught(FallingItem item)
    {
        switch (item.Type)
        {
            case FallingItemType.Star:
                _score += _starPoints;
                break;
            case FallingItemType.GoldenStar:
                _score += _goldenStarPoints;
                break;
            case FallingItemType.Bomb:
                _lives--;
                break;
        }
        RemoveItem(item);
        UpdateHud();
        if (_lives <= 0) EndGame();
    }

    public void HandleMissed(FallingItem item)
    {
        if (item.Type == FallingItemType.Star) _lives--;
        RemoveItem(item);
        UpdateHud();
        if (_lives <= 0) EndGame();
    }

    private void SpawnItem()
    {
        float bombChance = Mathf.Lerp(0.15f, 0.35f, Difficulty);
        float roll = Random.value;

        FallingItemType type;
        Color color;
        float radius;
        float speedMultiplier = 1f;

        if (roll < 0.07f)
        {
            type = FallingItemType.GoldenStar;
            color = _goldenStarColor;
            radius = 0.28f;
            speedMultiplier = 1.3f;
        }
        else if (roll < 0.07f + bombChance)
        {
            type = FallingItemType.Bomb;
            color = _bombColor;
            radius = 0.4f;
        }
        else
        {
            type = FallingItemType.Star;
            color = _starColor;
            radius = 0.35f;
        }

        var itemObject = new GameObject(type.ToString());
        itemObject.transform.SetParent(transform, false);
        itemObject.transform.position = new Vector3(Random.Range(_minX + radius, _maxX - radius), _topY + radius, 0f);

        var renderer = itemObject.AddComponent<SpriteRenderer>();
        renderer.sprite = type == FallingItemType.Bomb ? SpriteFactory.Circle : SpriteFactory.RoundedBox;
        renderer.color = color;
        renderer.sortingOrder = 10;

        float speed = Mathf.Lerp(_startFallSpeed, _maxFallSpeed, Difficulty) * speedMultiplier * Random.Range(0.85f, 1.15f);
        var item = itemObject.AddComponent<FallingItem>();
        item.Init(this, type, speed, radius);
        _items.Add(item);
    }

    private void RemoveItem(FallingItem item)
    {
        if (!_items.Remove(item)) return;
        Destroy(item.gameObject);
    }

    private void ClearItems()
    {
        foreach (FallingItem item in _items)
        {
            if (item != null) Destroy(item.gameObject);
        }
        _items.Clear();
    }

    private void ComputeBounds()
    {
        Camera cam = Camera.main;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        Vector3 center = cam.transform.position;

        _minX = center.x - halfWidth;
        _maxX = center.x + halfWidth;
        _topY = center.y + halfHeight;
        BottomY = center.y - halfHeight;
    }

    private void EnsurePlayer()
    {
        if (Player == null)
        {
            var playerObject = new GameObject("Player");
            playerObject.transform.SetParent(transform, false);

            var renderer = playerObject.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteFactory.RoundedBox;
            renderer.color = _playerColor;
            renderer.sortingOrder = 11;

            Player = playerObject.AddComponent<PlayerCatcher>();
        }

        Player.gameObject.SetActive(true);
        // Se deja espacio abajo para el pie de página con el nombre del autor.
        Player.Setup(new Vector2(1.9f, 0.45f), BottomY + 1.3f, _minX, _maxX);
    }

    private void UpdateHud()
    {
        _scoreLabel.text = "PUNTOS : " + _score.ToString("0000");
        _livesLabel.text = "VIDAS : " + Mathf.Max(_lives, 0);
    }
}
