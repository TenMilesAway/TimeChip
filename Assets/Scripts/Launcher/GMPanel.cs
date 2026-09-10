using System;
using UnityEngine;

/// <summary>仅在 Launcher 开关启用时创建的运行时 GM 面板。</summary>
public sealed class GMPanel : MonoBehaviour
{
    private const int MaxGrantCount = 99999;

    private readonly Rect _buttonRect = new Rect(12f, 12f, 84f, 36f);
    private Rect _windowRect = new Rect(12f, 56f, 340f, 190f);

    private bool _isOpen;
    private string _itemIdInput = string.Empty;
    private string _countInput = "1";
    private string _message = string.Empty;

    public static GMPanel Create()
    {
        GameObject panelObject = new GameObject(nameof(GMPanel));
        return panelObject.AddComponent<GMPanel>();
    }

    private void OnGUI()
    {
        if (GUI.Button(_buttonRect, "GM"))
        {
            _isOpen = !_isOpen;
        }

        if (_isOpen)
        {
            _windowRect = GUI.Window(
                GetInstanceID(),
                _windowRect,
                DrawWindow,
                "GM 物品发放");
        }
    }

    private void DrawWindow(int windowId)
    {
        GUI.Label(new Rect(16f, 34f, 92f, 24f), "物品 ID");
        _itemIdInput = GUI.TextField(
            new Rect(110f, 34f, 200f, 24f),
            _itemIdInput);

        GUI.Label(new Rect(16f, 68f, 92f, 24f), "发放数量");
        _countInput = GUI.TextField(
            new Rect(110f, 68f, 200f, 24f),
            _countInput);

        if (GUI.Button(new Rect(16f, 104f, 294f, 30f), "发放物品"))
        {
            TryGrantItem();
        }

        GUI.Label(new Rect(16f, 142f, 294f, 32f), _message);
        GUI.DragWindow(new Rect(0f, 0f, 340f, 24f));
    }

    private void TryGrantItem()
    {
        if (!int.TryParse(_itemIdInput, out int itemId) || itemId <= 0)
        {
            _message = "请输入有效的物品 ID。";
            return;
        }

        if (!int.TryParse(_countInput, out int count) ||
            count <= 0 ||
            count > MaxGrantCount)
        {
            _message = $"数量必须在 1 到 {MaxGrantCount} 之间。";
            return;
        }

        cfg.Tables tables = DataTableMananger.GetInstance().Tables;
        if (tables == null)
        {
            _message = "数据表尚未初始化。";
            return;
        }

        cfg.Item itemConfig = tables.ItemTable.GetOrDefault(itemId);
        if (itemConfig == null)
        {
            _message = $"未找到物品 ID：{itemId}。";
            return;
        }

        PlayerInfoManager.GetInstance().AddItem(itemId, count);
        _message = $"已获得 {itemConfig.Name} × {count}。";
    }
}
