using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using DS.Enumerations;

public sealed class MissionDialogueLineData
{
    public DSDialogueSpeaker speaker;
    public string expressionPath;
    public string text;
}

public sealed class MissionDialogueViewData
{
    public string title;
    public List<MissionDialogueLineData> lines;
}

public class DialogueView : UIBasePanel
{
    private const string MeAvatarPath = "Assets/Art/Role/SpriteAtlas.spriteatlasv2[role_me]";
    private const string GirlfriendAvatarPath = "Assets/Art/Role/SpriteAtlas.spriteatlasv2[role_girlfriend]";
    private const string DaughterAvatarPath = "Assets/Art/Role/SpriteAtlas.spriteatlasv2[role_daughter]";
    private const string RoleSpriteAtlasPath = "Assets/Art/Role/SpriteAtlas.spriteatlasv2";

    [SerializeField] private Text _txtName;
    [SerializeField] private Text _txtDialogue;
    [SerializeField] private Image _imgAvatar;
    [SerializeField] private Button _btnShowOrNext;
    [SerializeField, Min(0.01f)] private float _typewriterSecondsPerChar = 0.05f;

    private MissionDialogueViewData _viewData;
    private int _currentIndex;
    private int _presentationVersion;
    private Tween _typewriterTween;
    private bool _isTyping;
    private string _currentLineText;

    private void Awake()
    {
        EnsureReferences();
        if (_btnShowOrNext != null)
        {
            _btnShowOrNext.onClick.AddListener(ShowNextLine);
        }
    }

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);
        EnsureReferences();
        _presentationVersion++;

        if (!(param?.data is MissionDialogueViewData viewData) ||
            viewData.lines == null ||
            viewData.lines.Count == 0)
        {
            Debug.LogWarning("[对话系统] 对话数据为空，已关闭对话面板。", this);
            UIManager.GetInstance().ClosePanel(GetPanelName());
            return;
        }

        _viewData = viewData;
        _currentIndex = 0;

        ShowCurrentLine();
    }

    protected override void OnDestroy()
    {
        StopTypewriter();

        if (_btnShowOrNext != null)
        {
            _btnShowOrNext.onClick.RemoveListener(ShowNextLine);
        }

        base.OnDestroy();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.DialogueView;
    }

    private void ShowNextLine()
    {
        GameManager.Audio.Play(AudioDefine.SFXClick);
        if (_viewData == null || _viewData.lines == null || _viewData.lines.Count == 0)
        {
            UIManager.GetInstance().ClosePanel(GetPanelName());
            return;
        }

        if (_isTyping)
        {
            CompleteCurrentTypewriterLine();
            return;
        }

        _currentIndex++;
        if (_currentIndex >= _viewData.lines.Count)
        {
            UIManager.GetInstance().ClosePanel(GetPanelName());
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (_txtDialogue == null || _viewData == null || _viewData.lines == null)
        {
            return;
        }

        MissionDialogueLineData lineData = _viewData.lines[_currentIndex];
        _currentLineText = lineData?.text ?? string.Empty;
        PlayTypewriter(_currentLineText);

        if (_txtName != null)
        {
            _txtName.text = GetSpeakerName(lineData?.speaker ?? DSDialogueSpeaker.Me);
        }

        SetAvatarAsync(
            lineData?.speaker ?? DSDialogueSpeaker.Me,
            lineData?.expressionPath,
            _presentationVersion);
    }

    private void EnsureReferences()
    {
        if (_txtName == null)
        {
            _txtName = FindInChildren<Text>("Text Name");
        }

        if (_txtDialogue == null)
        {
            _txtDialogue = FindInChildren<Text>("Text Dialogue");
        }

        if (_imgAvatar == null)
        {
            _imgAvatar = FindInChildren<Image>("Image Avatar");
        }

        if (_btnShowOrNext == null)
        {
            _btnShowOrNext = FindInChildren<Button>("Next Dialogue Button");
        }
    }

    private T FindInChildren<T>(string nodeName) where T : Component
    {
        Transform node = FindChildByName(transform, nodeName);
        return node == null ? null : node.GetComponent<T>();
    }

    private static Transform FindChildByName(Transform root, string nodeName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == nodeName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = FindChildByName(root.GetChild(i), nodeName);
            if (child != null)
            {
                return child;
            }
        }

        return null;
    }

    private static string GetSpeakerName(DSDialogueSpeaker speaker)
    {
        switch (speaker)
        {
            case DSDialogueSpeaker.Girlfriend:
                return "女朋友";
            case DSDialogueSpeaker.Daughter:
                return "女儿";
            default:
                return "我";
        }
    }

    private async void SetAvatarAsync(
        DSDialogueSpeaker speaker,
        string expressionPath,
        int presentationVersion)
    {
        if (_imgAvatar == null)
        {
            return;
        }

        string avatarPath = string.IsNullOrEmpty(expressionPath)
            ? GetAvatarPath(speaker)
            : NormalizeExpressionReference(expressionPath);
        Sprite avatar = await GameManager.Resource.LoadResource<Sprite>(
            avatarPath,
            GetInstanceID().ToString());
        if (presentationVersion != _presentationVersion || !isActiveAndEnabled)
        {
            return;
        }

        if (avatar == null)
        {
            Debug.LogError($"[对话系统] 头像加载失败: [{avatarPath}]");
            return;
        }

        _imgAvatar.sprite = avatar;
    }

    private void PlayTypewriter(string fullText)
    {
        StopTypewriter();

        if (_txtDialogue == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(fullText))
        {
            _txtDialogue.text = string.Empty;
            _isTyping = false;
            return;
        }

        _txtDialogue.text = string.Empty;
        _isTyping = true;

        int characterCount = fullText.Length;
        float duration = Mathf.Max(0.01f, _typewriterSecondsPerChar * characterCount);
        int visibleCharacters = 0;

        _typewriterTween = DOTween
            .To(() => visibleCharacters, value =>
            {
                visibleCharacters = value;
                _txtDialogue.text = fullText.Substring(0, Mathf.Clamp(value, 0, characterCount));
            }, characterCount, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                _txtDialogue.text = fullText;
                _isTyping = false;
                _typewriterTween = null;
            });
    }

    private void CompleteCurrentTypewriterLine()
    {
        StopTypewriter();

        if (_txtDialogue != null)
        {
            _txtDialogue.text = _currentLineText ?? string.Empty;
        }
    }

    private void StopTypewriter()
    {
        if (_typewriterTween != null)
        {
            _typewriterTween.Kill();
            _typewriterTween = null;
        }

        _isTyping = false;
    }

    private static string GetAvatarPath(DSDialogueSpeaker speaker)
    {
        switch (speaker)
        {
            case DSDialogueSpeaker.Girlfriend:
                return GirlfriendAvatarPath;
            case DSDialogueSpeaker.Daughter:
                return DaughterAvatarPath;
            default:
                return MeAvatarPath;
        }
    }

    private static string NormalizeExpressionReference(string expressionPath)
    {
        if (expressionPath.Contains(".spriteatlasv2[") && expressionPath.EndsWith("]"))
        {
            return expressionPath;
        }

        string spriteName = Path.GetFileNameWithoutExtension(expressionPath);
        return string.IsNullOrEmpty(spriteName)
            ? expressionPath
            : $"{RoleSpriteAtlasPath}[{spriteName}]";
    }
}
