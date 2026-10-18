using System;
using DG.Tweening;
using RedSaw.MissionSystem;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuMissionItem : MonoBehaviour
{
    private const float CompleteBreathingDuration = 0.8f;
    private const float CompleteBreathingScale = 1.15f;

    [SerializeField] private Text _txtTitle;
    [SerializeField] private Text _txtDes;
    [SerializeField] private Text _txtProgress;
    [SerializeField] private Image _imgComplete;
    [SerializeField] private Button _btnComplete;
    [SerializeField] private Slider _sliderProgress;
    [SerializeField] private GameObject _goCompleteGroup;
    [SerializeField] private GameObject _goGo;
    [SerializeField] private Button _btnGo;

    private Vector3 _completeIconInitialScale;
    private bool _hasCompleteIconInitialScale;

    private void Awake()
    {
        EnsureGoButton();
    }

    public void SetData(
        cfg.Mission missionConfig,
        Mission<MissionMessage> mission,
        Action completeAction)
    {
        if (missionConfig == null || mission == null)
        {
            Debug.LogError("主菜单任务条目初始化失败：任务配置或任务实例为空。", this);
            Clear();
            return;
        }

        EnsureGoButton();
        _txtTitle.text = missionConfig.Name;
        _txtDes.text = missionConfig.Desc;
        if (MissionAPI.TryGetMissionTarget(
                mission.id,
                out int targetItemId,
                out int targetItemCount))
        {
            cfg.Item targetItem = DataTableMananger.GetInstance()
                .Tables
                .ItemTable
                .GetOrDefault(targetItemId);
            if (targetItem != null)
            {
                _txtDes.text = string.Format(
                    missionConfig.Desc,
                    targetItem.Name,
                    targetItemCount);
            }
        }

        MissionProgress[] progresses = mission.Progresses;
        int target = progresses.Length > 0
            ? Mathf.Max(1, progresses[0].targetCount)
            : Mathf.Max(1, int.TryParse(missionConfig.Target, out int value) ? value : 1);
        int current = progresses.Length == 0 ? 0 : Mathf.Clamp(progresses[0].currentCount, 0, target);
        _sliderProgress.minValue = 0f;
        _sliderProgress.maxValue = target;
        _sliderProgress.value = current;
        _txtProgress.text = $"{current} / {target}";

        bool canComplete = mission.IsFinished;
        _goCompleteGroup.SetActive(canComplete);
        _btnComplete.onClick.RemoveAllListeners();
        _btnComplete.interactable = canComplete;
        if (canComplete)
        {
            _btnComplete.onClick.AddListener(() =>
            {
                UiButtonAudio.PlayClick();
                completeAction();
            });
            PlayCompleteBreathingAnimation();
        }
        else
        {
            StopCompleteBreathingAnimation();
        }

        bool showGoButton = MissionGoActions.ShouldShowGoButton(missionConfig, mission);
        if (_goGo != null)
        {
            _goGo.SetActive(showGoButton);
        }

        if (_btnGo != null)
        {
            _btnGo.onClick.RemoveAllListeners();
            _btnGo.interactable = showGoButton;
            if (showGoButton)
            {
                _btnGo.onClick.AddListener(() => MissionGoActions.TryRunGoAction(missionConfig));
            }
        }

        gameObject.SetActive(true);
    }

    public void Clear()
    {
        StopCompleteBreathingAnimation();
        _btnComplete.onClick.RemoveAllListeners();
        _btnComplete.interactable = false;
        _goCompleteGroup.SetActive(false);
        if (_goGo != null)
        {
            _goGo.SetActive(false);
        }

        if (_btnGo != null)
        {
            _btnGo.onClick.RemoveAllListeners();
            _btnGo.interactable = false;
        }

        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        StopCompleteBreathingAnimation();
    }

    private void EnsureGoButton()
    {
        if (_btnGo != null || _goGo == null)
        {
            return;
        }

        _btnGo = _goGo.GetComponent<Button>();
        if (_btnGo != null)
        {
            return;
        }

        Image clickImage = _goGo.GetComponentInChildren<Image>(true);
        if (clickImage == null)
        {
            return;
        }

        _btnGo = clickImage.GetComponent<Button>();
        if (_btnGo == null)
        {
            _btnGo = clickImage.gameObject.AddComponent<Button>();
            _btnGo.targetGraphic = clickImage;
        }
    }

    private void PlayCompleteBreathingAnimation()
    {
        if (!_hasCompleteIconInitialScale)
        {
            _completeIconInitialScale = _imgComplete.rectTransform.localScale;
            _hasCompleteIconInitialScale = true;
        }

        DOTween.Kill(_imgComplete.rectTransform);
        _imgComplete.rectTransform.localScale = _completeIconInitialScale;
        _imgComplete.rectTransform.DOScale(
            _completeIconInitialScale * CompleteBreathingScale,
            CompleteBreathingDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetTarget(_imgComplete.rectTransform);
    }

    private void StopCompleteBreathingAnimation()
    {
        if (_imgComplete == null)
        {
            return;
        }

        DOTween.Kill(_imgComplete.rectTransform);
        if (_hasCompleteIconInitialScale)
        {
            _imgComplete.rectTransform.localScale = _completeIconInitialScale;
        }
    }
}
