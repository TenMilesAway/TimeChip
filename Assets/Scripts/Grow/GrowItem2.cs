using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class GrowItem2 : MonoBehaviour
{
    [SerializeField] private Image _imgIconUnlock;
    [SerializeField] private Image _imgIconLock;
    [SerializeField] private Image _imgBGUnlock;
    [SerializeField] private Button _btnDetail;
    [SerializeField] private int _growID;

    private RectTransform _rectTransform;
    private Vector3 _baseScale;
    private Sequence _juiceSequence;
    private bool _isUnlockAnimating;

    public int GrowId => _growID;
    public RectTransform RectTransform => _rectTransform;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
        _baseScale = _rectTransform != null ? _rectTransform.localScale : Vector3.one;
        if (_btnDetail == null)
        {
            _btnDetail = GetComponent<Button>();
        }

        if (_imgBGUnlock != null)
        {
            _imgBGUnlock.fillAmount = 0f;
        }
    }

    private void OnDestroy()
    {
        KillJuiceTween();
    }

    public void BindDetailClick(Action<GrowItem2> onDetail)
    {
        if (_btnDetail == null)
        {
            return;
        }

        _btnDetail.onClick.RemoveAllListeners();
        _btnDetail.onClick.AddListener(() => onDetail?.Invoke(this));
    }

    public void RefreshVisual(Grow2TalentGraph graph, GlobalInfoManager globalInfo)
    {
        if (_isUnlockAnimating)
        {
            return;
        }

        bool unlocked = globalInfo.IsGrow2TalentUnlocked(_growID);
        if (_imgIconLock != null)
        {
            _imgIconLock.gameObject.SetActive(!unlocked);
        }

        if (_imgIconUnlock != null)
        {
            _imgIconUnlock.gameObject.SetActive(unlocked);
        }

        if (_imgBGUnlock != null)
        {
            _imgBGUnlock.fillAmount = unlocked ? 1f : 0f;
        }

        if (_rectTransform != null)
        {
            _rectTransform.localScale = _baseScale;
            _rectTransform.localRotation = Quaternion.identity;
        }
    }

    public void ApplyUnlockHoldProgress(float progress)
    {
        if (_imgBGUnlock != null)
        {
            _imgBGUnlock.fillAmount = progress;
        }

        if (progress <= 0f)
        {
            return;
        }

        if (_juiceSequence == null || !_juiceSequence.IsActive())
        {
            StartJuiceTween();
        }
    }

    public void BeginUnlockHold()
    {
        _isUnlockAnimating = true;
        if (_imgBGUnlock != null)
        {
            _imgBGUnlock.fillAmount = 0f;
        }

        StartJuiceTween();
    }

    public void CancelUnlockHold()
    {
        _isUnlockAnimating = false;
        KillJuiceTween();
        if (_imgBGUnlock != null)
        {
            _imgBGUnlock.fillAmount = 0f;
        }

        if (_rectTransform != null)
        {
            _rectTransform.localScale = _baseScale;
            _rectTransform.localRotation = Quaternion.identity;
        }
    }

    public void CompleteUnlockVisual()
    {
        _isUnlockAnimating = false;
        KillJuiceTween();
        if (_imgBGUnlock != null)
        {
            _imgBGUnlock.fillAmount = 1f;
        }

        if (_imgIconLock != null)
        {
            _imgIconLock.gameObject.SetActive(false);
        }

        if (_imgIconUnlock != null)
        {
            _imgIconUnlock.gameObject.SetActive(true);
        }

        if (_rectTransform != null)
        {
            _rectTransform.localScale = _baseScale;
            _rectTransform.DOPunchScale(Vector3.one * 0.18f, 0.35f, 8, 0.8f).SetUpdate(true);
        }
    }

    private void StartJuiceTween()
    {
        KillJuiceTween();
        if (_rectTransform == null)
        {
            return;
        }

        _rectTransform.localScale = _baseScale;
        _juiceSequence = DOTween.Sequence().SetUpdate(true).SetTarget(_rectTransform);
        _juiceSequence.Append(
            _rectTransform.DOScale(_baseScale * 1.08f, 0.12f).SetEase(Ease.OutQuad));
        _juiceSequence.Append(
            _rectTransform.DOScale(_baseScale * 0.96f, 0.1f).SetEase(Ease.InOutSine));
        _juiceSequence.SetLoops(-1, LoopType.Yoyo);
        _rectTransform.DORotate(new Vector3(0f, 0f, 6f), 0.18f, RotateMode.Fast)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true)
            .SetTarget(_rectTransform);
    }

    private void KillJuiceTween()
    {
        if (_rectTransform != null)
        {
            DOTween.Kill(_rectTransform);
        }

        if (_juiceSequence != null && _juiceSequence.IsActive())
        {
            _juiceSequence.Kill();
        }

        _juiceSequence = null;
    }
}
