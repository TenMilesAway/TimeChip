using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FishView : UIBasePanel
{
    private const float BiteWaitMinSeconds = 1f;             // 钓鱼: 最短等待时间
    private const float BiteWaitMaxSeconds = 2f;            // 钓鱼: 最长等待时间
    private const float FishMoveDurationMinSeconds = 0.25f;  // 捕鱼: 鱼移动最短时间
    private const float FishMoveDurationMaxSeconds = 1.2f;   // 捕鱼: 鱼移动最长时间
    private const float CatchRiseDistance = 110f;            // 捕鱼: 捕捉上升距离
    private const float CatchRiseSpeed = 1100f;               // 捕鱼: 捕捉上升速度
    private const float CatchFallSpeed = 220f;                // 捕鱼: 捕捉下降速度
    private const float ProgressIncreasePerSecond = 0.25f;   // 捕鱼游戏: 进度条增加速度
    private const float ProgressDecreasePerSecond = 0.12f;   // 捕鱼游戏: 进度条减少速度

    [SerializeField] private Image _imgFishProgress;    // 捕鱼游戏: 鱼进度条
    [SerializeField] private Button _btnFish;           // 钓鱼按钮
    [SerializeField] private Button _btnClick;          // 捕鱼游戏: 与鱼互动按钮
    [SerializeField] private Animator _animatorFishRod; // 鱼竿动画

    [SerializeField] private GameObject _goFishCatch;   // 捕鱼游戏
    [SerializeField] private RectTransform _rectFishBg; // 捕鱼游戏: 鱼背景位置
    [SerializeField] private RectTransform _rectFish;   // 捕鱼游戏: 鱼位置
    [SerializeField] private RectTransform _rectCatch;  // 捕鱼游戏: 捕捉位置

    private const string AnimationEmpty = "Empty";   // 空闲动画
    private const string AnimationStart = "Start";   // 开始钓鱼动画
    private const string AnimationIdle = "Idle";     // 钓鱼中动画
    private const string AnimationEnd = "End";       // 上钩动画

    private Coroutine _fishingCoroutine;
    private bool _isCatchActive;
    private float _fishTargetY;
    private float _fishMoveSpeed;
    private float _fishMinY;
    private float _fishMaxY;
    private float _catchMinY;
    private float _catchMaxY;
    private float _catchTargetY;
    private Vector2 _initialFishPosition;
    private Vector2 _initialCatchPosition;

    private void Awake()
    {
        if (!HasValidUiReferences())
        {
            enabled = false;
            return;
        }

        _initialFishPosition = _rectFish.anchoredPosition;
        _initialCatchPosition = _rectCatch.anchoredPosition;
        _btnFish.onClick.AddListener(OnClickFish);
        _btnClick.onClick.AddListener(OnClickCatch);
    }

    protected override void ShowHandle()
    {
        ResetFishingState();
    }

    protected override void HideHandle()
    {
        StopFishing();
    }

    protected override void OnDestroy()
    {
        StopFishing();
        if (_btnFish != null)
        {
            _btnFish.onClick.RemoveListener(OnClickFish);
        }

        if (_btnClick != null)
        {
            _btnClick.onClick.RemoveListener(OnClickCatch);
        }

        base.OnDestroy();
    }

    private void Update()
    {
        if (!_isCatchActive)
        {
            return;
        }

        MoveFish();
        MoveCatch();
        UpdateFishProgress();
    }

    private void OnClickFish()
    {
        if (_fishingCoroutine != null)
        {
            return;
        }

        _btnFish.interactable = false;
        _fishingCoroutine = StartCoroutine(WaitForFishBite());
    }

    private void OnClickCatch()
    {
        if (!_isCatchActive)
        {
            return;
        }

        _catchTargetY = Mathf.Min(
            Mathf.Max(_catchTargetY, _rectCatch.anchoredPosition.y) + CatchRiseDistance,
            _catchMaxY);
    }

    private IEnumerator WaitForFishBite()
    {
        yield return PlayAnimationOnce(AnimationStart);
        _animatorFishRod.Play(AnimationIdle, 0, 0f);
        yield return new WaitForSeconds(Random.Range(BiteWaitMinSeconds, BiteWaitMaxSeconds));
        yield return PlayAnimationOnce(AnimationEnd);

        StartCatchGame();
        _fishingCoroutine = null;
    }

    private IEnumerator PlayAnimationOnce(string animationName)
    {
        int animationHash = Animator.StringToHash(animationName);
        _animatorFishRod.Play(animationHash, 0, 0f);
        yield return null;

        while (_animatorFishRod.isActiveAndEnabled)
        {
            AnimatorStateInfo stateInfo = _animatorFishRod.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.shortNameHash == animationHash &&
                stateInfo.normalizedTime >= 1f &&
                !_animatorFishRod.IsInTransition(0))
            {
                yield break;
            }

            yield return null;
        }
    }

    private void StartCatchGame()
    {
        _goFishCatch.SetActive(true);
        CalculateMovementBounds();
        _isCatchActive = true;
        _btnClick.interactable = true;
        SelectNextFishTarget();
    }

    private void MoveFish()
    {
        Vector2 position = _rectFish.anchoredPosition;
        position.y = Mathf.MoveTowards(
            position.y,
            _fishTargetY,
            _fishMoveSpeed * Time.deltaTime);
        _rectFish.anchoredPosition = position;

        if (Mathf.Approximately(position.y, _fishTargetY))
        {
            SelectNextFishTarget();
        }
    }

    private void MoveCatch()
    {
        Vector2 position = _rectCatch.anchoredPosition;
        if (position.y < _catchTargetY)
        {
            position.y = Mathf.MoveTowards(
                position.y,
                _catchTargetY,
                CatchRiseSpeed * Time.deltaTime);
            _rectCatch.anchoredPosition = position;
            return;
        }

        position.y = Mathf.Max(position.y - CatchFallSpeed * Time.deltaTime, _catchMinY);
        _rectCatch.anchoredPosition = position;
        _catchTargetY = position.y;
    }

    private void UpdateFishProgress()
    {
        bool isFishCaught = _rectFish.anchoredPosition.y >=
            _rectCatch.anchoredPosition.y - _rectCatch.rect.height * _rectCatch.pivot.y &&
            _rectFish.anchoredPosition.y <=
            _rectCatch.anchoredPosition.y +
            _rectCatch.rect.height * (1f - _rectCatch.pivot.y);
        float targetFillAmount = isFishCaught ? 1f : 0f;
        float changeSpeed = isFishCaught
            ? ProgressIncreasePerSecond
            : ProgressDecreasePerSecond;
        _imgFishProgress.fillAmount = Mathf.MoveTowards(
            _imgFishProgress.fillAmount,
            targetFillAmount,
            changeSpeed * Time.deltaTime);

        if (_imgFishProgress.fillAmount >= 1f)
        {
            CompleteFishCatch();
        }
    }

    private void CompleteFishCatch()
    {
        ResetFishingState();
        CommonTipView.Show("成功捕捉到鱼！");
    }

    private void SelectNextFishTarget()
    {
        float currentY = _rectFish.anchoredPosition.y;
        _fishTargetY = Random.Range(_fishMinY, _fishMaxY);
        if (Mathf.Abs(_fishTargetY - currentY) < 1f)
        {
            _fishTargetY = currentY < (_fishMinY + _fishMaxY) * 0.5f
                ? _fishMaxY
                : _fishMinY;
        }

        float duration = Random.Range(
            FishMoveDurationMinSeconds,
            FishMoveDurationMaxSeconds);
        _fishMoveSpeed = Mathf.Abs(_fishTargetY - currentY) / duration;
    }

    private void CalculateMovementBounds()
    {
        GetVerticalBounds(_rectFish, out _fishMinY, out _fishMaxY);
        GetVerticalBounds(_rectCatch, out _catchMinY, out _catchMaxY);
    }

    private void GetVerticalBounds(
        RectTransform movingRect,
        out float minY,
        out float maxY)
    {
        float backgroundBottom = -_rectFishBg.rect.height * _rectFishBg.pivot.y;
        float backgroundTop = _rectFishBg.rect.height * (1f - _rectFishBg.pivot.y);
        minY = backgroundBottom + movingRect.rect.height * movingRect.pivot.y;
        maxY = backgroundTop - movingRect.rect.height * (1f - movingRect.pivot.y);
    }

    private void ResetFishingState()
    {
        StopFishing();
        _goFishCatch.SetActive(false);
        _imgFishProgress.fillAmount = 0f;
        _rectFish.anchoredPosition = _initialFishPosition;
        _rectCatch.anchoredPosition = _initialCatchPosition;
        _catchTargetY = _initialCatchPosition.y;
        _btnFish.interactable = true;
        _btnClick.interactable = false;
        _animatorFishRod.Play(AnimationEmpty, 0, 0f);
    }

    private void StopFishing()
    {
        if (_fishingCoroutine != null)
        {
            StopCoroutine(_fishingCoroutine);
            _fishingCoroutine = null;
        }

        _isCatchActive = false;
    }

    private bool HasValidUiReferences()
    {
        if (_imgFishProgress != null &&
            _btnFish != null &&
            _btnClick != null &&
            _animatorFishRod != null &&
            _goFishCatch != null &&
            _rectFishBg != null &&
            _rectFish != null &&
            _rectCatch != null)
        {
            return true;
        }

        Debug.LogError("FishView 存在未绑定的 UI 引用", this);
        return false;
    }

    public override string GetPanelName()
    {
        return GlobalDefine.FishView;
    }
}
