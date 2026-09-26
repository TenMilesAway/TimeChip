using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class GuideMask : MonoBehaviour
{
    private const int SystemSortingOrder = 999;

    private static readonly Color MaskColor = new Color(0f, 0f, 0f, 0.65f);

    private readonly List<Image> blockers = new List<Image>();

    private RectTransform maskRectTransform;
    private RectTransform targetRectTransform;
    private Text instructionText;
    private Button targetButton;
    private Action onCompleted;
    private bool isClosed;

    public static GuideMask Show(
        RectTransform target,
        string instruction,
        Action onCompleted = null)
    {
        Canvas canvas = target == null ? null : target.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("无法创建引导遮罩：未找到目标 Canvas。");
            return null;
        }

        GameObject maskObject = new GameObject(
            "Guide Mask",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(GraphicRaycaster),
            typeof(GuideMask));
        maskObject.transform.SetParent(canvas.transform, false);

        Canvas maskCanvas = maskObject.GetComponent<Canvas>();
        maskCanvas.overrideSorting = true;
        maskCanvas.sortingLayerName = "System";
        maskCanvas.sortingOrder = SystemSortingOrder;

        GuideMask guideMask = maskObject.GetComponent<GuideMask>();
        guideMask.Initialize(target, instruction, onCompleted);
        return guideMask;
    }

    public static GuideMask Show(
        Button targetButton,
        string instruction,
        Action onCompleted)
    {
        return Show(
            targetButton == null ? null : targetButton.transform as RectTransform,
            targetButton,
            instruction,
            onCompleted);
    }

    public static GuideMask Show(
        RectTransform target,
        Button completionButton,
        string instruction,
        Action onCompleted)
    {
        GuideMask guideMask = Show(target, instruction, onCompleted);
        if (guideMask == null)
        {
            return null;
        }

        if (completionButton == null)
        {
            Debug.LogError("无法创建引导遮罩：未找到完成引导的按钮。");
            Destroy(guideMask.gameObject);
            return null;
        }

        guideMask.targetButton = completionButton;
        completionButton.onClick.AddListener(guideMask.Close);
        return guideMask;
    }

    private void Initialize(RectTransform target, string instruction, Action completed)
    {
        targetRectTransform = target;
        onCompleted = completed;
        maskRectTransform = transform as RectTransform;
        StretchToParent(maskRectTransform);

        for (int i = 0; i < 4; i++)
        {
            blockers.Add(CreateBlocker());
        }

        instructionText = CreateInstructionText(instruction);
        UpdateBlockers();
    }

    private void LateUpdate()
    {
        if (targetRectTransform == null || !targetRectTransform.gameObject.activeInHierarchy)
        {
            Close();
            return;
        }

        UpdateBlockers();
    }

    private Image CreateBlocker()
    {
        GameObject blockerObject = new GameObject("Blocker", typeof(RectTransform), typeof(Image));
        blockerObject.transform.SetParent(transform, false);

        Image blocker = blockerObject.GetComponent<Image>();
        blocker.color = MaskColor;
        blocker.raycastTarget = true;
        return blocker;
    }

    private Text CreateInstructionText(string instruction)
    {
        if (string.IsNullOrWhiteSpace(instruction))
        {
            return null;
        }

        GameObject instructionObject = new GameObject(
            "Guide Instruction",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));
        instructionObject.transform.SetParent(transform, false);

        Text text = instructionObject.GetComponent<Text>();
        text.font = GetGuideFont();
        text.fontSize = 32;
        text.fontStyle = FontStyle.Normal;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.text = instruction;
        return text;
    }

    private Font GetGuideFont()
    {
        Text targetText = targetRectTransform.GetComponentInChildren<Text>(true);
        if (targetText != null && targetText.font != null)
        {
            return targetText.font;
        }

        Text canvasText = targetRectTransform.GetComponentInParent<Canvas>()
            .GetComponentInChildren<Text>(true);
        if (canvasText != null && canvasText.font != null)
        {
            return canvasText.font;
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private void UpdateBlockers()
    {
        Bounds targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
            maskRectTransform,
            targetRectTransform);
        Rect maskRect = maskRectTransform.rect;

        SetBlocker(
            blockers[0],
            new Rect(maskRect.xMin, maskRect.yMin, targetBounds.min.x - maskRect.xMin, maskRect.height));
        SetBlocker(
            blockers[1],
            new Rect(targetBounds.max.x, maskRect.yMin, maskRect.xMax - targetBounds.max.x, maskRect.height));
        SetBlocker(
            blockers[2],
            new Rect(targetBounds.min.x, targetBounds.max.y, targetBounds.size.x, maskRect.yMax - targetBounds.max.y));
        SetBlocker(
            blockers[3],
            new Rect(targetBounds.min.x, maskRect.yMin, targetBounds.size.x, targetBounds.min.y - maskRect.yMin));

        UpdateInstructionPosition(targetBounds, maskRect);
    }

    private void UpdateInstructionPosition(Bounds targetBounds, Rect maskRect)
    {
        if (instructionText == null)
        {
            return;
        }

        const float InstructionHeight = 42f;
        const float InstructionMargin = 12f;
        RectTransform instructionRectTransform = instructionText.rectTransform;
        float maxWidth = Mathf.Max(0f, maskRect.width - InstructionMargin * 2f);
        float instructionWidth = Mathf.Min(
            Mathf.Max(targetBounds.size.x, instructionText.preferredWidth + 24f),
            maxWidth);
        float minX = maskRect.xMin + InstructionMargin + instructionWidth * 0.5f;
        float maxX = maskRect.xMax - InstructionMargin - instructionWidth * 0.5f;
        float xPosition = Mathf.Clamp(targetBounds.center.x, minX, maxX);
        float minY = maskRect.yMin + InstructionMargin;
        float maxY = maskRect.yMax - InstructionMargin;
        bool placeBelow = targetBounds.max.y + InstructionMargin + InstructionHeight > maxY;

        if (placeBelow &&
            targetBounds.min.y - InstructionMargin - InstructionHeight < minY)
        {
            placeBelow = false;
        }

        instructionRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        instructionRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        instructionRectTransform.sizeDelta = new Vector2(instructionWidth, InstructionHeight);
        if (placeBelow)
        {
            instructionRectTransform.pivot = new Vector2(0.5f, 1f);
            instructionRectTransform.anchoredPosition = new Vector2(
                xPosition,
                Mathf.Clamp(
                    targetBounds.min.y - InstructionMargin,
                    minY + InstructionHeight,
                    maxY));
            return;
        }

        instructionRectTransform.pivot = new Vector2(0.5f, 0f);
        instructionRectTransform.anchoredPosition = new Vector2(
            xPosition,
            Mathf.Clamp(
                targetBounds.max.y + InstructionMargin,
                minY,
                maxY - InstructionHeight));
    }

    private static void SetBlocker(Image blocker, Rect rect)
    {
        bool isVisible = rect.width > 0f && rect.height > 0f;
        blocker.gameObject.SetActive(isVisible);
        if (!isVisible)
        {
            return;
        }

        RectTransform blockerRectTransform = blocker.rectTransform;
        blockerRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        blockerRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        blockerRectTransform.pivot = new Vector2(0.5f, 0.5f);
        blockerRectTransform.sizeDelta = rect.size;
        blockerRectTransform.anchoredPosition = rect.center;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    public void Close()
    {
        if (isClosed)
        {
            return;
        }

        isClosed = true;
        if (targetButton != null)
        {
            targetButton.onClick.RemoveListener(Close);
        }

        Action completed = onCompleted;
        onCompleted = null;
        Destroy(gameObject);
        completed?.Invoke();
    }
}
