using System;
using DG.Tweening;
using RedSaw.MissionSystem;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuMissionItem : MonoBehaviour
{
    private const float CompleteBreathingDuration = 0.8f;
    private const float CompleteBreathingScale = 1.15f;

    [SerializeField] private Text _txtTitle;         // 任务名称
    [SerializeField] private Text _txtDes;           // 任务描述
    [SerializeField] private Text _txtProgress;      // 任务进度, 例: 1 / 1
    [SerializeField] private Image _imgComplete;     // 完成图标
    [SerializeField] private Button _btnComplete;    // 任务完成按钮
    [SerializeField] private Slider _sliderProgress; // 任务进度条
    [SerializeField] private GameObject _goCompleteGroup; // 完成

    private Vector3 _completeIconInitialScale;
    private bool _hasCompleteIconInitialScale;

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

        _txtTitle.text = missionConfig.Name;
        _txtDes.text = missionConfig.Desc;

        int target = Mathf.Max(1, int.TryParse(missionConfig.Target, out int value) ? value : 1);
        MissionProgress[] progresses = mission.Progresses;
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
            _btnComplete.onClick.AddListener(() => completeAction());
            PlayCompleteBreathingAnimation();
        }
        else
        {
            StopCompleteBreathingAnimation();
        }

        gameObject.SetActive(true);
    }

    public void Clear()
    {
        StopCompleteBreathingAnimation();
        _btnComplete.onClick.RemoveAllListeners();
        _btnComplete.interactable = false;
        _goCompleteGroup.SetActive(false);
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        StopCompleteBreathingAnimation();
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
