using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家当前状态的数据模型，用于初始化、存档和生成只读快照
/// </summary>
[Serializable]
public sealed class PlayerInfoData
{
    /// <summary>玩家的当前年龄</summary>
    public int currentAge = 18;

    /// <summary>当前年份中的月份, 取值范围为 1 至 12</summary>
    public int currentMonth = 1;

    /// <summary>玩家的当前健康值</summary>
    public int health = 100;

    /// <summary>玩家的最大健康值</summary>
    public int maxHealth = 100;

    /// <summary>玩家持有的模拟币数量</summary>
    public int simulationCoins;

    /// <summary>玩家持有的时间币数量</summary>
    public int timeCoins;

    /// <summary>标识玩家在当前回合是否已经打工</summary>
    public bool workedThisTurn;

    /// <summary>玩家拥有的可叠加道具列表</summary>
    public List<PlayerInventoryItem> inventory = new List<PlayerInventoryItem>();

    /// <summary>玩家已解锁的家具配置 ID 列表</summary>
    public List<int> unlockedHomeIds = new List<int>();
}

/// <summary>
/// 玩家背包中的单种道具及其数量
/// </summary>
[Serializable]
public sealed class PlayerInventoryItem
{
    /// <summary>道具的配置 ID</summary>
    public int itemId;

    /// <summary>玩家持有该道具的数量</summary>
    public int amount;
}

/// <summary>
/// 玩家数据的单例管理器，是游戏逻辑与 UI 获取玩家状态的统一入口
/// </summary>
public class PlayerInfoManager : Singleton<PlayerInfoManager>
{
    /// <summary>每年的月份数量</summary>
    private const int MonthsPerYear = 12;

    /// <summary>当前由管理器维护的玩家数据</summary>
    private PlayerInfoData _data = new PlayerInfoData();

    /// <summary>获取玩家当前年龄</summary>
    public int CurrentAge { get { return _data.currentAge; } }

    /// <summary>获取当前月份, 范围为 1 至 12</summary>
    public int CurrentMonth { get { return _data.currentMonth; } }

    /// <summary>获取玩家当前健康值</summary>
    public int Health { get { return _data.health; } }

    /// <summary>获取玩家最大健康值</summary>
    public int MaxHealth { get { return _data.maxHealth; } }

    /// <summary>获取玩家当前持有的模拟币数量</summary>
    public int SimulationCoins { get { return _data.simulationCoins; } }

    /// <summary>获取玩家当前持有的时间币数量</summary>
    public int TimeCoins { get { return _data.timeCoins; } }

    /// <summary>获取玩家在本回合是否已经打工</summary>
    public bool WorkedThisTurn { get { return _data.workedThisTurn; } }

    /// <summary>玩家数据发生变化时触发, UI 可订阅此事件刷新界面</summary>
    public event Action<PlayerInfoManager> PlayerInfoChanged;

    /// <summary>回合推进完成时触发, 其他系统可订阅此事件执行回合状态重置</summary>
    public event Action TurnAdvanced;

    /// <summary>初始化玩家数据; 未提供初始数据时使用默认值</summary>
    /// <param name="initialData">用于初始化的数据, 会复制以防止外部直接修改内部状态</param>
    public void Init(PlayerInfoData initialData = null)
    {
        _data = initialData == null ? new PlayerInfoData() : CreateCopy(initialData);
        NormalizeData();
        NotifyPlayerInfoChanged();
    }

    /// <summary>获取独立的数据快照, 修改返回对象不会影响内部数据</summary>
    /// <returns>当前玩家数据的副本</returns>
    public PlayerInfoData GetSnapshot()
    {
        return CreateCopy(_data);
    }

    /// <summary>设置玩家年龄, 年龄不能小于零</summary>
    /// <param name="age">要设置的年龄</param>
    public void SetCurrentAge(int age)
    {
        SetValue(ref _data.currentAge, Mathf.Max(0, age));
    }

    /// <summary>设置当前月份, 输入值会限制在 1 至 12 之间</summary>
    /// <param name="month">要设置的月份</param>
    public void SetCurrentMonth(int month)
    {
        SetValue(ref _data.currentMonth, Mathf.Clamp(month, 1, MonthsPerYear));
    }

