using RedSaw.MissionSystem;
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

    /// <summary>当前生效的 BUFF 实例</summary>
    public List<ActiveBuffData> activeBuffs = new List<ActiveBuffData>();

    /// <summary>玩家当前进行中的任务及其条件进度</summary>
    public List<PlayerMissionData> activeMissions = new List<PlayerMissionData>();

    /// <summary>玩家已经完成的任务 ID</summary>
    public List<string> completedMissionIds = new List<string>();

    /// <summary>各零工类型的等级与经验</summary>
    public List<PlayerWorkProgress> workProgresses = new List<PlayerWorkProgress>();

    /// <summary>便利店商品上次刷新的年龄</summary>
    public int convenienceOfferAge = -1;

    /// <summary>便利店商品上次刷新的月份</summary>
    public int convenienceOfferMonth = -1;

    /// <summary>当月便利店商品及其剩余购买次数</summary>
    public List<PlayerConvenienceOffer> convenienceOffers = new List<PlayerConvenienceOffer>();
}

/// <summary>
/// 玩家单个零工类型的等级与经验存档数据
/// </summary>
[Serializable]
public sealed class PlayerWorkProgress
{
    public string workType;
    public int level = 1;
    public int experience;
}

/// <summary>
/// 玩家当月单个便利店商品的购买状态
/// </summary>
[Serializable]
public sealed class PlayerConvenienceOffer
{
    public int convenienceId;
    public int remainingCount;
}

public enum ConveniencePurchaseResult
{
    Success,
    SoldOut,
    InsufficientCoins,
    InvalidOffer
}

/// <summary>
/// 玩家当前进行中的单个任务存档数据
/// </summary>
[Serializable]
public sealed class PlayerMissionData
{
    /// <summary>任务原型的唯一标识</summary>
    public string missionId;

    /// <summary>按任务条件定义顺序保存的完成进度</summary>
    public List<int> requirementProgress = new List<int>();

    /// <summary>任务开始时的年龄</summary>
    public int startedAge;

    /// <summary>任务开始时的月份</summary>
    public int startedMonth;

    /// <summary>任务截止时的年龄，零表示没有截止日期</summary>
    public int deadlineAge;

    /// <summary>任务截止时的月份，零表示没有截止日期</summary>
    public int deadlineMonth;
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
    public const int MaxWorkLevel = 5;
    private static readonly int[] WorkLevelExperienceRequirements = { 100, 240, 540, 1100 };

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

    /// <summary>已购家具提供的满意度总和</summary>
    public float Satisfaction
    {
        get
        {
            float satisfaction = 0f;
            cfg.Tables tables = DataTableMananger.GetInstance().Tables;
            if (tables == null)
            {
                return satisfaction;
            }

            for (int i = 0; i < _data.unlockedHomeIds.Count; i++)
            {
                cfg.Home home = tables.HomeTable.GetOrDefault(_data.unlockedHomeIds[i]);
                if (home != null)
                {
                    satisfaction += home.Satisfaction;
                }
            }

            return satisfaction;
        }
    }

    /// <summary>玩家数据发生变化时触发, UI 可订阅此事件刷新界面</summary>
    public event Action<PlayerInfoManager> PlayerInfoChanged;

    /// <summary>回合推进完成时触发, 其他系统可订阅此事件执行回合状态重置</summary>
    public event Action TurnAdvanced;

    /// <summary>当前回合结束、月份切换前触发，用于结算回合末效果。</summary>
    public event Action TurnEnding;

    /// <summary>初始化玩家数据; 未提供初始数据时使用默认值</summary>
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

    /// <summary>
    /// 更新当前进行中的任务存档数据
    /// </summary>
    /// <param name="missions">需要保存的任务状态</param>
    public void SetActiveMissions(List<PlayerMissionData> missions)
    {
        _data.activeMissions = CreateMissionCopy(missions);
        NotifyPlayerInfoChanged();
    }

    /// <summary>更新已完成任务记录。</summary>
    public void SetCompletedMissionIds(List<string> missionIds)
    {
        _data.completedMissionIds = CreateMissionIdCopy(missionIds);
        NotifyPlayerInfoChanged();
    }

