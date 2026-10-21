using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameSpringFestival : UIBasePanel
{
    private const int LuckyPouchItemId = 3102;
    private const int RedEnvelopeItemId = 3103;

    private static readonly string[] ItemSpritePaths =
    {
        "Assets/Art/LittleGame/SpriteAtlas.spriteatlasv2[firecrackers]",
        "Assets/Art/LittleGame/SpriteAtlas.spriteatlasv2[lucky_pouch]",
        "Assets/Art/LittleGame/SpriteAtlas.spriteatlasv2[red_envelope]"
    };

    [SerializeField] private Transform _generator;
    [SerializeField] private Transform _basket;
    [SerializeField] private Transform _basketCollider;
    [SerializeField] private GameObject _game;
    [SerializeField] private GameObject _goStart;
    [SerializeField] private Button _btnStart;
    [SerializeField] private Button _btnClose;
    [SerializeField] private Text _txtPouchNum;
    [SerializeField] private Text _txtRedEnvelopeMNum;
    [SerializeField] private Text _txtTime;
    [SerializeField, Min(1f)] private float _gameDuration = 30f;
    [SerializeField, Min(0.1f)] private float _spawnInterval = 0.85f;
    [SerializeField, Min(1f)] private float _itemFallSpeedMin = 120f;
    [SerializeField, Min(1f)] private float _itemFallSpeedMax = 420f;
    [SerializeField, Min(0f)] private float _firecrackerStunDuration = 3f;
    [SerializeField] private float _despawnBottomOffset = 120f;

    private readonly List<GameSpringFestivalItem> _activeItems = new List<GameSpringFestivalItem>();
    private readonly Sprite[] _itemSprites = new Sprite[ItemSpritePaths.Length];

    private RectTransform _generatorRect;
    private RectTransform _gameAreaRect;
    private RectTransform _basketRect;
    private RectTransform _basketColliderRect;
    private GameSpringFestivalBasket _basketDrag;
    private string _resourceTag;
    private bool _isGameStarted;
    private bool _isGameEnded;
    private bool _isStunned;
    private int _pouchCount;
    private int _envelopeCount;
    private float _remainingTime;
    private Coroutine _countdownCoroutine;
    private Coroutine _spawnCoroutine;
    private Coroutine _stunCoroutine;
    private int _spawnSerial;

    private void Awake()
    {
        _resourceTag = GetInstanceID().ToString();
        _generatorRect = _generator as RectTransform;
        _gameAreaRect = _game != null ? _game.transform as RectTransform : null;
        _basketRect = _basket as RectTransform;
        _basketColliderRect = _basketCollider as RectTransform;
        if (_basketColliderRect == null && _basketCollider != null)
        {
            Debug.LogError("GameSpringFestival 的 _basketCollider 需要 RectTransform。", this);
        }

        if (_basket != null)
        {
            _basketDrag = _basket.GetComponent<GameSpringFestivalBasket>();
            if (_basketDrag == null)
            {
                _basketDrag = _basket.gameObject.AddComponent<GameSpringFestivalBasket>();
            }

            _basketDrag.Configure(_gameAreaRect, () => _isGameStarted && !_isGameEnded && !_isStunned);
        }

        if (_btnStart != null)
        {
            _btnStart.onClick.AddListener(OnStartButtonClicked);
        }

        if (_btnClose != null)
        {
            _btnClose.onClick.AddListener(ClosePanel);
        }
    }

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);
        ResetGameState();
        LoadSpritesAsync();
    }

    protected override void HideHandle()
    {
        base.HideHandle();
        StopGameLoops();
        ClearActiveItems();
    }

    protected override void OnDestroy()
    {
        if (_btnStart != null)
        {
            _btnStart.onClick.RemoveListener(OnStartButtonClicked);
        }

        if (_btnClose != null)
        {
            _btnClose.onClick.RemoveListener(ClosePanel);
        }

        base.OnDestroy();
    }

    private void Update()
    {
        if (!_isGameStarted || _isGameEnded)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        for (int i = _activeItems.Count - 1; i >= 0; i--)
        {
            GameSpringFestivalItem item = _activeItems[i];
            if (item == null || !item.IsActive)
            {
                _activeItems.RemoveAt(i);
                continue;
            }

            item.Tick(deltaTime);
            if (TryPickupItem(item))
            {
                _activeItems.RemoveAt(i);
                continue;
            }

            if (IsBelowDespawnLine(item))
            {
                RecycleItem(item);
                _activeItems.RemoveAt(i);
            }
        }
    }

    public override string GetPanelName()
    {
        return GlobalDefine.GameSpringFestival;
    }

    private async void LoadSpritesAsync()
    {
        for (int i = 0; i < ItemSpritePaths.Length; i++)
        {
            _itemSprites[i] = await GameManager.Resource.LoadResource<Sprite>(
                ItemSpritePaths[i],
                _resourceTag);
            if (_itemSprites[i] == null)
            {
                Debug.LogError($"春节小游戏图标加载失败：{ItemSpritePaths[i]}", this);
            }
        }
    }

    private void ResetGameState()
    {
        StopGameLoops();
        ClearActiveItems();
        _isGameStarted = false;
        _isGameEnded = false;
        _isStunned = false;
        _pouchCount = 0;
        _envelopeCount = 0;
        _remainingTime = _gameDuration;
        RefreshCountTexts();
        RefreshTimeText(_remainingTime);
        SetGameplayVisible(false);
        SetStartUiVisible(true);
    }

    private void OnStartButtonClicked()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        if (_isGameStarted || _isGameEnded)
        {
            return;
        }

        StartGame();
    }

    private void StartGame()
    {
        SetStartUiVisible(false);
        SetGameplayVisible(true);
        _isGameStarted = true;
        _remainingTime = _gameDuration;
        RefreshTimeText(_remainingTime);
        BringBasketToFront();
        _countdownCoroutine = StartCoroutine(CountdownCoroutine());
        _spawnCoroutine = StartCoroutine(SpawnItemsCoroutine());
    }

    private void BringBasketToFront()
    {
        if (_basketRect != null)
        {
            _basketRect.SetAsLastSibling();
        }
    }

    private IEnumerator CountdownCoroutine()
    {
        while (_remainingTime > 0f && !_isGameEnded)
        {
            RefreshTimeText(_remainingTime);
            yield return null;
            _remainingTime -= Time.deltaTime;
        }

        if (!_isGameEnded)
        {
            EndGameAndGrantRewards();
        }
    }

    private IEnumerator SpawnItemsCoroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(_spawnInterval);
        while (_isGameStarted && !_isGameEnded)
        {
            SpawnItem();
            yield return wait;
        }
    }

    private async void SpawnItem()
    {
        if (!_isGameStarted || _isGameEnded || _gameAreaRect == null)
        {
            return;
        }

        GameObject itemObject = await UnityObjectPoolFactory.GetInstance()
            .GetItem<GameObject>(GlobalDefine.GameSpringFestivalItem, _resourceTag);
        if (!_isGameStarted || _isGameEnded || itemObject == null)
        {
            if (itemObject != null)
            {
                UnityObjectPoolFactory.GetInstance()
                    .PutItem(GlobalDefine.GameSpringFestivalItem, itemObject);
            }

            return;
        }

        itemObject.transform.SetParent(_gameAreaRect, false);
        GameSpringFestivalItem item = itemObject.GetComponent<GameSpringFestivalItem>();
        if (item == null)
        {
            Debug.LogError("GameSpringFestivalItem 预制体缺少组件。", this);
            UnityObjectPoolFactory.GetInstance()
                .PutItem(GlobalDefine.GameSpringFestivalItem, itemObject);
            return;
        }

        GameSpringFestivalItemKind kind = PickRandomItemKind();
        Sprite sprite = _itemSprites[(int)kind];
        float fallSpeedMin = Mathf.Min(_itemFallSpeedMin, _itemFallSpeedMax);
        float fallSpeedMax = Mathf.Max(_itemFallSpeedMin, _itemFallSpeedMax);
        float fallSpeed = TurnRandom.Range(
            $"GameSpringFestival.FallSpeed.{_spawnSerial}",
            fallSpeedMin,
            fallSpeedMax);
        item.Configure(kind, sprite, fallSpeed);
        item.RectTransform.anchoredPosition = GetRandomSpawnAnchoredPosition();
        item.RectTransform.localRotation = Quaternion.identity;
        _activeItems.Add(item);
        BringBasketToFront();
    }

    private GameSpringFestivalItemKind PickRandomItemKind()
    {
        int roll = TurnRandom.Range(
            $"GameSpringFestival.Spawn.{_spawnSerial++}",
            0,
            ItemSpritePaths.Length);
        return (GameSpringFestivalItemKind)roll;
    }

    private Vector2 GetRandomSpawnAnchoredPosition()
    {
        if (_generatorRect == null || _gameAreaRect == null)
        {
            return Vector2.zero;
        }

        float halfWidth = Mathf.Max(0f, _generatorRect.rect.width * 0.5f - 40f);
        float localX = TurnRandom.Range(
            $"GameSpringFestival.SpawnX.{_spawnSerial}",
            -halfWidth,
            halfWidth);
        Vector3 spawnWorld = _generatorRect.TransformPoint(new Vector3(localX, 0f, 0f));
        Vector3 spawnLocal = _gameAreaRect.InverseTransformPoint(spawnWorld);
        return new Vector2(spawnLocal.x, spawnLocal.y);
    }

    private bool TryPickupItem(GameSpringFestivalItem item)
    {
        RectTransform pickupCollider = _basketColliderRect != null
            ? _basketColliderRect
            : _basketRect;
        if (item == null || pickupCollider == null || item.RectTransform == null)
        {
            return false;
        }

        if (!TryGetWorldRectOverlap(item.RectTransform, pickupCollider))
        {
            return false;
        }

        switch (item.Kind)
        {
            case GameSpringFestivalItemKind.LuckyPouch:
                _pouchCount++;
                RefreshCountTexts();
                break;
            case GameSpringFestivalItemKind.RedEnvelope:
                _envelopeCount++;
                RefreshCountTexts();
                break;
            case GameSpringFestivalItemKind.Firecrackers:
                BeginFirecrackerStun();
                break;
        }

        RecycleItem(item);
        return true;
    }

    private static float GetWorldCornerY(RectTransform rectTransform, int cornerIndex)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        return corners[cornerIndex].y;
    }

    private static bool TryGetWorldRectOverlap(RectTransform first, RectTransform second)
    {
        Vector3[] firstCorners = new Vector3[4];
        Vector3[] secondCorners = new Vector3[4];
        first.GetWorldCorners(firstCorners);
        second.GetWorldCorners(secondCorners);

        float firstMinX = firstCorners[0].x;
        float firstMaxX = firstCorners[2].x;
        float firstMinY = firstCorners[0].y;
        float firstMaxY = firstCorners[1].y;
        float secondMinX = secondCorners[0].x;
        float secondMaxX = secondCorners[2].x;
        float secondMinY = secondCorners[0].y;
        float secondMaxY = secondCorners[1].y;

        bool overlapsX = firstMaxX > secondMinX && firstMinX < secondMaxX;
        bool overlapsY = firstMaxY > secondMinY && firstMinY < secondMaxY;
        return overlapsX && overlapsY;
    }

    private bool IsBelowDespawnLine(GameSpringFestivalItem item)
    {
        if (_gameAreaRect == null || item.RectTransform == null)
        {
            return true;
        }

        float itemBottomY = GetWorldCornerY(item.RectTransform, 0);
        float areaBottomY = GetWorldCornerY(_gameAreaRect, 0);
        return itemBottomY < areaBottomY - _despawnBottomOffset;
    }

    private void BeginFirecrackerStun()
    {
        if (_stunCoroutine != null)
        {
            StopCoroutine(_stunCoroutine);
        }

        _stunCoroutine = StartCoroutine(FirecrackerStunCoroutine());
    }

    private IEnumerator FirecrackerStunCoroutine()
    {
        _isStunned = true;
        yield return new WaitForSeconds(_firecrackerStunDuration);
        _isStunned = false;
        _stunCoroutine = null;
    }

    private void EndGameAndGrantRewards()
    {
        _isGameEnded = true;
        StopGameLoops();
        ClearActiveItems();

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        if (_pouchCount > 0)
        {
            playerInfoManager.AddItem(LuckyPouchItemId, _pouchCount);
        }

        if (_envelopeCount > 0)
        {
            playerInfoManager.AddItem(RedEnvelopeItemId, _envelopeCount);
        }

        if (_pouchCount > 0 || _envelopeCount > 0)
        {
            List<CommonRewardItemData> rewards = new List<CommonRewardItemData>(2);
            if (_pouchCount > 0)
            {
                rewards.Add(new CommonRewardItemData
                {
                    itemId = LuckyPouchItemId,
                    itemCount = _pouchCount
                });
            }

            if (_envelopeCount > 0)
            {
                rewards.Add(new CommonRewardItemData
                {
                    itemId = RedEnvelopeItemId,
                    itemCount = _envelopeCount
                });
            }

            UIManager.GetInstance().QueueRewardPanel(new OpenUIParam
            {
                data = rewards,
                rewardsAlreadyGranted = true
            });
        }

        UIManager.GetInstance().ClosePanel(GetPanelName());
    }

    private void ClosePanel()
    {
        GameManager.Audio.Play(AudioDefine.SFXClose);
        if (_isGameEnded)
        {
            return;
        }

        _isGameEnded = true;
        StopGameLoops();
        ClearActiveItems();
        UIManager.GetInstance().ClosePanel(GetPanelName());
    }

    private void StopGameLoops()
    {
        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = null;
        }

        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }

        if (_stunCoroutine != null)
        {
            StopCoroutine(_stunCoroutine);
            _stunCoroutine = null;
        }

        _isStunned = false;
    }

    private void ClearActiveItems()
    {
        for (int i = 0; i < _activeItems.Count; i++)
        {
            RecycleItem(_activeItems[i]);
        }

        _activeItems.Clear();
    }

    private void RecycleItem(GameSpringFestivalItem item)
    {
        if (item == null)
        {
            return;
        }

        item.Deactivate();
        UnityObjectPoolFactory.GetInstance()
            .PutItem(GlobalDefine.GameSpringFestivalItem, item.gameObject);
    }

    private void SetGameplayVisible(bool visible)
    {
        if (_game != null)
        {
            _game.SetActive(visible);
        }
    }

    private void SetStartUiVisible(bool visible)
    {
        if (_btnStart != null)
        {
            _btnStart.gameObject.SetActive(visible);
        }

        if (_btnClose != null)
        {
            _btnClose.gameObject.SetActive(visible);
        }

        if (_goStart != null)
        {
            _goStart.SetActive(visible);
        }
    }

    private void RefreshCountTexts()
    {
        if (_txtPouchNum != null)
        {
            _txtPouchNum.text = _pouchCount.ToString();
        }

        if (_txtRedEnvelopeMNum != null)
        {
            _txtRedEnvelopeMNum.text = _envelopeCount.ToString();
        }
    }

    private void RefreshTimeText(float remainingSeconds)
    {
        if (_txtTime == null)
        {
            return;
        }

        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        _txtTime.text = $"{minutes:00}:{seconds:00}";
    }
}
