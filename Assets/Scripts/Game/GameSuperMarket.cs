using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameSuperMarket : UIBasePanel
{
    private const int ItemCount = 20;
    private const int TargetCount = 4;
    private const string FailMessage = "游戏失败";
    private const string SuccessTipMessage = "挑战成功";
    private const string StartPreparingMessage = "游戏准备中";
    private const string StartReadyMessage = "点击开启游戏";

    private static readonly string[] ItemSpritePaths =
    {
        "Assets/Art/LittleGame/Sprites/SuperMarket/01_milk.png",
        "Assets/Art/LittleGame/Sprites/SuperMarket/02_bread.png",
        "Assets/Art/LittleGame/Sprites/SuperMarket/03_eggs.png",
        "Assets/Art/LittleGame/Sprites/SuperMarket/04_apple.png",
        "Assets/Art/LittleGame/Sprites/SuperMarket/05_bananas.png",
        "Assets/Art/LittleGame/Sprites/SuperMarket/06_orange.png",
        "Assets/Art/LittleGame/Sprites/SuperMarket/07_carrots.png",
        "Assets/Art/LittleGame/Sprites/SuperMarket/08_dried_fish.png",
        "Assets/Art/LittleGame/Sprites/SuperMarket/09_mineral_water.png",
        "Assets/Art/LittleGame/Sprites/SuperMarket/10_juice.png",
        "Assets/Art/LittleGame/Sprites/SuperMarket/11_soda.png",
        "Assets/Art/LittleGame/Sprites/SuperMarket/12_instant_noodles.png",
        "Assets/Art/LittleGame/Sprites/SuperMarket/13_cookies.png",
        "Assets/Art/LittleGame/Sprites/SuperMarket/14_canned_food.png",
        "Assets/Art/LittleGame/Sprites/SuperMarket/15_tissues.png",
        "Assets/Art/LittleGame/Sprites/SuperMarket/16_toothpaste.png",
        "Assets/Art/LittleGame/Sprites/SuperMarket/17_soap.png",
        "Assets/Art/LittleGame/Sprites/SuperMarket/18_laundry_detergent.png",
        "Assets/Art/LittleGame/Sprites/SuperMarket/19_cat_food.png",
        "Assets/Art/LittleGame/Sprites/SuperMarket/20_chocolate.png"
    };

    [SerializeField] private Text _txtTime;
    [SerializeField] private Text _txtStart;
    [SerializeField] private Button _btnStart;
    [SerializeField] private Button _btnClose;
    [SerializeField] private Transform _itemParent;
    [SerializeField] private Transform _targetParent;
    [SerializeField] private GameObject _goStart;
    [SerializeField] private GameSuperMarketItem[] _marketItems;
    [SerializeField] private GameSuperMarketTarget[] _marketTargets;
    [SerializeField, Min(1f)] private float _gameDuration = 10f;
    [SerializeField, Min(0f)] private float _wrongPickPenaltyDuration = 3f;

    private readonly List<Vector2> _itemDefaultAnchoredPositions = new List<Vector2>();
    private readonly List<Vector2> _targetDefaultAnchoredPositions = new List<Vector2>();
    private readonly Sprite[] _itemSprites = new Sprite[ItemCount];
    private readonly HashSet<int> _foundTargetItemIndices = new HashSet<int>();

    private bool _isGameStarted;
    private bool _isGameEnded;
    private bool _isSearchOnCooldown;
    private bool _isPreparingRound;
    private bool _isRoundReady;
    private int _layoutSerial;
    private float _remainingTime;
    private string _resourceTag;
    private Coroutine _countdownCoroutine;
    private Coroutine _searchCooldownCoroutine;
    private Action<bool> _onGameCompleted;

    private void Awake()
    {
        _resourceTag = GetInstanceID().ToString();
        CacheDefaultAnchoredPositions();

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
        _onGameCompleted = null;
        if (param?.data is GameSuperMarketOpenParam openParam)
        {
            _onGameCompleted = openParam.onCompleted;
        }

        ResetGameState();
        PrepareRoundAsync();
    }

    protected override void HideHandle()
    {
        base.HideHandle();
        StopSearchCooldown();
        StopCountdown();
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

    public override string GetPanelName()
    {
        return GlobalDefine.GameSuperMarket;
    }

    private void CacheDefaultAnchoredPositions()
    {
        _itemDefaultAnchoredPositions.Clear();
        for (int i = 0; i < _marketItems.Length; i++)
        {
            RectTransform itemRect = _marketItems[i] != null ? _marketItems[i].RectTransform : null;
            _itemDefaultAnchoredPositions.Add(itemRect != null ? itemRect.anchoredPosition : Vector2.zero);
        }

        _targetDefaultAnchoredPositions.Clear();
        for (int i = 0; i < _marketTargets.Length; i++)
        {
            RectTransform targetRect = _marketTargets[i] != null ? _marketTargets[i].RectTransform : null;
            _targetDefaultAnchoredPositions.Add(targetRect != null ? targetRect.anchoredPosition : Vector2.zero);
        }
    }

    private async void PrepareRoundAsync()
    {
        if (_isPreparingRound)
        {
            return;
        }

        _isPreparingRound = true;
        SetRoundReady(false);
        try
        {
            if (!await LoadItemSpritesAsync())
            {
                return;
            }

            ApplyRandomRoundLayout();
            SetGameplayContentVisible(false);
            SetRoundReady(true);
        }
        finally
        {
            _isPreparingRound = false;
        }
    }

    private async System.Threading.Tasks.Task<bool> LoadItemSpritesAsync()
    {
        for (int i = 0; i < ItemSpritePaths.Length; i++)
        {
            Sprite sprite = await GameManager.Resource.LoadResource<Sprite>(ItemSpritePaths[i], _resourceTag);
            if (sprite == null)
            {
                Debug.LogError($"超市小游戏图标加载失败：{ItemSpritePaths[i]}", this);
                return false;
            }

            _itemSprites[i] = sprite;
        }

        return true;
    }

    private void ApplyRandomRoundLayout()
    {
        System.Random random = CreateLayoutRandom();
        int[] itemSpriteIndices = CreateShuffledIndices(ItemCount, random);
        int[] targetSpriteIndices = PickDistinctIndices(ItemCount, TargetCount, random);
        int[] itemPositionOrder = CreateShuffledIndices(ItemCount, random);
        int[] targetPositionOrder = CreateShuffledIndices(TargetCount, random);

        _foundTargetItemIndices.Clear();

        for (int i = 0; i < _marketItems.Length; i++)
        {
            GameSuperMarketItem marketItem = _marketItems[i];
            if (marketItem == null)
            {
                continue;
            }

            int spriteIndex = i < itemSpriteIndices.Length ? itemSpriteIndices[i] : 0;
            marketItem.Configure(spriteIndex, _itemSprites[spriteIndex], OnMarketItemClicked);

            RectTransform itemRect = marketItem.RectTransform;
            if (itemRect != null && i < itemPositionOrder.Length)
            {
                int positionIndex = itemPositionOrder[i];
                if (positionIndex >= 0 && positionIndex < _itemDefaultAnchoredPositions.Count)
                {
                    itemRect.anchoredPosition = _itemDefaultAnchoredPositions[positionIndex];
                }
            }

            marketItem.SetInteractable(false);
        }

        for (int i = 0; i < _marketTargets.Length; i++)
        {
            GameSuperMarketTarget marketTarget = _marketTargets[i];
            if (marketTarget == null)
            {
                continue;
            }

            int spriteIndex = i < targetSpriteIndices.Length ? targetSpriteIndices[i] : 0;
            marketTarget.Configure(spriteIndex, _itemSprites[spriteIndex]);
            marketTarget.ResetFoundState();

            RectTransform targetRect = marketTarget.RectTransform;
            if (targetRect != null && i < targetPositionOrder.Length)
            {
                int positionIndex = targetPositionOrder[i];
                if (positionIndex >= 0 && positionIndex < _targetDefaultAnchoredPositions.Count)
                {
                    targetRect.anchoredPosition = _targetDefaultAnchoredPositions[positionIndex];
                }
            }
        }
    }

    private void ResetGameState()
    {
        StopSearchCooldown();
        StopCountdown();

        _isGameStarted = false;
        _isGameEnded = false;
        _remainingTime = _gameDuration;
        _foundTargetItemIndices.Clear();

        RefreshTimeText(_remainingTime);
        SetGameplayContentVisible(false);
        SetStartUiVisible(true);
        SetRoundReady(false);
        SetAllItemButtonsInteractable(false);

        for (int i = 0; i < _marketTargets.Length; i++)
        {
            if (_marketTargets[i] != null)
            {
                _marketTargets[i].ResetFoundState();
            }
        }
    }

    private void OnStartButtonClicked()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);

        if (_isGameEnded || _isGameStarted || _isPreparingRound || !_isRoundReady)
        {
            return;
        }

        StartGame();
    }

    private void StartGame()
    {
        SetStartUiVisible(false);
        SetGameplayContentVisible(true);
        SetAllItemButtonsInteractable(true);

        _isGameStarted = true;
        _remainingTime = _gameDuration;
        RefreshTimeText(_remainingTime);
        _countdownCoroutine = StartCoroutine(CountdownCoroutine());
    }

    private IEnumerator CountdownCoroutine()
    {
        while (_remainingTime > 0f && !_isGameEnded)
        {
            RefreshTimeText(_remainingTime);
            yield return null;
            _remainingTime -= Time.deltaTime;
        }

        if (!_isGameEnded && _foundTargetItemIndices.Count < TargetCount)
        {
            EndGameFail();
        }
    }

    private void OnMarketItemClicked(GameSuperMarketItem marketItem)
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);

        if (!_isGameStarted || _isGameEnded || _isSearchOnCooldown || marketItem == null)
        {
            return;
        }

        int itemIndex = marketItem.ItemIndex;
        GameSuperMarketTarget matchedTarget = FindTargetByItemIndex(itemIndex);
        if (matchedTarget == null)
        {
            BeginSearchCooldown();
            return;
        }

        if (matchedTarget.IsFound)
        {
            return;
        }

        matchedTarget.MarkFound();
        _foundTargetItemIndices.Add(itemIndex);
        if (_foundTargetItemIndices.Count >= TargetCount)
        {
            EndGameSuccess();
        }
    }

    private GameSuperMarketTarget FindTargetByItemIndex(int itemIndex)
    {
        for (int i = 0; i < _marketTargets.Length; i++)
        {
            GameSuperMarketTarget marketTarget = _marketTargets[i];
            if (marketTarget != null && marketTarget.ItemIndex == itemIndex)
            {
                return marketTarget;
            }
        }

        return null;
    }

    private void BeginSearchCooldown()
    {
        if (_isSearchOnCooldown || _isGameEnded || !_isGameStarted)
        {
            return;
        }

        StopSearchCooldown();
        _isSearchOnCooldown = true;
        SetAllItemButtonsInteractable(false);
        _searchCooldownCoroutine = StartCoroutine(SearchCooldownCoroutine());
    }

    private IEnumerator SearchCooldownCoroutine()
    {
        yield return new WaitForSeconds(_wrongPickPenaltyDuration);
        _isSearchOnCooldown = false;
        _searchCooldownCoroutine = null;

        if (!_isGameEnded && _isGameStarted)
        {
            SetAllItemButtonsInteractable(true);
        }
    }

    private void StopSearchCooldown()
    {
        if (_searchCooldownCoroutine != null)
        {
            StopCoroutine(_searchCooldownCoroutine);
            _searchCooldownCoroutine = null;
        }

        _isSearchOnCooldown = false;
    }

    private void StopCountdown()
    {
        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = null;
        }
    }

    private void SetGameplayContentVisible(bool visible)
    {
        if (_itemParent != null)
        {
            _itemParent.gameObject.SetActive(visible);
        }

        if (_targetParent != null)
        {
            _targetParent.gameObject.SetActive(visible);
        }

        if (!visible)
        {
            return;
        }

        for (int i = 0; i < _marketItems.Length; i++)
        {
            if (_marketItems[i] != null)
            {
                _marketItems[i].SetGameplayVisible(true);
            }
        }

        for (int i = 0; i < _marketTargets.Length; i++)
        {
            if (_marketTargets[i] != null)
            {
                _marketTargets[i].SetGameplayVisible(true);
            }
        }
    }

    private void SetStartUiVisible(bool visible)
    {
        if (_btnStart != null)
        {
            _btnStart.gameObject.SetActive(visible);
        }

        if (_goStart != null)
        {
            _goStart.SetActive(visible);
        }
    }

    private void SetRoundReady(bool isReady)
    {
        _isRoundReady = isReady;
        RefreshStartText(isReady);

        if (_btnStart != null)
        {
            _btnStart.interactable = isReady;
        }
    }

    private void RefreshStartText(bool isReady)
    {
        if (_txtStart == null)
        {
            return;
        }

        _txtStart.text = isReady ? StartReadyMessage : StartPreparingMessage;
    }

    private void SetAllItemButtonsInteractable(bool interactable)
    {
        for (int i = 0; i < _marketItems.Length; i++)
        {
            if (_marketItems[i] != null)
            {
                _marketItems[i].SetInteractable(interactable);
            }
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

    private void EndGameSuccess()
    {
        if (_isGameEnded)
        {
            return;
        }

        _isGameEnded = true;
        StopSearchCooldown();
        StopCountdown();
        SetAllItemButtonsInteractable(false);

        PlayerInfoManager.GetInstance().GrantConvenienceSuperMarketMonthlyDiscount();
        FinishGameAndClose(true);
        CommonTipView.Show(SuccessTipMessage);
    }

    private void EndGameFail()
    {
        if (_isGameEnded)
        {
            return;
        }

        _isGameEnded = true;
        StopSearchCooldown();
        StopCountdown();
        SetAllItemButtonsInteractable(false);

        FinishGameAndClose(false);
        CommonMessageView.Show(FailMessage);
    }

    private void ClosePanel()
    {
        GameManager.Audio.Play(AudioDefine.SFXClose);
        if (_isGameEnded)
        {
            return;
        }

        _isGameEnded = true;
        StopSearchCooldown();
        StopCountdown();
        FinishGameAndClose(false);
    }

    private void FinishGameAndClose(bool isSuccess)
    {
        Action<bool> onGameCompleted = _onGameCompleted;
        _onGameCompleted = null;
        UIManager.GetInstance().ClosePanel(GetPanelName());
        onGameCompleted?.Invoke(isSuccess);
    }

    private System.Random CreateLayoutRandom()
    {
        int seed = unchecked(Environment.TickCount * 397 ^ GetInstanceID() ^ ++_layoutSerial);
        return new System.Random(seed);
    }

    private static int[] CreateShuffledIndices(int count, System.Random random)
    {
        int[] indices = new int[count];
        for (int i = 0; i < count; i++)
        {
            indices[i] = i;
        }

        for (int i = count - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            int temporary = indices[i];
            indices[i] = indices[swapIndex];
            indices[swapIndex] = temporary;
        }

        return indices;
    }

    private static int[] PickDistinctIndices(int totalCount, int pickCount, System.Random random)
    {
        int[] shuffled = CreateShuffledIndices(totalCount, random);
        int[] result = new int[pickCount];
        for (int i = 0; i < pickCount; i++)
        {
            result[i] = shuffled[i];
        }

        return result;
    }
}