    /// <summary>设置最大健康值, 并同步限制当前健康值不超过新的上限</summary>
    /// <param name="maxHealth">要设置的最大健康值, 最小为 1</param>
    public void SetMaxHealth(int maxHealth)
    {
        maxHealth = Mathf.Max(1, maxHealth);
        if (_data.maxHealth == maxHealth && _data.health <= maxHealth)
        {
            return;
        }

        _data.maxHealth = maxHealth;
        _data.health = Mathf.Clamp(_data.health, 0, _data.maxHealth);
        NotifyPlayerInfoChanged();
    }

    /// <summary>按指定数值增减健康值, 结果限制在零与最大健康值之间</summary>
    /// <param name="amount">健康值变化量, 正数增加, 负数减少</param>
    public void ChangeHealth(int amount)
    {
        SetValue(ref _data.health, Mathf.Clamp(_data.health + amount, 0, _data.maxHealth));
    }

    /// <summary>按指定数值增减模拟币, 模拟币不会低于零</summary>
    /// <param name="amount">模拟币变化量, 正数增加, 负数减少</param>
    public void AddSimulationCoins(int amount)
    {
        SetValue(ref _data.simulationCoins, Mathf.Max(0, _data.simulationCoins + amount));
    }

    /// <summary>尝试消耗指定数量的模拟币</summary>
    /// <param name="amount">要消耗的模拟币数量, 必须为非负数</param>
    /// <returns>模拟币充足且成功扣除时返回 true, 否则返回 false</returns>
    public bool TrySpendSimulationCoins(int amount)
    {
        return TrySpendCoins(ref _data.simulationCoins, amount);
    }

    /// <summary>按指定数值增减时间币, 时间币不会低于零</summary>
    /// <param name="amount">时间币变化量, 正数增加, 负数减少</param>
    public void AddTimeCoins(int amount)
    {
        SetValue(ref _data.timeCoins, Mathf.Max(0, _data.timeCoins + amount));
    }

    /// <summary>尝试消耗指定数量的时间币</summary>
    /// <param name="amount">要消耗的时间币数量, 必须为非负数</param>
    /// <returns>时间币充足且成功扣除时返回 true, 否则返回 false</returns>
    public bool TrySpendTimeCoins(int amount)
    {
        return TrySpendCoins(ref _data.timeCoins, amount);
    }

    /// <summary>向背包添加道具; 相同道具会自动叠加数量</summary>
    /// <param name="itemId">要添加的道具 ID, 必须大于零</param>
    /// <param name="amount">要添加的数量, 必须大于零</param>
    public void AddItem(int itemId, int amount)
    {
        if (itemId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        for (int i = 0; i < _data.inventory.Count; i++)
        {
            PlayerInventoryItem item = _data.inventory[i];
            if (item.itemId != itemId)
            {
                continue;
            }

            item.amount += amount;
            NotifyPlayerInfoChanged();
            return;
        }

        _data.inventory.Add(new PlayerInventoryItem
        {
            itemId = itemId,
            amount = amount
        });
        NotifyPlayerInfoChanged();
    }

    /// <summary>获取背包中指定道具的持有数量。</summary>
    /// <param name="itemId">要查询的道具 ID。</param>
    /// <returns>指定道具的持有数量，未拥有时返回零。</returns>
    public int GetItemCount(int itemId)
    {
        for (int i = 0; i < _data.inventory.Count; i++)
        {
            PlayerInventoryItem item = _data.inventory[i];
            if (item.itemId == itemId)
            {
                return item.amount;
            }
        }

        return 0;
    }

    /// <summary>判断指定家具是否已解锁</summary>
    public bool IsHomeUnlocked(int homeId)
    {
        return homeId > 0 && _data.unlockedHomeIds.Contains(homeId);
    }

    /// <summary>解锁指定家具; 已解锁时不重复通知</summary>
    public bool UnlockHome(int homeId)
    {
        if (homeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(homeId));
        }

        if (_data.unlockedHomeIds.Contains(homeId))
        {
            return false;
        }

        _data.unlockedHomeIds.Add(homeId);
        NotifyPlayerInfoChanged();
        return true;
    }

    /// <summary>将本回合打工状态标记为已完成; 同一回合不能重复打工</summary>
    /// <returns>首次成功标记时返回 true, 已经打工时返回 false</returns>
    public bool TryMarkWorkedThisTurn()
    {
        if (_data.workedThisTurn)
        {
            return false;
        }

        _data.workedThisTurn = true;
        NotifyPlayerInfoChanged();
        return true;
    }

