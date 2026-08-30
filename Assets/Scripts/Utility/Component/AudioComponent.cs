using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

public class AudioComponent : BaseComponent
{
    public AudioDataSO _audioDataSO;
    public AudioMixer _audioMixer;

    private readonly Dictionary<string, AudioData> _audioDic = new Dictionary<string, AudioData>();
    private readonly Dictionary<string, List<AudioSource>> _playingAudioSources =
        new Dictionary<string, List<AudioSource>>();

    private static string _MusicVolumeKey = "MusicVolume";
    private static string _SFXVolumeKey = "SFXVolume";
    private static string _MusicKey = "Music";
    private static string _SFXKey = "SFX";
    private static string _VibrationKey = "Vibration";
    private static string _AudioSourceTemplatePath = "Assets/Audio/AudioSourceTemplate.prefab";

    private string _currentPlayMusic;
    private bool _isPlaySFX;
    private bool _isPlayMusic;
    private bool _isVibrationEnabled;

    private void LateUpdate()
    {
        RecycleStoppedSFX();
    }

    public void Init()
    {
        _audioDic.Clear();

        if (_audioDataSO)
        {
            foreach (var data in _audioDataSO.conf)
            {
                _audioDic.Add(data.key, data);
            }
        }

        // 加载音量
        LoadMusicVolumeSetting();
        LoadSFXVolumeSetting();
        LoadSettingStatus();
        LoadVibrationSetting();

        _ = PlayMusic(AudioDefine.Community);
    }

    #region 主要方法: 初始加载
    /// <summary>
    /// 加载背景音乐音量
    /// </summary>
    private void LoadMusicVolumeSetting()
    {
        float volume = 0;

        if (PlayerPrefs.HasKey(_MusicVolumeKey))
        {
            volume = PlayerPrefs.GetFloat(_MusicVolumeKey);
        }

        _audioMixer.SetFloat(_MusicVolumeKey, volume);
    }

    /// <summary>
    /// 加载音效音量
    /// </summary>
    private void LoadSFXVolumeSetting()
    {
        float volume = 0;

        if (PlayerPrefs.HasKey(_SFXVolumeKey))
        {
            volume = PlayerPrefs.GetFloat(_SFXVolumeKey);
        }

        _audioMixer.SetFloat(_SFXVolumeKey, volume);
    }

    /// <summary>
    /// 加载音乐、音效的开启
    /// </summary>
    private void LoadSettingStatus()
    {
        // 0 为关闭，1 为开启
        // 音乐
        if (PlayerPrefs.HasKey(_MusicKey))
        {
            _isPlayMusic = PlayerPrefs.GetInt(_MusicKey) == 1;
        }
        else
        {
            _isPlayMusic = true;
            PlayerPrefs.SetInt(_MusicKey, 1);
        }

        // 音效
        if (PlayerPrefs.HasKey(_SFXKey))
        {
            _isPlaySFX = PlayerPrefs.GetInt(_SFXKey) == 1;
        }
        else
        {
            _isPlaySFX = true;
            PlayerPrefs.SetInt(_SFXKey, 1);
        }
    }

    private void LoadVibrationSetting()
    {
        if (PlayerPrefs.HasKey(_VibrationKey))
        {
            _isVibrationEnabled = PlayerPrefs.GetInt(_VibrationKey) == 1;
        }
        else
        {
            _isVibrationEnabled = true;
            PlayerPrefs.SetInt(_VibrationKey, 1);
        }
    }
    #endregion

    #region 主要方法: 播放
    /// <summary>
    /// Plays an audio entry defined by <see cref="AudioDefine"/>. The entry's loop
    /// configuration determines whether it is played as music or as a sound effect.
    /// </summary>
    public Task<AudioSource> Play(string key, Transform parent = null)
    {
        if (!_audioDic.TryGetValue(key, out AudioData data))
        {
            Debug.LogErrorFormat("音频配置不存在: {0}", key);
            return Task.FromResult<AudioSource>(null);
        }

        return data.loop ? PlayMusic(key, parent) : PlaySFX(key, parent);
    }

    /// <summary>
    /// Plays loop-configured background music and stops the current track.
    /// </summary>
    public async Task<AudioSource> PlayMusic(string key, Transform parent = null)
    {
        if (!_isPlayMusic || !_audioDic.TryGetValue(key, out AudioData data) || !data.loop)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(_currentPlayMusic))
        {
            RecycleAudioHandle(_currentPlayMusic);
        }

