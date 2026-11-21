using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CommonPermanentBuffListView : UIBasePanel
{
    private const float DefaultEntryFontSize = 20f;
    private const float DefaultEntryMinHeight = 32f;

    [SerializeField] private Text _txtTitle;
    [SerializeField] private Transform _entryParent;
    [SerializeField] private Button _btnClose;
    [SerializeField] private Text _entryTextTemplate;

    private readonly List<Text> _activeEntryTexts = new List<Text>();

    private void Awake()
    {
        if (_btnClose != null)
        {
            _btnClose.onClick.AddListener(ClosePanel);
        }

        EnsureEntryTextTemplate();
    }

    protected override void InitHandle(OpenUIParam param)
    {
        base.InitHandle(param);
        if (_txtTitle != null)
        {
            _txtTitle.text = "增益";
        }

        BuildEntries();
    }

    protected override void HideHandle()
    {
        ClearEntries();
        base.HideHandle();
    }

    protected override void OnDestroy()
    {
        if (_btnClose != null)
        {
            _btnClose.onClick.RemoveListener(ClosePanel);
        }

        ClearEntries();
        base.OnDestroy();
    }

    public override string GetPanelName()
    {
        return GlobalDefine.CommonPermanentBuffListView;
    }

    private void EnsureEntryTextTemplate()
    {
        if (_entryTextTemplate != null)
        {
            _entryTextTemplate.gameObject.SetActive(false);
            return;
        }

        if (_entryParent == null)
        {
            return;
        }

        GameObject templateObject = new GameObject("EntryTextTemplate", typeof(RectTransform));
        templateObject.transform.SetParent(_entryParent, false);
        templateObject.SetActive(false);

        Text text = templateObject.AddComponent<Text>();
        Text styleSource = _txtTitle;
        if (styleSource != null)
        {
            text.font = styleSource.font;
            text.color = styleSource.color;
            text.material = styleSource.material;
            text.supportRichText = styleSource.supportRichText;
        }

        text.fontSize = Mathf.RoundToInt(DefaultEntryFontSize);
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        RectTransform rectTransform = templateObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.sizeDelta = new Vector2(0f, DefaultEntryMinHeight);

        LayoutElement layoutElement = templateObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = DefaultEntryMinHeight;
        layoutElement.preferredHeight = DefaultEntryMinHeight;

        ApplyOutlineStyle(text, _txtTitle);

        _entryTextTemplate = text;
    }

    private static void ApplyOutlineStyle(Text targetText, Text styleSource)
    {
        if (targetText == null)
        {
            return;
        }

        Outline outline = targetText.GetComponent<Outline>();
        if (outline == null)
        {
            outline = targetText.gameObject.AddComponent<Outline>();
        }

        Outline sourceOutline = styleSource != null ? styleSource.GetComponent<Outline>() : null;
        if (sourceOutline != null)
        {
            outline.effectColor = sourceOutline.effectColor;
            outline.effectDistance = sourceOutline.effectDistance;
            outline.useGraphicAlpha = sourceOutline.useGraphicAlpha;
            return;
        }

        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = true;
    }

    private void BuildEntries()
    {
        ClearEntries();
        if (_entryParent == null)
        {
            Debug.LogError("CommonPermanentBuffListView 未配置 _entryParent。", this);
            return;
        }

        EnsureEntryTextTemplate();
        if (_entryTextTemplate == null)
        {
            Debug.LogError("CommonPermanentBuffListView 无法创建条目 Text 模板。", this);
            return;
        }

        List<ActiveBuffData> permanentBuffs = CollectPermanentActiveBuffs();
        if (permanentBuffs.Count == 0)
        {
            Text emptyEntry = CreateEntryText("当前没有永久增益");
            if (emptyEntry != null)
            {
                _activeEntryTexts.Add(emptyEntry);
            }

            return;
        }

        List<string> displayLines = PermanentBuffListPresentation.BuildStackedDisplayLines(permanentBuffs);
        if (displayLines.Count == 0)
        {
            string emptyText = permanentBuffs.Count > 0
                ? "暂无生效的永久增益"
                : "当前没有永久增益";
            Text emptyEntry = CreateEntryText(emptyText);
            if (emptyEntry != null)
            {
                _activeEntryTexts.Add(emptyEntry);
            }

            return;
        }

        for (int i = 0; i < displayLines.Count; i++)
        {
            Text entryText = CreateEntryText(displayLines[i]);
            if (entryText != null)
            {
                _activeEntryTexts.Add(entryText);
            }
        }
    }

    private Text CreateEntryText(string content)
    {
        Text entryText = Instantiate(_entryTextTemplate, _entryParent);
        entryText.gameObject.SetActive(true);
        entryText.text = content;
        return entryText;
    }

    private static List<ActiveBuffData> CollectPermanentActiveBuffs()
    {
        List<ActiveBuffData> result = new List<ActiveBuffData>();
        List<ActiveBuffData> activeBuffs = PlayerInfoManager.GetInstance().GetActiveBuffs();
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            ActiveBuffData activeBuff = activeBuffs[i];
            cfg.BuffConfig buffConfig = DataTableMananger.GetInstance().Tables.BuffConfigTable
                .GetOrDefault(activeBuff.buffId);
            if (!BuffSystem.IsPermanentActiveBuff(activeBuff, buffConfig))
            {
                continue;
            }

            result.Add(activeBuff);
        }

        result.Sort((left, right) => left.buffId.CompareTo(right.buffId));
        return result;
    }

    private void ClearEntries()
    {
        for (int i = 0; i < _activeEntryTexts.Count; i++)
        {
            Text entryText = _activeEntryTexts[i];
            if (entryText == null)
            {
                continue;
            }

            Destroy(entryText.gameObject);
        }

        _activeEntryTexts.Clear();
    }

    private void ClosePanel()
    {
        GameManager.Audio.Play(AudioDefine.SFXClose);
        UIManager.GetInstance().ClosePanel(GetPanelName());
    }
}