    /// <summary>推进至下一回合, 重置本回合状态并更新月份; 跨年时年龄增加一岁</summary>
    public void AdvanceTurn()
    {
        _data.workedThisTurn = false;
        _data.currentMonth++;

        if (_data.currentMonth > MonthsPerYear)
        {
            _data.currentMonth = 1;
            _data.currentAge++;
        }

        TurnAdvanced?.Invoke();
        NotifyPlayerInfoChanged();
    }

    /// <summary>当整数值发生变化时写入新值并通知订阅者</summary>
    /// <param name="target">要更新的目标数值</param>
    /// <param name="value">更新后的数值</param>
    private void SetValue(ref int target, int value)
    {
        if (target == value)
        {
            return;
        }

        target = value;
        NotifyPlayerInfoChanged();
    }

    /// <summary>尝试从指定货币余额中扣除数值</summary>
    /// <param name="coins">要扣除的货币余额</param>
    /// <param name="amount">要扣除的数量</param>
    /// <returns>扣除成功时返回 true, 否则返回 false</returns>
    private bool TrySpendCoins(ref int coins, int amount)
    {
        if (amount < 0 || coins < amount)
        {
            return false;
        }

        coins -= amount;
        NotifyPlayerInfoChanged();
        return true;
    }

    /// <summary>修正初始化数据中的无效值, 确保内部状态始终处于合法范围</summary>
    private void NormalizeData()
    {
        _data.currentAge = Mathf.Max(0, _data.currentAge);
        _data.currentMonth = Mathf.Clamp(_data.currentMonth, 1, MonthsPerYear);
        _data.maxHealth = Mathf.Max(1, _data.maxHealth);
        _data.health = Mathf.Clamp(_data.health, 0, _data.maxHealth);
        _data.simulationCoins = Mathf.Max(0, _data.simulationCoins);
        _data.timeCoins = Mathf.Max(0, _data.timeCoins);
        if (_data.inventory == null)
        {
            _data.inventory = new List<PlayerInventoryItem>();
        }

        _data.inventory.RemoveAll(item => item == null || item.itemId <= 0 || item.amount <= 0);

        if (_data.unlockedHomeIds == null)
        {
            _data.unlockedHomeIds = new List<int>();
        }

        _data.unlockedHomeIds.RemoveAll(homeId => homeId <= 0);
        _data.unlockedHomeIds.Sort();
        for (int i = _data.unlockedHomeIds.Count - 1; i > 0; i--)
        {
            if (_data.unlockedHomeIds[i] == _data.unlockedHomeIds[i - 1])
            {
                _data.unlockedHomeIds.RemoveAt(i);
            }
        }
    }

    /// <summary>通知所有订阅者玩家数据已更新</summary>
    private void NotifyPlayerInfoChanged()
    {
        PlayerInfoChanged?.Invoke(this);
    }

    /// <summary>创建玩家数据的独立副本</summary>
    /// <param name="source">要复制的源数据</param>
    /// <returns>与源数据内容相同的新对象</returns>
    private static PlayerInfoData CreateCopy(PlayerInfoData source)
    {
        return new PlayerInfoData
        {
            currentAge = source.currentAge,
            currentMonth = source.currentMonth,
            health = source.health,
            maxHealth = source.maxHealth,
            simulationCoins = source.simulationCoins,
            timeCoins = source.timeCoins,
            workedThisTurn = source.workedThisTurn,
            inventory = CreateInventoryCopy(source.inventory),
            unlockedHomeIds = CreateHomeIdCopy(source.unlockedHomeIds)
        };
    }

    /// <summary>创建背包数据的深拷贝, 避免快照被外部修改</summary>
    /// <param name="source">要复制的源背包列表</param>
    /// <returns>与源背包内容相同的新列表</returns>
    private static List<PlayerInventoryItem> CreateInventoryCopy(List<PlayerInventoryItem> source)
    {
        List<PlayerInventoryItem> copy = new List<PlayerInventoryItem>();
        if (source == null)
        {
            return copy;
        }

        for (int i = 0; i < source.Count; i++)
        {
            PlayerInventoryItem item = source[i];
            if (item == null)
            {
                continue;
            }

            copy.Add(new PlayerInventoryItem
            {
                itemId = item.itemId,
                amount = item.amount
            });
        }

        return copy;
    }

    /// <summary>复制已解锁家具 ID 列表, 避免快照修改内部数据</summary>
    private static List<int> CreateHomeIdCopy(List<int> source)
    {
        return source == null ? new List<int>() : new List<int>(source);
    }
}