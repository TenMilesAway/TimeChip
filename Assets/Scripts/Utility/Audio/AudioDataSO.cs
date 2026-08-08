using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioDataSO", menuName = "AudioDataSO", order = 0)]
public class AudioDataSO : ScriptableObject
{
    public string desc = "此配置是自动生成的，请勿手动修改";
    [SerializeField] public List<AudioData> conf;
}

[Serializable]
public class AudioData
{
    public string key;       // key 值, 使用 AudioClip 的 name
    public string path;      // AudioClip 存储路径
    public bool loop;        // 是否循环播放
    public string mixerName; // AudioMixer Group 的名称
}

public enum AudioClipType
{
    SFXOpenPanel,
    SFXClosePanel,
    Spawn,
    FirstLevel,
}

public class AudioClipData
{
    public AudioClipType _type;
    public string _content;
}