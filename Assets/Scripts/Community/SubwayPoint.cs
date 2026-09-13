using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SubwayPoint : MonoBehaviour
{
    private const float SelectionPopScale = 1.1f;
    private const float SelectionBreathScale = 1.03f;
    private const float SelectionPopDuration = 0.12f;
    private const float SelectionBreathDuration = 0.65f;

    [SerializeField] private GameObject _goNormal;   // 普通状态
    [SerializeField] private GameObject _goSelected; // 选中状态
    [SerializeField] private GameObject _goCurrent;  // 当前所在
    [SerializeField] private GameObject _goLocked;   // 锁定状态

    private Button _button;
    private cfg.Subway _locationConfig;
    private Action<cfg.Subway> _selectHandler;
    private bool _isCurrent;
    private bool _isUnlocked;
    private bool _isSelected;
    private Vector3 _selectedScale;
    private Vector3 _currentScale;
    private Vector3 _lockedScale;

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_button == null)
        {
            Debug.LogError("SubwayPoint 缺少 Button 组件", this);
            enabled = false;
            return;
        }

        _selectedScale = _goSelected.transform.localScale;
        _currentScale = _goCurrent.transform.localScale;
        _lockedScale = _goLocked.transform.localScale;
        _button.onClick.AddListener(OnClick);
    }

    private void OnDisable()
    {
        StopSelectionFeedback();
    }

    private void OnDestroy()
    {
        StopSelectionFeedback();
        if (_button != null)
        {
            _button.onClick.RemoveListener(OnClick);
        }
    }

    public void SetData(
        cfg.Subway locationConfig,
        bool isCurrent,
        bool isUnlocked,
        Action<cfg.Subway> selectHandler)
    {
        _locationConfig = locationConfig;
        _selectHandler = selectHandler;
        _isCurrent = isCurrent;
        _isUnlocked = isUnlocked;
        _isSelected = false;
        StopSelectionFeedback();

        gameObject.SetActive(locationConfig != null);
        if (locationConfig == null)
        {
            return;
        }

        RefreshState();
    }

    public void SetSelected(bool isSelected)
    {
        if (_locationConfig == null)
        {
            return;
        }

        _isSelected = isSelected;
        RefreshState();
        if (isSelected)
        {
            PlaySelectionFeedback();
        }
        else
        {
            StopSelectionFeedback();
        }
    }

    private void RefreshState()
    {
        _goNormal.SetActive(!_isCurrent && _isUnlocked && !_isSelected);
        _goSelected.SetActive(!_isCurrent && _isUnlocked && _isSelected);
        _goCurrent.SetActive(_isCurrent);
        _goLocked.SetActive(!_isCurrent && !_isUnlocked);
        _button.interactable = true;
    }

    private void OnClick()
    {
        if (_locationConfig != null)
        {
            _selectHandler?.Invoke(_locationConfig);
        }
    }

    private void PlaySelectionFeedback()
    {
        Transform feedbackTransform = GetFeedbackTransform();
        if (feedbackTransform == null)
        {
            return;
        }

        StopSelectionFeedback();
        Vector3 originalScale = GetOriginalScale(feedbackTransform);
        feedbackTransform.localScale = originalScale;

        Sequence sequence = DOTween.Sequence().SetTarget(this);
        sequence.Append(
            feedbackTransform.DOScale(originalScale * SelectionPopScale, SelectionPopDuration)
                .SetEase(Ease.OutBack));
        sequence.Append(
            feedbackTransform.DOScale(originalScale, SelectionPopDuration)
                .SetEase(Ease.OutQuad));

        if (_isUnlocked && !_isCurrent)
        {
            sequence.Append(
                feedbackTransform.DOScale(
                    originalScale * SelectionBreathScale,
                    SelectionBreathDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo));
        }
    }

    private void StopSelectionFeedback()
    {
        DOTween.Kill(this);
        _goSelected.transform.localScale = _selectedScale;
        _goCurrent.transform.localScale = _currentScale;
        _goLocked.transform.localScale = _lockedScale;
    }

    private Transform GetFeedbackTransform()
    {
        if (_isCurrent)
        {
            return _goCurrent.transform;
        }

        return _isUnlocked ? _goSelected.transform : _goLocked.transform;
    }

    private Vector3 GetOriginalScale(Transform feedbackTransform)
    {
        if (feedbackTransform == _goSelected.transform)
        {
            return _selectedScale;
        }

        return feedbackTransform == _goCurrent.transform ? _currentScale : _lockedScale;
    }
}
