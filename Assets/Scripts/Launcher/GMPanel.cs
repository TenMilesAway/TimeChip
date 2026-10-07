using System;
using UnityEngine;

/// <summary>仅在 Launcher 开关启用时创建的运行时 GM 面板。</summary>
public sealed class GMPanel : MonoBehaviour
{
    private const int MaxGrantCount = 99999;

    private readonly Rect _buttonRect = new Rect(12f, 12f, 84f, 36f);
    private Rect _windowRect = new Rect(12f, 56f, 340f, 290f);

    private bool _isOpen;
    private string _itemIdInput = string.Empty;
    private string _countInput = "1";
    private string _healthInput = string.Empty;
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
                "GM 工具");
        }
    }

    private void DrawWindow(int windowId)
    {
        GUI.Label(new Rect(16f, 34f, 92f, 24f), "物品 / 属性 ID");
        _itemIdInput = GUI.TextField(
            new Rect(110f, 34f, 200f, 24f),
            _itemIdInput);

        GUI.Label(new Rect(16f, 68f, 92f, 24f), "发放数量");
        _countInput = GUI.TextField(
            new Rect(110f, 68f, 200f, 24f),
            _countInput);

        if (GUI.Button(new Rect(16f, 104f, 294f, 30f), "发放"))
        {
            TryGrantItem();
        }

        GUI.Label(new Rect(16f, 146f, 92f, 24f), "当前健康值");
        _healthInput = GUI.TextField(
            new Rect(110f, 146f, 200f, 24f),
            _healthInput);

        if (GUI.Button(new Rect(16f, 180f, 294f, 30f), "设置健康值"))
        {
            TrySetHealth();
        }

        GUI.Label(new Rect(16f, 218f, 294f, 48f), _message);
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
        if (itemConfig != null)
        {
            PlayerInfoManager.GetInstance().AddItem(itemId, count);
            _message = $"已获得 {itemConfig.Name} × {count}。";
            return;
        }

        cfg.Base baseConfig = tables.BaseTable.GetOrDefault(itemId);
        if (baseConfig == null || !TryGrantBaseProperty(itemId, count))
        {
            _message = $"未找到可发放的物品或属性 ID：{itemId}。";
            return;
        }

        _message = $"已获得 {baseConfig.Name} × {count}。";
    }

    private void TrySetHealth()
    {
        if (!int.TryParse(_healthInput, out int health) || health < 0)
        {
            _message = "请输入大于或等于 0 的健康值。";
            return;
        }

        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        playerInfoManager.SetHealth(health);
        _message = $"当前健康值已设为 {playerInfoManager.Health}。";
    }

    private static bool TryGrantBaseProperty(int basePropertyId, int count)
    {
        PlayerInfoManager playerInfoManager = PlayerInfoManager.GetInstance();
        switch (basePropertyId)
        {
            case BasePropertyId.SimulationCoin:
                playerInfoManager.AddSimulationCoins(count);
                return true;
            case BasePropertyId.TimeCoin:
                playerInfoManager.AddTimeCoins(count);
                return true;
            case BasePropertyId.Health:
                playerInfoManager.ChangeHealth(count);
                return true;
            case BasePropertyId.WheelCoin:
                playerInfoManager.AddWheelCoins(count);
                return true;
            case BasePropertyId.BoxCoin:
                playerInfoManager.AddBoxCoins(count);
                return true;
            case BasePropertyId.CardCoin:
                playerInfoManager.AddCardCoins(count);
                return true;
            default:
                return false;
        }
    }
}
