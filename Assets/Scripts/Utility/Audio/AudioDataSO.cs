using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioDataSO", menuName = "AudioDataSO", order = 0)]
public class AudioDataSO : ScriptableObject
{
    public string desc = "���������Զ����ɵģ������ֶ��޸�";
    [SerializeField] public List<AudioData> conf;
}

[Serializable]
public class AudioData
{
    public string key;       // key ֵ, ʹ�� AudioClip �� name
    public string path;      // AudioClip �洢·��
    public bool loop;        // �Ƿ�ѭ������
    public string mixerName; // AudioMixer Group ������
}
