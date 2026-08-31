using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 独立于玩家存档的全局成长数据
/// </summary>
[Serializable]
public sealed class GlobalInfoData
{
    /// <summary>可用于时光藏馆升星的回忆点数量</summary>
    public int memoryPoints;

    /// <summary>各成长卡牌的全局状态</summary>
    public List<GrowCardData> growCards = new List<GrowCardData>();
}

/// <summary>
/// 单张时光藏馆成长卡牌的全局状态
/// </summary>
[Serializable]
public sealed class GrowCardData
{
    /// <summary>对应 Grow 配置表中的唯一 ID</summary>
    public int growId;

    /// <summary>是否已解锁该卡牌</summary>
    public bool isUnlocked;

    /// <summary>当前星级，范围为 0 至 5</summary>
    public int starLevel;
}

/// <summary>
/// 提供回忆点与时光藏馆卡牌状态的统一访问和持久化
/// </summary>
public class GlobalInfoManager : Singleton<GlobalInfoManager>
{
    private const string SaveKey = "TimeChip.GlobalInfo";
    private const string BackupSaveKey = SaveKey + ".Backup";
    private const int MaxGrowCardStarLevel = 5;

    private GlobalInfoData _data;
    private bool _isInitialized;

    /// <summary>全局数据改变时触发</summary>
    public event Action<GlobalInfoManager> GlobalInfoChanged;

    /// <summary>当前拥有的回忆点</summary>
    public int MemoryPoints
    {
        get
        {
            EnsureInitialized();
            return _data.memoryPoints;
        }
    }

    /// <summary>
    /// 从专属全局存档加载数据, 重复调用不会重新加载或覆盖内存数据
    /// </summary>
    public void Init()
    {
        if (_isInitialized)
        {
            return;
        }

        _data = LoadData();
        NormalizeData();
        _isInitialized = true;
    }

    /// <summary>获取全局数据的独立副本</summary>
    public GlobalInfoData GetSnapshot()
    {
        EnsureInitialized();
        return CreateCopy(_data);
    }

    /// <summary>获取指定成长卡牌的独立副本；不存在时返回 null</summary>
    public GrowCardData GetGrowCard(int growId)
    {
        EnsureInitialized();
        GrowCardData card = FindGrowCard(growId);
        return card == null ? null : CreateCopy(card);
    }

    /// <summary>指定成长卡牌是否已解锁</summary>
    public bool IsGrowCardUnlocked(int growId)
    {
        EnsureInitialized();
        GrowCardData card = FindGrowCard(growId);
        return card != null && card.isUnlocked;
    }

    /// <summary>获取全部成长卡牌状态的独立副本</summary>
    public List<GrowCardData> GetGrowCards()
    {
        EnsureInitialized();
        return CreateGrowCardCopies(_data.growCards);
    }

