using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class GameStamp : UIBasePanel
{
    [SerializeField] private Text _txtTime;
    [SerializeField] private Button _btnPoint;
    [SerializeField] private Button _btnClose;
    [SerializeField] private Transform _fileParent;
    [SerializeField] private Transform _stampBottom;
    [SerializeField] private Transform _stampMid;
    [SerializeField] private RectTransform _rectJudgeZone;
    [SerializeField] private GameObject[] _goTargets;
    [SerializeField, Min(1f)] private float _gameDuration = 15f;
    [SerializeField] private float _leftBoundary = -280f;
    [SerializeField] private float _rightBoundary = 280f;
    [SerializeField, Min(0f)] private float _minFileSpeed = 210f;
    [SerializeField, Min(0f)] private float _maxFileSpeed = 300f;
    [SerializeField, Min(0.1f)] private float _fileSpawnInterval = 2.4f;
    [SerializeField, Min(0.01f)] private float _fileSpawnScaleDuration = 0.22f;
    [SerializeField, Min(0.01f)] private float _fileDespawnScaleDuration = 0.18f;
    [SerializeField, Min(0.01f)] private float _targetRevealDuration = 0.28f;
    [SerializeField] private float _targetRevealStartScale = 2f;
    [SerializeField, Min(0.01f)] private float _stampPressDuration = 0.18f;
    [SerializeField] private float _stampPressStartScale = 1.2f;
    [SerializeField] private float _stampPressEndScale = 0.75f;
    [SerializeField] private float _stampBottomIdleScaleMultiplier = 1.06f;
    [SerializeField, Min(0.01f)] private float _stampBottomIdleHalfCycleDuration = 1.6f;
    [SerializeField, Min(0f)] private float _stampMissCooldownDuration = 3f;
    [SerializeField, Range(0f, 1f)] private float _stampBottomDisabledAlpha = 0.5f;

    private const string FailMessage = "游戏失败";
    private const string SuccessTipMessage = "挑战成功";

    private static readonly object StampBottomIdleTweenId = new object();

    private readonly List<GameStampFile> _activeFiles = new List<GameStampFile>();
    private readonly List<Vector3> _targetDefaultScales = new List<Vector3>();

    private bool _isGameStarted;
    private bool _isGameEnded;
    private int _stampedCount;
    private int _spawnSerial;
    private float _remainingTime;
    private string _resourceTag;
    private Coroutine _countdownCoroutine;
    private Coroutine _spawnCoroutine;
    private Vector3 _stampMidDefaultScale = Vector3.one;
    private Vector3 _stampBottomDefaultScale = Vector3.one;
    private bool _isStampAnimating;
    private bool _isStampOnCooldown;
    private Coroutine _stampCooldownCoroutine;
    private CanvasGroup _stampBottomCanvasGroup;
    private float _stampBottomDefaultAlpha = 1f;

    private void Awake()
    {
        _resourceTag = GetInstanceID().ToString();
        if (_stampMid != null)
        {
            _stampMidDefaultScale = _stampMid.localScale;
        }

        if (_stampBottom != null)
        {
            _stampBottomDefaultScale = _stampBottom.localScale;
            _stampBottomCanvasGroup = _stampBottom.GetComponent<CanvasGroup>();
            if (_stampBottomCanvasGroup == null)
            {
                _stampBottomCanvasGroup = _stampBottom.gameObject.AddComponent<CanvasGroup>();
            }

            _stampBottomDefaultAlpha = _stampBottomCanvasGroup.alpha;
        }

        CacheTargetDefaultScales();

        if (_btnPoint != null)
        {
            _btnPoint.onClick.AddListener(OnPointButtonClicked);
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
    }

    protected override void HideHandle()
    {
        base.HideHandle();
        StopStampCooldown();
        StopGameLoops();
        StopStampBottomIdleAnimation();
        ClearActiveFiles();
        DOTween.Kill(this);
    }

    protected override void OnDestroy()
    {
        StopStampBottomIdleAnimation();

        if (_btnPoint != null)
        {
            _btnPoint.onClick.RemoveListener(OnPointButtonClicked);
        }

        if (_btnClose != null)
        {
            _btnClose.onClick.RemoveListener(ClosePanel);
        }

        DOTween.Kill(this);
        base.OnDestroy();
    }

    private void Update()
    {
        if (!_isGameStarted || _isGameEnded)
        {
            return;
        }

        MoveActiveFiles(Time.deltaTime);
    }

    public override string GetPanelName()
    {
        return GlobalDefine.GameStamp;
    }

    private void CacheTargetDefaultScales()
    {
        _targetDefaultScales.Clear();
        if (_goTargets == null)
        {
            return;
        }

        for (int i = 0; i < _goTargets.Length; i++)
        {
            if (_goTargets[i] != null)
            {
                _targetDefaultScales.Add(_goTargets[i].transform.localScale);
            }
            else
            {
                _targetDefaultScales.Add(Vector3.one);
            }
        }
    }

    private void ResetGameState()
    {
        StopGameLoops();
        StopStampBottomIdleAnimation();
        ClearActiveFiles();
        DOTween.Kill(this);

        _isGameStarted = false;
        _isGameEnded = false;
        _stampedCount = 0;
        _spawnSerial = 0;
        _remainingTime = _gameDuration;
        _isStampAnimating = false;
        StopStampCooldown();

        RefreshTimeText(_remainingTime);
        SetTargetsVisible(0);
        ResetStampVisuals();
        EnsureStampBottomIdleAnimation();

        if (_btnPoint != null)
        {
            _btnPoint.interactable = true;
        }
    }

    private void OnPointButtonClicked()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);

        if (_isGameEnded)
        {
            return;
        }

        if (!_isGameStarted)
        {
            StartGame();
            return;
        }

        if (_isStampOnCooldown)
        {
            return;
        }

        PlayStampPressAnimation();
        if (!TryStampFileInJudgeZone())
        {
            BeginStampMissCooldown();
        }
    }

    private void StartGame()
    {
        StopStampBottomIdleAnimation();
        _isGameStarted = true;
        _remainingTime = _gameDuration;
        RefreshTimeText(_remainingTime);
        _countdownCoroutine = StartCoroutine(CountdownCoroutine());
        _spawnCoroutine = StartCoroutine(SpawnFilesCoroutine());
        SpawnFile();
    }

    private IEnumerator CountdownCoroutine()
    {
        while (_remainingTime > 0f && !_isGameEnded)
        {
            RefreshTimeText(_remainingTime);
            yield return null;
            _remainingTime -= Time.deltaTime;
        }

        if (!_isGameEnded && _stampedCount < GetRequiredStampCount())
        {
            EndGameFail();
        }
    }

    private IEnumerator SpawnFilesCoroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(_fileSpawnInterval);
        while (_isGameStarted && !_isGameEnded)
        {
            yield return wait;
            if (_isGameStarted && !_isGameEnded)
            {
                SpawnFile();
            }
        }
    }

    private async void SpawnFile()
    {
        if (!_isGameStarted || _isGameEnded || _fileParent == null)
        {
            return;
        }

        GameObject fileObject = await UnityObjectPoolFactory.GetInstance()
            .GetItem<GameObject>(GlobalDefine.GameStampFile, _resourceTag);
        if (!_isGameStarted || _isGameEnded || fileObject == null)
        {
            if (fileObject != null)
            {
                UnityObjectPoolFactory.GetInstance()
                    .PutItem(GlobalDefine.GameStampFile, fileObject);
            }

            return;
        }

        fileObject.transform.SetParent(_fileParent, false);
        RectTransform fileRect = fileObject.transform as RectTransform;
        if (fileRect != null)
        {
            Vector2 anchoredPosition = fileRect.anchoredPosition;
            anchoredPosition.x = _leftBoundary;
            fileRect.anchoredPosition = anchoredPosition;
        }

        GameStampFile stampFile = fileObject.GetComponent<GameStampFile>();
        if (stampFile == null)
        {
            Debug.LogError("GameStampFile 预制体缺少 GameStampFile 组件。", this);
            UnityObjectPoolFactory.GetInstance().PutItem(GlobalDefine.GameStampFile, fileObject);
            return;
        }

        float minSpeed = Mathf.Min(_minFileSpeed, _maxFileSpeed);
        float maxSpeed = Mathf.Max(_minFileSpeed, _maxFileSpeed);
        float speed = TurnRandom.Range(
            $"GameStamp.FileSpeed.{_spawnSerial++}",
            minSpeed,
            maxSpeed);
        stampFile.Configure(speed);
        stampFile.PrepareForSpawn();
        _activeFiles.Add(stampFile);
        stampFile.PlaySpawnAnimation(_fileSpawnScaleDuration, null);
    }

    private void MoveActiveFiles(float deltaTime)
    {
        for (int i = _activeFiles.Count - 1; i >= 0; i--)
        {
            GameStampFile stampFile = _activeFiles[i];
            if (stampFile == null)
            {
                _activeFiles.RemoveAt(i);
                continue;
            }

            RectTransform fileRect = stampFile.RectTransform;
            if (fileRect == null)
            {
                _activeFiles.RemoveAt(i);
                continue;
            }

            if (stampFile.CanMove)
            {
                Vector2 anchoredPosition = fileRect.anchoredPosition;
                anchoredPosition.x += stampFile.MoveSpeed * deltaTime;
                fileRect.anchoredPosition = anchoredPosition;

                if (anchoredPosition.x > _rightBoundary && !stampFile.IsDespawning)
                {
                    BeginDespawnFile(stampFile);
                }
            }
        }
    }

    private void BeginDespawnFile(GameStampFile stampFile)
    {
        stampFile.PlayDespawnAnimation(_fileDespawnScaleDuration, () =>
        {
            _activeFiles.Remove(stampFile);
            if (stampFile != null)
            {
                stampFile.ResetForPool();
                UnityObjectPoolFactory.GetInstance()
                    .PutItem(GlobalDefine.GameStampFile, stampFile.gameObject);
            }
        });
    }

    private bool TryStampFileInJudgeZone()
    {
        GameStampFile targetFile = FindStampableFileInJudgeZone();
        if (targetFile == null)
        {
            return false;
        }

        targetFile.ApplyStamp();
        RevealTarget(_stampedCount);
        _stampedCount++;
        if (_stampedCount >= GetRequiredStampCount())
        {
            EndGameSuccess();
        }

        return true;
    }

    private void BeginStampMissCooldown()
    {
        if (_isStampOnCooldown || _isGameEnded || !_isGameStarted)
        {
            return;
        }

        StopStampCooldown();
        _isStampOnCooldown = true;
        SetStampBottomInteractable(false);
        _stampCooldownCoroutine = StartCoroutine(StampMissCooldownCoroutine());
    }

    private IEnumerator StampMissCooldownCoroutine()
    {
        yield return new WaitForSeconds(_stampMissCooldownDuration);
        _isStampOnCooldown = false;
        _stampCooldownCoroutine = null;
        if (!_isGameEnded && _isGameStarted)
        {
            SetStampBottomInteractable(true);
        }
    }

    private void StopStampCooldown()
    {
        if (_stampCooldownCoroutine != null)
        {
            StopCoroutine(_stampCooldownCoroutine);
            _stampCooldownCoroutine = null;
        }

        _isStampOnCooldown = false;
        SetStampBottomInteractable(true);
    }

    private void SetStampBottomInteractable(bool interactable)
    {
        if (_stampBottomCanvasGroup != null)
        {
            _stampBottomCanvasGroup.alpha = interactable
                ? _stampBottomDefaultAlpha
                : _stampBottomDisabledAlpha;
        }

        if (_btnPoint != null && _isGameStarted && !_isGameEnded)
        {
            _btnPoint.interactable = interactable;
        }
    }

    private void RevealTarget(int targetIndex)
    {
        if (_goTargets == null ||
            targetIndex < 0 ||
            targetIndex >= _goTargets.Length ||
            _goTargets[targetIndex] == null)
        {
            return;
        }

        GameObject target = _goTargets[targetIndex];
        Transform targetTransform = target.transform;
        Vector3 endScale = targetIndex < _targetDefaultScales.Count
            ? _targetDefaultScales[targetIndex]
            : targetTransform.localScale;

        target.SetActive(true);
        DOTween.Kill(targetTransform);
        targetTransform.localScale = endScale * _targetRevealStartScale;
        targetTransform.DOScale(endScale, _targetRevealDuration)
            .SetEase(Ease.OutBack)
            .SetTarget(targetTransform);
    }

    private int GetRequiredStampCount()
    {
        return _goTargets == null ? 0 : _goTargets.Length;
    }

    private GameStampFile FindStampableFileInJudgeZone()
    {
        GameStampFile bestFile = null;
        float bestDistanceToCenter = float.MaxValue;
        Vector2 judgeCenter = _rectJudgeZone != null
            ? _rectJudgeZone.rect.center
            : Vector2.zero;

        for (int i = 0; i < _activeFiles.Count; i++)
        {
            GameStampFile stampFile = _activeFiles[i];
            if (stampFile == null ||
                stampFile.IsStamped ||
                !stampFile.CanMove ||
                !stampFile.IsFullyInsideJudgeZone(_rectJudgeZone))
            {
                continue;
            }

            Vector2 localPoint = _rectJudgeZone.InverseTransformPoint(stampFile.RectTransform.position);
            float distance = Vector2.SqrMagnitude(localPoint - judgeCenter);
            if (distance < bestDistanceToCenter)
            {
                bestDistanceToCenter = distance;
                bestFile = stampFile;
            }
        }

        return bestFile;
    }

    private void PlayStampPressAnimation()
    {
        if (_stampBottom == null || _stampMid == null || _isStampAnimating)
        {
            return;
        }

        _isStampAnimating = true;
        DOTween.Kill(this);

        _stampBottom.gameObject.SetActive(false);
        _stampMid.gameObject.SetActive(true);
        _stampMid.localScale = Vector3.one * _stampPressStartScale;

        _stampMid.DOScale(_stampPressEndScale, _stampPressDuration)
            .SetEase(Ease.InQuad)
            .SetTarget(this)
            .OnComplete(ResetStampVisualsAfterPress);
    }

    private void ResetStampVisualsAfterPress()
    {
        _isStampAnimating = false;
        ResetStampVisuals();
    }

    private void ResetStampVisuals()
    {
        if (_stampMid != null)
        {
            _stampMid.gameObject.SetActive(false);
            _stampMid.localScale = _stampMidDefaultScale;
        }

        if (_stampBottom != null)
        {
            _stampBottom.gameObject.SetActive(true);
            _stampBottom.localScale = _stampBottomDefaultScale;
        }
    }

    private void EnsureStampBottomIdleAnimation()
    {
        if (_isGameStarted || _stampBottom == null || DOTween.IsTweening(StampBottomIdleTweenId))
        {
            return;
        }

        _stampBottom.localScale = _stampBottomDefaultScale;
        _stampBottom
            .DOScale(_stampBottomDefaultScale * _stampBottomIdleScaleMultiplier, _stampBottomIdleHalfCycleDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetTarget(StampBottomIdleTweenId);
    }

    private void StopStampBottomIdleAnimation()
    {
        DOTween.Kill(StampBottomIdleTweenId);
        if (_stampBottom != null)
        {
            _stampBottom.localScale = _stampBottomDefaultScale;
        }
    }

    private void SetTargetsVisible(int visibleCount)
    {
        if (_goTargets == null)
        {
            return;
        }

        for (int i = 0; i < _goTargets.Length; i++)
        {
            if (_goTargets[i] == null)
            {
                continue;
            }

            bool visible = i < visibleCount;
            _goTargets[i].SetActive(visible);
            if (!visible)
            {
                DOTween.Kill(_goTargets[i].transform);
                Vector3 defaultScale = i < _targetDefaultScales.Count
                    ? _targetDefaultScales[i]
                    : _goTargets[i].transform.localScale;
                _goTargets[i].transform.localScale = defaultScale;
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
        StopStampCooldown();
        StopGameLoops();
        if (_btnPoint != null)
        {
            _btnPoint.interactable = false;
        }

        UIManager.GetInstance().ClosePanel(GetPanelName());
        MissionAPI.Broadcast(new MissionMessage(MissionEventType.GameStamp));
        CommonTipView.Show(SuccessTipMessage);
    }

    private void EndGameFail()
    {
        if (_isGameEnded)
        {
            return;
        }

        _isGameEnded = true;
        StopStampCooldown();
        StopGameLoops();
        if (_btnPoint != null)
        {
            _btnPoint.interactable = false;
        }

        UIManager.GetInstance().ClosePanel(GetPanelName());
        CommonMessageView.Show(FailMessage);
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
    }

    private void ClearActiveFiles()
    {
        for (int i = 0; i < _activeFiles.Count; i++)
        {
            if (_activeFiles[i] != null)
            {
                _activeFiles[i].ResetForPool();
                UnityObjectPoolFactory.GetInstance()
                    .PutItem(GlobalDefine.GameStampFile, _activeFiles[i].gameObject);
            }
        }

        _activeFiles.Clear();
    }

    private void ClosePanel()
    {
        GameManager.Audio.Play(AudioDefine.SFXClose);
        _isGameEnded = true;
        StopStampCooldown();
        StopGameLoops();
        UIManager.GetInstance().ClosePanel(GetPanelName());
    }
}