    /// <summary>获取激活 BUFF 的独立副本。</summary>
    public List<ActiveBuffData> GetActiveBuffs()
    {
        return CreateActiveBuffCopy(_data.activeBuffs);
    }

    /// <summary>更新激活 BUFF 列表。</summary>
    public void SetActiveBuffs(List<ActiveBuffData> activeBuffs)
    {
        _data.activeBuffs = CreateActiveBuffCopy(activeBuffs);
        NotifyPlayerInfoChanged();
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
        int health = Mathf.Clamp(_data.health + amount, 0, _data.maxHealth);
        if (_data.health == health)
        {
            return;
        }

        _data.health = health;
        NotifyPlayerInfoChanged();
        MissionAPI.Broadcast(new MissionMessage(MissionEventType.Health, _data.health));
    }

    /// <summary>按指定数值增减模拟币, 模拟币不会低于零</summary>
    /// <param name="amount">模拟币变化量, 正数增加, 负数减少</param>
    public void AddSimulationCoins(int amount)
    {
        SetValue(ref _data.simulationCoins, Mathf.Max(0, _data.simulationCoins + amount));
        if (amount > 0)
        {
            MissionAPI.Broadcast(new MissionMessage(MissionEventType.Coin, amount));
        }
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

    /// <summary>获取背包中指定道具的持有数量</summary>
    /// <param name="itemId">要查询的道具 ID</param>
    /// <returns>指定道具的持有数量, 未拥有时返回零</returns>
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

    /// <summary>尝试消耗背包中的指定道具数量。</summary>
    /// <returns>道具充足且成功消耗时返回 true，否则返回 false。</returns>
    public bool TryConsumeItem(int itemId, int amount = 1)
    {
        if (itemId <= 0 || amount <= 0)
        {
            return false;
        }

        for (int i = 0; i < _data.inventory.Count; i++)
        {
            PlayerInventoryItem item = _data.inventory[i];
            if (item.itemId != itemId || item.amount < amount)
            {
                continue;
            }

            item.amount -= amount;
            if (item.amount == 0)
            {
                _data.inventory.RemoveAt(i);
            }

            NotifyPlayerInfoChanged();
            return true;
        }

        return false;
    }

    /// <summary>判断当前月份是否已有指定数量的便利店商品。</summary>
    public bool HasMonthlyConvenienceOffers(int expectedCount)
    {
        return expectedCount > 0 &&
            _data.convenienceOfferAge == _data.currentAge &&
            _data.convenienceOfferMonth == _data.currentMonth &&
            _data.convenienceOffers != null &&
            _data.convenienceOffers.Count == expectedCount;
    }

    /// <summary>按本月抽取时的顺序获取指定槽位的便利店商品 ID。</summary>
    public int GetMonthlyConvenienceOfferIdAt(int index)
    {
        if (index < 0 || _data.convenienceOffers == null || index >= _data.convenienceOffers.Count)
        {
            return 0;
        }

        PlayerConvenienceOffer offer = _data.convenienceOffers[index];
        return offer == null ? 0 : offer.convenienceId;
    }

    /// <summary>保存当前月份随机生成的便利店商品。</summary>
    public void SetMonthlyConvenienceOffers(IReadOnlyList<cfg.Convenience> offers)
    {
        if (offers == null || offers.Count == 0)
        {
            throw new ArgumentException("便利店商品不能为空。", nameof(offers));
        }

        List<PlayerConvenienceOffer> monthlyOffers = new List<PlayerConvenienceOffer>(offers.Count);
        for (int i = 0; i < offers.Count; i++)
        {
            cfg.Convenience offer = offers[i];
            if (offer == null ||
                offer.Id <= 0 ||
                offer.Num <= 0 ||
                FindConvenienceOffer(monthlyOffers, offer.Id) != null)
            {
                throw new ArgumentException("便利店商品必须有不重复的有效库存。", nameof(offers));
            }

            monthlyOffers.Add(new PlayerConvenienceOffer
            {
                convenienceId = offer.Id,
                remainingCount = offer.Num
            });
        }

        _data.convenienceOfferAge = _data.currentAge;
        _data.convenienceOfferMonth = _data.currentMonth;
        _data.convenienceOffers = monthlyOffers;
        NotifyPlayerInfoChanged();
    }

    /// <summary>获取当月指定便利店商品的剩余购买次数。</summary>
    public int GetConvenienceOfferRemainingCount(int convenienceId)
    {
        PlayerConvenienceOffer offer = FindConvenienceOffer(_data.convenienceOffers, convenienceId);
        return offer == null ? 0 : offer.remainingCount;
    }

    /// <summary>判断指定商品是否属于本月便利店商品。</summary>
    public bool HasMonthlyConvenienceOffer(int convenienceId)
    {
        return FindConvenienceOffer(_data.convenienceOffers, convenienceId) != null;
    }

    /// <summary>尝试购买本月便利店商品，同时扣除模拟币、库存并发放物品。</summary>
    public ConveniencePurchaseResult TryPurchaseConvenienceOffer(cfg.Convenience offerConfig)
    {
        if (offerConfig == null ||
            !HasMonthlyConvenienceOffers(_data.convenienceOffers.Count))
        {
            return ConveniencePurchaseResult.InvalidOffer;
        }

        PlayerConvenienceOffer offer = FindConvenienceOffer(_data.convenienceOffers, offerConfig.Id);
        if (offer == null)
        {
            return ConveniencePurchaseResult.InvalidOffer;
        }

        if (offer.remainingCount <= 0)
        {
            return ConveniencePurchaseResult.SoldOut;
        }

        cfg.Item itemConfig = DataTableMananger.GetInstance().Tables.ItemTable
            .GetOrDefault(offerConfig.ItemId);
        cfg.Base baseConfig = DataTableMananger.GetInstance().Tables.BaseTable
            .GetOrDefault(offerConfig.ItemId);
        if (itemConfig == null && (baseConfig == null || !IsSupportedBaseProperty(baseConfig.Id)))
        {
            return ConveniencePurchaseResult.InvalidOffer;
        }

        if (offerConfig.Price < 0 || _data.simulationCoins < offerConfig.Price)
        {
            return ConveniencePurchaseResult.InsufficientCoins;
        }

        _data.simulationCoins -= offerConfig.Price;
        offer.remainingCount--;
        if (itemConfig != null)
        {
            AddInventoryItem(itemConfig.Id, 1);
        }
        else
        {
            AddBaseProperty(baseConfig.Id);
        }

        NotifyPlayerInfoChanged();
        return ConveniencePurchaseResult.Success;
    }

    /// <summary>获取指定零工类型的当前等级; 未进行过该零工时默认为一级。</summary>
    public int GetWorkLevel(string workType)
    {
        PlayerWorkProgress progress = FindWorkProgress(workType);
        return progress == null ? 1 : progress.level;
    }

    /// <summary>获取指定零工类型当前等级的经验。</summary>
    public int GetWorkExperience(string workType)
    {
        PlayerWorkProgress progress = FindWorkProgress(workType);
        return progress == null ? 0 : progress.experience;
    }

    /// <summary>获取指定零工等级升级所需的经验，满级时返回零。</summary>
    public static int GetWorkExperienceRequired(int level)
    {
        return level < 1 || level >= MaxWorkLevel
            ? 0
            : WorkLevelExperienceRequirements[level - 1];
    }

    /// <summary>增加指定零工类型的经验，并在达到各等级阈值时自动升级。</summary>
    public void AddWorkExperience(string workType, int amount)
    {
        if (string.IsNullOrWhiteSpace(workType))
        {
            throw new ArgumentException("零工类型不能为空。", nameof(workType));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        PlayerWorkProgress progress = FindWorkProgress(workType);
        if (progress == null)
        {
            progress = new PlayerWorkProgress { workType = workType };
            _data.workProgresses.Add(progress);
        }

        if (progress.level >= MaxWorkLevel)
        {
            return;
        }

        long totalExperience = (long)progress.experience + amount;
        while (progress.level < MaxWorkLevel)
        {
            int requiredExperience = GetWorkExperienceRequired(progress.level);
            if (totalExperience < requiredExperience)
            {
                progress.experience = (int)totalExperience;
                break;
            }

            totalExperience -= requiredExperience;
            progress.level++;
            if (progress.level >= MaxWorkLevel)
            {
                progress.experience = 0;
            }
        }

        NotifyPlayerInfoChanged();
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
        TurnEnding?.Invoke();
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

        if (_data.activeBuffs == null)
        {
            _data.activeBuffs = new List<ActiveBuffData>();
        }

        _data.activeBuffs.RemoveAll(buff => buff == null || buff.buffId <= 0 || buff.stacks <= 0 ||
            buff.remainingTurns == 0 || buff.remainingTurns < -1);

        if (_data.workProgresses == null)
        {
            _data.workProgresses = new List<PlayerWorkProgress>();
        }

        HashSet<string> workTypes = new HashSet<string>();
        for (int i = _data.workProgresses.Count - 1; i >= 0; i--)
        {
            PlayerWorkProgress progress = _data.workProgresses[i];
            if (progress == null || string.IsNullOrWhiteSpace(progress.workType) ||
                !workTypes.Add(progress.workType))
            {
                _data.workProgresses.RemoveAt(i);
                continue;
            }

            progress.level = Mathf.Clamp(progress.level, 1, MaxWorkLevel);
            int requiredExperience = GetWorkExperienceRequired(progress.level);
            progress.experience = requiredExperience == 0
                ? 0
                : Mathf.Clamp(progress.experience, 0, requiredExperience - 1);
        }

        if (_data.convenienceOffers == null)
        {
            _data.convenienceOffers = new List<PlayerConvenienceOffer>();
        }

        HashSet<int> convenienceIds = new HashSet<int>();
        for (int i = _data.convenienceOffers.Count - 1; i >= 0; i--)
        {
            PlayerConvenienceOffer offer = _data.convenienceOffers[i];
            if (offer == null ||
                offer.convenienceId <= 0 ||
                offer.remainingCount < 0 ||
                !convenienceIds.Add(offer.convenienceId))
            {
                _data.convenienceOffers.RemoveAt(i);
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
            unlockedHomeIds = CreateHomeIdCopy(source.unlockedHomeIds),
            activeBuffs = CreateActiveBuffCopy(source.activeBuffs),
            activeMissions = CreateMissionCopy(source.activeMissions),
            completedMissionIds = CreateMissionIdCopy(source.completedMissionIds),
            workProgresses = CreateWorkProgressCopy(source.workProgresses),
            convenienceOfferAge = source.convenienceOfferAge,
            convenienceOfferMonth = source.convenienceOfferMonth,
            convenienceOffers = CreateConvenienceOfferCopy(source.convenienceOffers)
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

    private static List<ActiveBuffData> CreateActiveBuffCopy(List<ActiveBuffData> source)
    {
        List<ActiveBuffData> copy = new List<ActiveBuffData>();
        if (source == null)
        {
            return copy;
        }

        for (int i = 0; i < source.Count; i++)
        {
            ActiveBuffData buff = source[i];
            if (buff != null)
            {
                copy.Add(new ActiveBuffData
                {
                    buffId = buff.buffId,
                    remainingTurns = buff.remainingTurns,
                    stacks = buff.stacks,
                    sourceId = buff.sourceId
                });
            }
        }

        return copy;
    }

    /// <summary>复制任务存档数据, 避免快照修改内部任务进度</summary>
    private static List<PlayerMissionData> CreateMissionCopy(List<PlayerMissionData> source)
    {
        List<PlayerMissionData> copy = new List<PlayerMissionData>();
        if (source == null)
        {
            return copy;
        }

        for (int i = 0; i < source.Count; i++)
        {
            PlayerMissionData mission = source[i];
            if (mission == null || string.IsNullOrEmpty(mission.missionId))
            {
                continue;
            }

            copy.Add(new PlayerMissionData
            {
                missionId = mission.missionId,
                requirementProgress = mission.requirementProgress == null
                    ? new List<int>()
                    : new List<int>(mission.requirementProgress),
                startedAge = mission.startedAge,
                startedMonth = mission.startedMonth,
                deadlineAge = mission.deadlineAge,
                deadlineMonth = mission.deadlineMonth
            });
        }

        return copy;
    }

    /// <summary>复制已完成任务 ID，过滤空值与重复项。</summary>
    private static List<string> CreateMissionIdCopy(List<string> source)
    {
        List<string> copy = new List<string>();
        if (source == null)
        {
            return copy;
        }

        for (int i = 0; i < source.Count; i++)
        {
            string missionId = source[i];
            if (!string.IsNullOrEmpty(missionId) && !copy.Contains(missionId))
            {
                copy.Add(missionId);
            }
        }

        return copy;
    }

    /// <summary>复制零工进度，避免快照修改内部数据。</summary>
    private static List<PlayerWorkProgress> CreateWorkProgressCopy(List<PlayerWorkProgress> source)
    {
        List<PlayerWorkProgress> copy = new List<PlayerWorkProgress>();
        if (source == null)
        {
            return copy;
        }

        for (int i = 0; i < source.Count; i++)
        {
            PlayerWorkProgress progress = source[i];
            if (progress != null)
            {
                copy.Add(new PlayerWorkProgress
                {
                    workType = progress.workType,
                    level = progress.level,
                    experience = progress.experience
                });
            }
        }

        return copy;
    }

    private static List<PlayerConvenienceOffer> CreateConvenienceOfferCopy(
        List<PlayerConvenienceOffer> source)
    {
        List<PlayerConvenienceOffer> copy = new List<PlayerConvenienceOffer>();
        if (source == null)
        {
            return copy;
        }

        for (int i = 0; i < source.Count; i++)
        {
            PlayerConvenienceOffer offer = source[i];
            if (offer != null)
            {
                copy.Add(new PlayerConvenienceOffer
                {
                    convenienceId = offer.convenienceId,
                    remainingCount = offer.remainingCount
                });
            }
        }

        return copy;
    }

    private static PlayerConvenienceOffer FindConvenienceOffer(
        List<PlayerConvenienceOffer> offers,
        int convenienceId)
    {
        if (offers == null)
        {
            return null;
        }

        for (int i = 0; i < offers.Count; i++)
        {
            PlayerConvenienceOffer offer = offers[i];
            if (offer != null && offer.convenienceId == convenienceId)
            {
                return offer;
            }
        }

        return null;
    }

    private void AddInventoryItem(int itemId, int amount)
    {
        for (int i = 0; i < _data.inventory.Count; i++)
        {
            PlayerInventoryItem item = _data.inventory[i];
            if (item.itemId == itemId)
            {
                item.amount += amount;
                return;
            }
        }

        _data.inventory.Add(new PlayerInventoryItem
        {
            itemId = itemId,
            amount = amount
        });
    }

    private static bool IsSupportedBaseProperty(int basePropertyId)
    {
        return basePropertyId == BasePropertyId.SimulationCoin ||
            basePropertyId == BasePropertyId.TimeCoin ||
            basePropertyId == BasePropertyId.Health;
    }

    private void AddBaseProperty(int basePropertyId)
    {
        switch (basePropertyId)
        {
            case BasePropertyId.SimulationCoin:
                AddSimulationCoins(1);
                break;
            case BasePropertyId.TimeCoin:
                AddTimeCoins(1);
                break;
            case BasePropertyId.Health:
                ChangeHealth(1);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(basePropertyId));
        }
    }

    private PlayerWorkProgress FindWorkProgress(string workType)
    {
        if (string.IsNullOrWhiteSpace(workType))
        {
            return null;
        }

        for (int i = 0; i < _data.workProgresses.Count; i++)
        {
            PlayerWorkProgress progress = _data.workProgresses[i];
            if (progress != null && progress.workType == workType)
            {
                return progress;
            }
        }

        return null;
    }
}