using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>为 UI 按钮统一播放点击音效。</summary>
public static class UiButtonAudio
{
    public static void PlayClick()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
    }

    public static void AttachClickSound(Button button)
    {
        if (button == null || button.GetComponent<UIButtonClickSound>() != null)
        {
            return;
        }

        button.gameObject.AddComponent<UIButtonClickSound>();
    }

    public static void Bind(Button button, UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        AttachClickSound(button);
        button.onClick.AddListener(action);
    }
}
