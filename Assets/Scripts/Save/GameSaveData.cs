using System;
using System.Collections.Generic;

namespace TimeChip.Save
{
    [Serializable]
    public sealed class GameSaveData
    {
        /// <summary>
        /// 当前存档数据结构的版本号, 用于后续存档兼容与迁移
        /// </summary>
        public const int CurrentSchemaVersion = 3;

        /// <summary>
        /// 本存档累计的游戏时长, 单位为秒
        /// </summary>
        public double totalPlayTimeSeconds;

        /// <summary>
        /// 玩家基础数据
        /// </summary>
        public PlayerData player = new PlayerData();

        /// <summary>
        /// 玩家当前的游戏状态数据
        /// </summary>
        public PlayerInfoData playerInfo = new PlayerInfoData();

        /// <summary>
        /// 游戏流程与关卡进度数据
        /// </summary>
        public ProgressData progress = new ProgressData();

        /// <summary>
        /// 随存档保存的游戏设置数据
        /// </summary>
        public GameSettingsData settings = new GameSettingsData();
    }

    [Serializable]
    public sealed class PlayerData
    {
        /// <summary>
        /// 玩家显示名称
        /// </summary>
        public string playerName;

        /// <summary>
        /// 玩家当前等级, 默认从 1 级开始
        /// </summary>
        public int level = 1;

        /// <summary>
        /// 玩家拥有的游戏货币数量
        /// </summary>
        public long currency;
    }

    [Serializable]
    public sealed class ProgressData
    {
        /// <summary>
        /// 当前进行中的章节标识
        /// </summary>
        public string currentChapterId;

        /// <summary>
        /// 已解锁章节的标识列表
        /// </summary>
        public List<string> unlockedChapterIds = new List<string>();

        /// <summary>
        /// 各关卡的通关与最高分记录
        /// </summary>
        public List<LevelRecord> levelRecords = new List<LevelRecord>();
    }

    [Serializable]
    public sealed class LevelRecord
    {
        /// <summary>
        /// 关卡的唯一标识
        /// </summary>
        public string levelId;

        /// <summary>
        /// 该关卡历史获得的最高分
        /// </summary>
        public int bestScore;

        /// <summary>
        /// 该关卡是否已完成
        /// </summary>
        public bool completed;
    }

    [Serializable]
    public sealed class GameSettingsData
    {
        /// <summary>
        /// 是否启用背景音乐
        /// </summary>
        public bool musicEnabled = true;

        /// <summary>
        /// 是否启用音效
        /// </summary>
        public bool soundEnabled = true;

        /// <summary>
        /// 背景音乐音量，默认值为最大音量
        /// </summary>
        public float musicVolume = 1f;

        /// <summary>
        /// 音效音量，默认值为最大音量
        /// </summary>
        public float soundVolume = 1f;
    }
}
