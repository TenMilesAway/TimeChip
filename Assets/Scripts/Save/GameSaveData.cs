using System;
using System.Collections.Generic;

namespace TimeChip.Save
{
    [Serializable]
    public sealed class GameSaveData
    {
        public const int CurrentSchemaVersion = 1;

        public double totalPlayTimeSeconds;
        public PlayerData player = new PlayerData();
        public ProgressData progress = new ProgressData();
        public GameSettingsData settings = new GameSettingsData();
    }

    [Serializable]
    public sealed class PlayerData
    {
        public string playerName;
        public int level = 1;
        public long currency;
    }

    [Serializable]
    public sealed class ProgressData
    {
        public string currentChapterId;
        public List<string> unlockedChapterIds = new List<string>();
        public List<LevelRecord> levelRecords = new List<LevelRecord>();
    }

    [Serializable]
    public sealed class LevelRecord
    {
        public string levelId;
        public int bestScore;
        public bool completed;
    }

    [Serializable]
    public sealed class GameSettingsData
    {
        public bool musicEnabled = true;
        public bool soundEnabled = true;
        public float musicVolume = 1f;
        public float soundVolume = 1f;
    }
}
