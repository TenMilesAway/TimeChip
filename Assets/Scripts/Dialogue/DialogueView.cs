using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DS.Enumerations;

public sealed class MissionDialogueLineData
{
    public DSDialogueSpeaker speaker;
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

    [SerializeField] private Text _txtName;
    [SerializeField] private Text _txtDialogue;
    [SerializeField] private Image _imgAvatar;
    [SerializeField] private Button _btnShowOrNext;

    private MissionDialogueViewData _viewData;
    private int _currentIndex;
    private int _presentationVersion;

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
        if (_viewData == null || _viewData.lines == null || _viewData.lines.Count == 0)
        {
            UIManager.GetInstance().ClosePanel(GetPanelName());
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
        _txtDialogue.text = lineData?.text ?? string.Empty;

        if (_txtName != null)
        {
            _txtName.text = GetSpeakerName(lineData?.speaker ?? DSDialogueSpeaker.Me);
        }

        SetAvatarAsync(lineData?.speaker ?? DSDialogueSpeaker.Me, _presentationVersion);
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

    private async void SetAvatarAsync(DSDialogueSpeaker speaker, int presentationVersion)
    {
        if (_imgAvatar == null)
        {
            return;
        }

        string avatarPath = GetAvatarPath(speaker);
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
}