    /// <summary>增加回忆点，数量必须为正数</summary>
    public void AddMemoryPoints(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "回忆点增加数量必须大于零");
        }

        EnsureInitialized();
        _data.memoryPoints = checked(_data.memoryPoints + amount);
        SaveAndNotify();
    }

    /// <summary>尝试消耗回忆点；余额不足时不会修改数据</summary>
    public bool TrySpendMemoryPoints(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "回忆点消耗数量必须大于零");
        }

        EnsureInitialized();
        if (_data.memoryPoints < amount)
        {
            return false;
        }

        _data.memoryPoints -= amount;
        SaveAndNotify();
        return true;
    }

    /// <summary>
    /// 根据成长配置补齐卡牌记录, 已有记录会保留其解锁与星级状态
    /// </summary>
    public void EnsureGrowCards(IEnumerable<int> growIds)
    {
        if (growIds == null)
        {
            throw new ArgumentNullException(nameof(growIds));
        }

        EnsureInitialized();
        bool changed = false;
        HashSet<int> ids = new HashSet<int>();
        foreach (int growId in growIds)
        {
            if (growId <= 0 || !ids.Add(growId) || FindGrowCard(growId) != null)
            {
                continue;
            }

            _data.growCards.Add(new GrowCardData { growId = growId });
            changed = true;
        }

        if (changed)
        {
            SaveAndNotify();
        }
    }

    /// <summary>解锁指定成长卡牌</summary>
    public void UnlockGrowCard(int growId)
    {
        EnsureInitialized();
        GrowCardData card = GetRequiredGrowCard(growId);
        if (card.isUnlocked)
        {
            return;
        }

        card.isUnlocked = true;
        SaveAndNotify();
    }

    /// <summary>设置已解锁成长卡牌的星级</summary>
    public void SetGrowCardStarLevel(int growId, int starLevel)
    {
        if (starLevel < 0 || starLevel > MaxGrowCardStarLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(starLevel));
        }

        EnsureInitialized();
        GrowCardData card = GetRequiredGrowCard(growId);
        if (!card.isUnlocked)
        {
            throw new InvalidOperationException("未解锁的卡牌不能设置星级");
        }

        if (card.starLevel == starLevel)
        {
            return;
        }

        card.starLevel = starLevel;
        SaveAndNotify();
    }

    /// <summary>
    /// 尝试消耗回忆点并将已解锁卡牌提升一星
    /// </summary>
    public bool TryUpgradeGrowCard(int growId, int memoryPointCost)
    {
        if (memoryPointCost <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(memoryPointCost),
                "升星消耗的回忆点必须大于零");
        }

        EnsureInitialized();
        GrowCardData card = GetRequiredGrowCard(growId);
        if (!card.isUnlocked ||
            card.starLevel >= MaxGrowCardStarLevel ||
            _data.memoryPoints < memoryPointCost)
        {
            return false;
        }

        _data.memoryPoints -= memoryPointCost;
        card.starLevel++;
        SaveAndNotify();
        return true;
    }

    /// <summary>
    /// 发放新人生的时光藏馆奖励，并从尚未解锁的卡牌中随机解锁指定数量
    /// </summary>
    /// <returns>本次实际解锁的卡牌数量</returns>
    public int GrantNewLifeReward(int memoryPointAmount, int unlockCount)
    {
        if (memoryPointAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(memoryPointAmount),
                "新人生奖励的回忆点必须大于零");
        }

        if (unlockCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unlockCount));
        }

        EnsureInitialized();
        _data.memoryPoints = checked(_data.memoryPoints + memoryPointAmount);

        List<GrowCardData> lockedCards = new List<GrowCardData>();
        for (int index = 0; index < _data.growCards.Count; index++)
        {
            GrowCardData card = _data.growCards[index];
            if (!card.isUnlocked)
            {
                lockedCards.Add(card);
            }
        }

        int unlockedCount = Mathf.Min(unlockCount, lockedCards.Count);
        for (int index = 0; index < unlockedCount; index++)
        {
            int randomIndex = UnityEngine.Random.Range(index, lockedCards.Count);
            GrowCardData selectedCard = lockedCards[index];
            lockedCards[index] = lockedCards[randomIndex];
            lockedCards[randomIndex] = selectedCard;
            lockedCards[index].isUnlocked = true;
        }

        SaveAndNotify();
        return unlockedCount;
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            Init();
        }
    }

    private GlobalInfoData LoadData()
    {
        if (TryLoad(SaveKey, out GlobalInfoData data))
        {
            return data;
        }

        if (TryLoad(BackupSaveKey, out data))
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            return data;
        }

        return new GlobalInfoData();
    }

    private static bool TryLoad(string key, out GlobalInfoData data)
    {
        data = null;
        string json = PlayerPrefs.GetString(key, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            return false;
        }

        try
        {
            data = JsonUtility.FromJson<GlobalInfoData>(json);
            return data != null;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private void NormalizeData()
    {
        _data = _data ?? new GlobalInfoData();
        _data.memoryPoints = Mathf.Max(0, _data.memoryPoints);
        _data.growCards = _data.growCards ?? new List<GrowCardData>();

        HashSet<int> knownIds = new HashSet<int>();
        for (int index = _data.growCards.Count - 1; index >= 0; index--)
        {
            GrowCardData card = _data.growCards[index];
            if (card == null || card.growId <= 0 || !knownIds.Add(card.growId))
            {
                _data.growCards.RemoveAt(index);
                continue;
            }

            card.starLevel = Mathf.Clamp(card.starLevel, 0, MaxGrowCardStarLevel);
        }
    }

    private GrowCardData GetRequiredGrowCard(int growId)
    {
        GrowCardData card = FindGrowCard(growId);
        if (card == null)
        {
            throw new ArgumentException("找不到指定的成长卡牌", nameof(growId));
        }

        return card;
    }

    private GrowCardData FindGrowCard(int growId)
    {
        for (int index = 0; index < _data.growCards.Count; index++)
        {
            GrowCardData card = _data.growCards[index];
            if (card.growId == growId)
            {
                return card;
            }
        }

        return null;
    }

    private void SaveAndNotify()
    {
        string existingJson = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (!string.IsNullOrEmpty(existingJson))
        {
            PlayerPrefs.SetString(BackupSaveKey, existingJson);
        }

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(_data));
        PlayerPrefs.Save();
        GlobalInfoChanged?.Invoke(this);
    }

    private static GlobalInfoData CreateCopy(GlobalInfoData source)
    {
        return new GlobalInfoData
        {
            memoryPoints = source.memoryPoints,
            growCards = CreateGrowCardCopies(source.growCards)
        };
    }

    private static List<GrowCardData> CreateGrowCardCopies(List<GrowCardData> source)
    {
        List<GrowCardData> copies = new List<GrowCardData>(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            copies.Add(CreateCopy(source[index]));
        }

        return copies;
    }

    private static GrowCardData CreateCopy(GrowCardData source)
    {
        return new GrowCardData
        {
            growId = source.growId,
            isUnlocked = source.isUnlocked,
            starLevel = source.starLevel
        };
    }
}
