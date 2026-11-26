using UnityEngine;
using UnityEngine.UI;

public class SettingView : UIBasePanel
{
    private const float MinVolumeDb = -80f;
    private const float MaxVolumeDb = 0f;

    [SerializeField] private Slider _sliderMusic;
    [SerializeField] private Slider _sliderSFX;
    [SerializeField] private Toggle _toggleVibra;
    [SerializeField] private Button _btnBack;
    [SerializeField] private Button _btnOver;
    [SerializeField] private GameObject _goTips;

    private bool _closingForVoluntaryEnd;
    private bool _showBackButton;
    private bool _showVoluntaryEndButton;
    private bool _showTips;

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);

        ResolveOpenParam(param, out _showBackButton, out _showVoluntaryEndButton, out _showTips);

        if (_showBackButton)
        {
            _btnBack.gameObject.SetActive(true);
            _btnBack.onClick.AddListener(OnBackButtonClicked);
        }
        else
        {
            _btnBack.gameObject.SetActive(false);
        }

        if (_showVoluntaryEndButton)
        {
            _btnOver.gameObject.SetActive(true);
            _btnOver.onClick.AddListener(OnOverButtonClicked);
        }
        else
        {
            _btnOver.gameObject.SetActive(false);
        }

        if (_goTips != null)
        {
            _goTips.SetActive(_showTips);
        }

        _sliderMusic.onValueChanged.AddListener(OnMusicVolumeChanged);
        _sliderSFX.onValueChanged.AddListener(OnSFXVolumeChanged);
        _toggleVibra.onValueChanged.AddListener(OnVibrationChanged);
    }

    protected override void ShowHandle()
    {
        base.ShowHandle();
        RefreshControls();
    }

    protected override void CloseHandle()
    {
        base.CloseHandle();

        GameManager.Audio.Play(AudioDefine.SFXClose);

        _sliderMusic.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        _sliderSFX.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        _toggleVibra.onValueChanged.RemoveListener(OnVibrationChanged);
        if (_showBackButton)
        {
            _btnBack.onClick.RemoveListener(OnBackButtonClicked);
        }

        if (_showVoluntaryEndButton)
        {
            _btnOver.onClick.RemoveListener(OnOverButtonClicked);
        }

        if (!_closingForVoluntaryEnd && Launcher.Instance != null)
        {
            Launcher.Instance.ReturnToLauncherMenu();
        }
    }

    public override string GetPanelName()
    {
        return GlobalDefine.SettingView;
    }

    private static void ResolveOpenParam(
        OpenUIParam param,
        out bool showBackButton,
        out bool showVoluntaryEndButton,
        out bool showTips)
    {
        showBackButton = false;
        showVoluntaryEndButton = false;
        showTips = false;
        if (param?.data is SettingViewOpenParam openParam)
        {
            showBackButton = openParam.ShowBackButton;
            showVoluntaryEndButton = openParam.ShowVoluntaryEndButton;
            showTips = openParam.ShowTips;
            return;
        }

        if (param?.data is bool legacyShowInGamePanel && legacyShowInGamePanel)
        {
            showBackButton = true;
            showVoluntaryEndButton = true;
            showTips = true;
        }
    }

    private void RefreshControls()
    {
        if (GameManager.Audio == null)
        {
            return;
        }

        _sliderMusic.SetValueWithoutNotify(VolumeDbToSliderValue(GameManager.Audio.GetMusicVolume()));
        _sliderSFX.SetValueWithoutNotify(VolumeDbToSliderValue(GameManager.Audio.GetSFXVolume()));
        _toggleVibra.SetIsOnWithoutNotify(GameManager.Audio.IsVibrationEnabled());
    }

    private void OnMusicVolumeChanged(float value)
    {
        GameManager.Audio.SetMusicVolume(SliderValueToVolumeDb(value));
    }

    private void OnSFXVolumeChanged(float value)
    {
        GameManager.Audio.SetSFXVolume(SliderValueToVolumeDb(value));
    }

    private void OnVibrationChanged(bool enabled)
    {
        GameManager.Audio.SetVibrationEnabled(enabled);
    }

    private void OnBackButtonClicked()
    {
        UiButtonAudio.PlayClick();
        if (Launcher.Instance == null)
        {
            UIManager.GetInstance().ClosePanel(GetPanelName());
            return;
        }

        if (_showVoluntaryEndButton)
        {
            Launcher.Instance.SaveAndReturnToMainInterface();
            return;
        }

        UIManager.GetInstance().ClosePanel(GetPanelName());
    }

    private void OnOverButtonClicked()
    {
        UiButtonAudio.PlayClick();

        if (Launcher.Instance == null)
        {
            return;
        }

        UIManager.GetInstance().OpenPanel(
            GlobalDefine.CommonChoosePanel,
            UILayer.System,
            new OpenUIParam
            {
                data = new CommonChooseData(
                    "主动结档",
                    "确认后将结算本局回忆点数并删除当前存档，是否继续?",
                    OnConfirmVoluntaryGameOver,
                    null,
                    "确认结档",
                    "取消")
            });
    }

    private void OnConfirmVoluntaryGameOver()
    {
        _closingForVoluntaryEnd = true;
        UIManager.GetInstance().ClosePanel(GlobalDefine.CommonChoosePanel);
        UIManager.GetInstance().ClosePanel(GetPanelName());
        Launcher.Instance.RequestVoluntaryGameOver();
    }

    private static float VolumeDbToSliderValue(float volumeDb)
    {
        return Mathf.Clamp01(Mathf.Pow(10f, volumeDb / 20f));
    }

    private static float SliderValueToVolumeDb(float sliderValue)
    {
        float linearVolume = Mathf.Clamp01(sliderValue);
        return linearVolume <= 0f
            ? MinVolumeDb
            : Mathf.Max(MinVolumeDb, 20f * Mathf.Log10(linearVolume));
    }
}
