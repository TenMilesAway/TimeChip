using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class LotteryView : UIBasePanel
{
    [Header("Animation")]
    [SerializeField] private RectTransform lotteryBox;
    [SerializeField] private RectTransform[] bubbles;
    [SerializeField] private Button lotteryButton;

    private Vector3 _lotteryBoxScale;
    private float _lotteryBoxRotation;
    private Vector2[] _bubblePositions;
    private Vector3[] _bubbleScales;
    private bool _hasCachedAnimationState;
    private bool _hasRegisteredButtonListener;

    public override string GetPanelName()
    {
        return GlobalDefine.LotteryView;
    }

    protected override void ShowHandle()
    {
        CacheAnimationState();
        RestoreAnimationState();
        RegisterButtonListener();
    }

    protected override void HideHandle()
    {
        DOTween.Kill(this);
        RestoreAnimationState();
    }

    protected override void OnDestroy()
    {
        DOTween.Kill(this);
        if (lotteryButton != null && _hasRegisteredButtonListener)
        {
            lotteryButton.onClick.RemoveListener(PlayAnimations);
        }

        base.OnDestroy();
    }

    private void RegisterButtonListener()
    {
        if (lotteryButton == null || _hasRegisteredButtonListener)
        {
            return;
        }

        lotteryButton.onClick.AddListener(PlayAnimations);
        _hasRegisteredButtonListener = true;
    }

    private void PlayAnimations()
    {
        DOTween.Kill(this);
        PlayLotteryBoxAnimation();
        PlayBubbleAnimations();
    }

    private void CacheAnimationState()
    {
        if (_hasCachedAnimationState)
        {
            return;
        }

        if (lotteryBox != null)
        {
            _lotteryBoxScale = lotteryBox.localScale;
            _lotteryBoxRotation = lotteryBox.localEulerAngles.z;
        }

        if (bubbles == null)
        {
            bubbles = new RectTransform[0];
        }

        _bubblePositions = new Vector2[bubbles.Length];
        _bubbleScales = new Vector3[bubbles.Length];
        for (int i = 0; i < bubbles.Length; i++)
        {
            if (bubbles[i] == null)
            {
                continue;
            }

            _bubblePositions[i] = bubbles[i].anchoredPosition;
            _bubbleScales[i] = bubbles[i].localScale;
        }

        _hasCachedAnimationState = true;
    }

    private void PlayLotteryBoxAnimation()
    {
        if (lotteryBox == null)
        {
            return;
        }

        const float shakeAngle = 7f;
        const float animationDuration = 0.16f;

        DOTween.Sequence()
            .Append(lotteryBox.DORotate(new Vector3(0f, 0f, _lotteryBoxRotation + shakeAngle), animationDuration))
            .Join(lotteryBox.DOScale(_lotteryBoxScale * 1.08f, animationDuration))
            .Append(lotteryBox.DORotate(new Vector3(0f, 0f, _lotteryBoxRotation - shakeAngle), animationDuration * 2f))
            .Join(lotteryBox.DOScale(_lotteryBoxScale * 0.92f, animationDuration * 2f))
            .Append(lotteryBox.DORotate(new Vector3(0f, 0f, _lotteryBoxRotation), animationDuration))
            .Join(lotteryBox.DOScale(_lotteryBoxScale, animationDuration))
            .SetEase(Ease.InOutSine)
            .SetLoops(-1)
            .SetUpdate(true)
            .SetTarget(this);
    }

    private void PlayBubbleAnimations()
    {
        for (int i = 0; i < bubbles.Length; i++)
        {
            RectTransform bubble = bubbles[i];
            if (bubble == null)
            {
                continue;
            }

            float duration = 1.2f + i * 0.1f;
            Vector2 movement = new Vector2(i % 2 == 0 ? 12f : -12f, 16f);

            DOTween.Sequence()
                .Append(bubble.DOAnchorPos(_bubblePositions[i] + movement, duration))
                .Join(bubble.DOScale(_bubbleScales[i] * 1.06f, duration))
                .Append(bubble.DOAnchorPos(_bubblePositions[i] - movement, duration * 2f))
                .Join(bubble.DOScale(_bubbleScales[i] * 0.94f, duration * 2f))
                .Append(bubble.DOAnchorPos(_bubblePositions[i], duration))
                .Join(bubble.DOScale(_bubbleScales[i], duration))
                .SetEase(Ease.InOutSine)
                .SetLoops(-1)
                .SetUpdate(true)
                .SetTarget(this);
        }
    }

    private void RestoreAnimationState()
    {
        if (!_hasCachedAnimationState)
        {
            return;
        }

        if (lotteryBox != null)
        {
            lotteryBox.localRotation = Quaternion.Euler(0f, 0f, _lotteryBoxRotation);
            lotteryBox.localScale = _lotteryBoxScale;
        }

        for (int i = 0; i < bubbles.Length; i++)
        {
            if (bubbles[i] == null)
            {
                continue;
            }

            bubbles[i].anchoredPosition = _bubblePositions[i];
            bubbles[i].localScale = _bubbleScales[i];
        }
    }
}
