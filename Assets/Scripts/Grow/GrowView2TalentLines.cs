using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 根据 grow2 DAG 在 Content 坐标系下绘制 GrowItem2 之间的连线。
/// </summary>
[DisallowMultipleComponent]
public sealed class GrowView2TalentLines : MonoBehaviour
{
    private const string LineRootName = "TalentLines";

    [SerializeField] private float _lineThickness = 6f;
    [SerializeField] private float _nodeLinkRadius = 72f;
    [SerializeField] private Color _lineColor = new Color(0.42f, 0.52f, 0.34f, 0.75f);

    private static Sprite _solidLineSprite;

    private RectTransform _lineRoot;
    private readonly List<RectTransform> _lineRects = new List<RectTransform>();
    private readonly List<(int fromId, int toId)> _edgeBuffer = new List<(int fromId, int toId)>();

    public void Rebuild(Grow2TalentGraph graph, IReadOnlyDictionary<int, GrowItem2> items)
    {
        if (graph == null || items == null || items.Count == 0)
        {
            ClearLines();
            return;
        }

        RectTransform content = transform as RectTransform;
        if (content == null)
        {
            return;
        }

        EnsureLineRoot(content);
        ClearLines();
        Canvas.ForceUpdateCanvases();

        graph.CollectEdges(_edgeBuffer);
        for (int index = 0; index < _edgeBuffer.Count; index++)
        {
            (int fromId, int toId) = _edgeBuffer[index];
            if (!items.TryGetValue(fromId, out GrowItem2 fromItem) ||
                !items.TryGetValue(toId, out GrowItem2 toItem) ||
                fromItem == null ||
                toItem == null)
            {
                continue;
            }

            Vector2 fromCenter = GetCenterInSpace(fromItem.RectTransform, content);
            Vector2 toCenter = GetCenterInSpace(toItem.RectTransform, content);
            Vector2 fromPoint = GetOuterPoint(fromCenter, toCenter, _nodeLinkRadius);
            Vector2 toPoint = GetOuterPoint(toCenter, fromCenter, _nodeLinkRadius);
            CreateStraightLine(fromPoint, toPoint);
        }
    }

    private void EnsureLineRoot(RectTransform content)
    {
        if (_lineRoot != null)
        {
            _lineRoot.SetAsFirstSibling();
            return;
        }

        Transform existing = content.Find(LineRootName);
        if (existing != null)
        {
            _lineRoot = existing as RectTransform;
            _lineRoot.SetAsFirstSibling();
            return;
        }

        GameObject rootObject = new GameObject(LineRootName, typeof(RectTransform));
        _lineRoot = rootObject.GetComponent<RectTransform>();
        _lineRoot.SetParent(content, false);
        _lineRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _lineRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _lineRoot.pivot = new Vector2(0.5f, 0.5f);
        _lineRoot.anchoredPosition = Vector2.zero;
        _lineRoot.sizeDelta = Vector2.zero;
        _lineRoot.SetAsFirstSibling();
    }

    private void CreateStraightLine(Vector2 from, Vector2 to)
    {
        GameObject lineObject = new GameObject("Line", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lineObject.transform.SetParent(_lineRoot, false);

        Image image = lineObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = _lineColor;
        image.sprite = GetSolidLineSprite();

        RectTransform lineRect = lineObject.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.pivot = new Vector2(0.5f, 0.5f);
        LayoutLine(lineRect, from, to, _lineThickness);
        _lineRects.Add(lineRect);
    }

    private static void LayoutLine(RectTransform lineRect, Vector2 from, Vector2 to, float thickness)
    {
        Vector2 delta = to - from;
        float length = delta.magnitude;
        if (length < 0.5f)
        {
            lineRect.sizeDelta = Vector2.zero;
            return;
        }

        lineRect.anchoredPosition = from + delta * 0.5f;
        lineRect.sizeDelta = new Vector2(length, thickness);
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        lineRect.localEulerAngles = new Vector3(0f, 0f, angle);
    }

    private static Sprite GetSolidLineSprite()
    {
        if (_solidLineSprite != null)
        {
            return _solidLineSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply(false, true);

        _solidLineSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            100f);
        _solidLineSprite.hideFlags = HideFlags.HideAndDontSave;
        return _solidLineSprite;
    }

    private static Vector2 GetCenterInSpace(RectTransform target, RectTransform space)
    {
        Vector3 worldCenter = target.TransformPoint(target.rect.center);
        return space.InverseTransformPoint(worldCenter);
    }

    private static Vector2 GetOuterPoint(Vector2 fromCenter, Vector2 toCenter, float radius)
    {
        Vector2 direction = toCenter - fromCenter;
        if (direction.sqrMagnitude < 0.01f)
        {
            return fromCenter;
        }

        return fromCenter + direction.normalized * radius;
    }

    private void ClearLines()
    {
        for (int index = 0; index < _lineRects.Count; index++)
        {
            RectTransform lineRect = _lineRects[index];
            if (lineRect != null)
            {
                Destroy(lineRect.gameObject);
            }
        }

        _lineRects.Clear();
    }

    private void OnDestroy()
    {
        ClearLines();
    }
}
