using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂在 <see cref="Button"/> 上，在点击时播放统一 UI Click 音效。
/// 业务逻辑里不要再手动调用 <see cref="UiButtonAudio.PlayClick"/>，避免重复播放。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class UIButtonClickSound : MonoBehaviour
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(UiButtonAudio.PlayClick);
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(UiButtonAudio.PlayClick);
        }
    }
}
