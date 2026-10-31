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

    private bool _closingForVoluntaryEnd;

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);

        bool isShowBackButton = param != null && param.data is bool showBack && showBack;

        if (isShowBackButton)
        {
            _btnBack.gameObject.SetActive(true);
            _btnBack.onClick.AddListener(OnBackButtonClicked);

            _btnOver.gameObject.SetActive(true);
            _btnOver.onClick.AddListener(OnOverButtonClicked);
        }
        else
        {
            _btnBack.gameObject.SetActive(false);
            _btnOver.gameObject.SetActive(false);
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
        _btnBack.onClick.RemoveListener(OnBackButtonClicked);
        _btnOver.onClick.RemoveListener(OnOverButtonClicked);

        if (!_closingForVoluntaryEnd && Launcher.Instance != null)
        {
            Launcher.Instance.ReturnToLauncherMenu();
        }
    }

    public override string GetPanelName()
    {
        return GlobalDefine.SettingView;
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

        Launcher.Instance.SaveAndReturnToMainInterface();
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
