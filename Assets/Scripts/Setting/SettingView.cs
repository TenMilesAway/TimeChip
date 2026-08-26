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

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);

        bool isShowBackButton = param != null && param.data is bool showBack && showBack;

        if (isShowBackButton)
        {
            _btnBack.gameObject.SetActive(true);
            _btnBack.onClick.AddListener(OnBackButtonClicked);
        }
        else
        {
            _btnBack.gameObject.SetActive(false);
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
        PlayerPrefs.Save();

        _sliderMusic.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        _sliderSFX.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        _toggleVibra.onValueChanged.RemoveListener(OnVibrationChanged);
        _btnBack.onClick.RemoveListener(OnBackButtonClicked);
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
        if (Launcher.Instance == null)
        {
            UIManager.GetInstance().ClosePanel(GetPanelName());
            return;
        }

        Launcher.Instance.SaveAndReturnToMainInterface();
    }

    private static float VolumeDbToSliderValue(float volumeDb)
    {
        return Mathf.InverseLerp(MinVolumeDb, MaxVolumeDb, volumeDb);
    }

    private static float SliderValueToVolumeDb(float sliderValue)
    {
        return Mathf.Lerp(MinVolumeDb, MaxVolumeDb, sliderValue);
    }
}