        return await PlayAudioClip(key, parent);
    }

    /// <summary>
    /// Plays a non-loop-configured sound effect.
    /// </summary>
    public Task<AudioSource> PlaySFX(string key, Transform parent = null)
    {
        if (!_isPlaySFX || !_audioDic.TryGetValue(key, out AudioData data) || data.loop)
        {
            return Task.FromResult<AudioSource>(null);
        }

        return PlayAudioClip(key, parent);
    }

    public async Task<AudioSource> PlayAudioClip(string key, Transform parent = null)
    {
        if (_audioDic.TryGetValue(key, out AudioData data))
        {
            AudioClip clip = await GameManager.Resource.LoadResource<AudioClip>(data.path, data.path);
            if (clip != null)
            {

                GameObject audioSourceGO = await UnityObjectPoolFactory.GetInstance().GetItem<GameObject>(_AudioSourceTemplatePath, GetInstanceID().ToString());

                if (audioSourceGO == null)
                {
                    Debug.LogErrorFormat("加载失败或 {0} 不存在", _AudioSourceTemplatePath);
                    return null;
                }

                AudioSource audioSource = audioSourceGO.GetComponent<AudioSource>();

                if (parent != null)
                {
                    audioSource.transform.SetParent(parent, false);
                }
                else
                {
                    audioSource.transform.SetParent(this.transform, false);
                }

                audioSource.clip = clip;
                audioSource.loop = data.loop;
                AudioMixerGroup[] groups = _audioMixer.FindMatchingGroups(data.mixerName);
                audioSource.outputAudioMixerGroup = groups.Length > 0 ? groups[0] : null;

                if (audioSource.outputAudioMixerGroup == null)
                {
                    Debug.LogErrorFormat("{0} 不存在", data.mixerName);
                    ReturnAudioSource(audioSource);
                    return null;
                }

                audioSource.transform.localPosition = Vector3.zero;
                audioSource.gameObject.SetActive(true);
                audioSource.Play();
                TrackAudioSource(key, audioSource);
                if (data.loop)
                {
                    _currentPlayMusic = key;
                }

                return audioSource;
            }
        }

        return null;
    }
    #endregion

    #region 辅助方法: 音量设置
    /// <summary>
    /// 获得背景音乐的音量
    /// </summary>
    public float GetMusicVolume()
    {
        float volume = 0;
        _audioMixer.GetFloat(_MusicVolumeKey, out volume);
        return volume;
    }

    /// <summary>
    /// 设置背景音乐的音量
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        _audioMixer.SetFloat(_MusicVolumeKey, volume);
        PlayerPrefs.SetFloat(_MusicVolumeKey, volume);
    }

    /// <summary>
    /// 获得背景音效的音量
    /// </summary>
    public float GetSFXVolume()
    {
        float volume = 0;
        _audioMixer.GetFloat(_SFXVolumeKey, out volume);
        return volume;
    }

    public void SetSFXVolume(float volume)
    {
        _audioMixer.SetFloat(_SFXVolumeKey, volume);
        PlayerPrefs.SetFloat(_SFXVolumeKey, volume);
    }

    public bool IsMusicEnabled()
    {
        return _isPlayMusic;
    }

    public void SetMusicEnabled(bool enabled)
    {
        _isPlayMusic = enabled;
        PlayerPrefs.SetInt(_MusicKey, enabled ? 1 : 0);

        if (!enabled && !string.IsNullOrEmpty(_currentPlayMusic))
        {
            RecycleAudioHandle(_currentPlayMusic);
        }
    }

    public bool IsSFXEnabled()
    {
        return _isPlaySFX;
    }

    public void SetSFXEnabled(bool enabled)
    {
        _isPlaySFX = enabled;
        PlayerPrefs.SetInt(_SFXKey, enabled ? 1 : 0);
    }
    #endregion

    #region Vibration
    public bool IsVibrationEnabled()
    {
        return _isVibrationEnabled;
    }

    public void SetVibrationEnabled(bool enabled)
    {
        _isVibrationEnabled = enabled;
        PlayerPrefs.SetInt(_VibrationKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void Vibrate()
    {
        if (!_isVibrationEnabled)
        {
            return;
        }

#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }
    #endregion

    #region 辅助方法: 回收音频
    /// <summary>
    /// 回收指定 key 的音频
    /// </summary>
    public void RecycleAudio(string key)
    {
        RecycleAudioHandle(key);
    }

    private void RecycleAudioHandle(string key)
    {
        if (!_playingAudioSources.TryGetValue(key, out List<AudioSource> audioSources))
        {
            return;
        }

        for (int i = audioSources.Count - 1; i >= 0; i--)
        {
            RecycleAudioSource(key, audioSources[i]);
        }
    }

    private void TrackAudioSource(string key, AudioSource audioSource)
    {
        if (!_playingAudioSources.TryGetValue(key, out List<AudioSource> audioSources))
        {
            audioSources = new List<AudioSource>();
            _playingAudioSources.Add(key, audioSources);
        }

        audioSources.Add(audioSource);
    }

    private void RecycleAudioSource(string key, AudioSource audioSource)
    {
        if (!_playingAudioSources.TryGetValue(key, out List<AudioSource> audioSources) ||
            !audioSources.Remove(audioSource))
        {
            return;
        }

        ReturnAudioSource(audioSource);

        if (audioSources.Count == 0)
        {
            _playingAudioSources.Remove(key);
        }
    }

    private void RecycleStoppedSFX()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            AudioSource audioSource = transform.GetChild(i).GetComponent<AudioSource>();
            if (audioSource == null || audioSource.loop || audioSource.isPlaying)
            {
                continue;
            }

            UntrackAudioSource(audioSource);
            ReturnAudioSource(audioSource);
        }
    }

    private void UntrackAudioSource(AudioSource audioSource)
    {
        string emptyKey = null;

        foreach (KeyValuePair<string, List<AudioSource>> pair in _playingAudioSources)
        {
            if (!pair.Value.Remove(audioSource))
            {
                continue;
            }

            if (pair.Value.Count == 0)
            {
                emptyKey = pair.Key;
            }

            break;
        }

        if (emptyKey != null)
        {
            _playingAudioSources.Remove(emptyKey);
        }
    }

    private void ReturnAudioSource(AudioSource audioSource)
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.Stop();
        audioSource.clip = null;
        audioSource.outputAudioMixerGroup = null;
        UnityObjectPoolFactory.GetInstance().PutItem(_AudioSourceTemplatePath, audioSource.gameObject);
    }
    #endregion

}