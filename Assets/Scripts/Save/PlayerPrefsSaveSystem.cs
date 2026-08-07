using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimeChip.Save
{
    public static class PlayerPrefsSaveSystem
    {
        private const string KeyPrefix = "TimeChip.Save.";
        private const string IndexKey = KeyPrefix + "Index";
        private const string IndexBackupKey = IndexKey + ".Backup";
        private const string Format = "TimeChip.PlayerPrefsSave";

        public static void Save<T>(
            int slotId,
            string displayName,
            T data,
            int schemaVersion) where T : class
        {
            ValidateSlotId(slotId);

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (schemaVersion < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(schemaVersion),
                    "Schema version must be at least 1.");
            }

            long now = DateTime.UtcNow.Ticks;
            SaveIndex index = LoadIndex();
            SaveSlotInfo slotInfo = FindSlot(index, slotId);

            if (slotInfo == null)
            {
                slotInfo = new SaveSlotInfo
                {
                    slotId = slotId,
                    createdAtUtcTicks = now
                };
                index.slots.Add(slotInfo);
            }

            slotInfo.displayName = string.IsNullOrWhiteSpace(displayName)
                ? $"存档 {slotId + 1}"
                : displayName.Trim();
            slotInfo.modifiedAtUtcTicks = now;
            slotInfo.schemaVersion = schemaVersion;

            SaveEnvelope<T> envelope = new SaveEnvelope<T>
            {
                format = Format,
                schemaVersion = schemaVersion,
                savedAtUtcTicks = now,
                data = data
            };

            string saveKey = GetSaveKey(slotId);
            string backupKey = GetBackupKey(slotId);
            string existingJson = PlayerPrefs.GetString(saveKey, string.Empty);

            if (TryDeserializeEnvelope(existingJson, out SaveEnvelope<T> _))
            {
                PlayerPrefs.SetString(backupKey, existingJson);
            }

            PlayerPrefs.SetString(saveKey, JsonUtility.ToJson(envelope));
            WriteIndex(index);
            PlayerPrefs.Save();
        }

        public static bool TryLoad<T>(
            int slotId,
            out T data,
            out int schemaVersion) where T : class
        {
            ValidateSlotId(slotId);

            if (TryLoadEnvelope(GetSaveKey(slotId), out SaveEnvelope<T> envelope))
            {
                data = envelope.data;
                schemaVersion = envelope.schemaVersion;
                return true;
            }

            if (TryLoadEnvelope(GetBackupKey(slotId), out envelope))
            {
                data = envelope.data;
                schemaVersion = envelope.schemaVersion;

                PlayerPrefs.SetString(GetSaveKey(slotId), JsonUtility.ToJson(envelope));
                PlayerPrefs.Save();
                return true;
            }

            data = null;
            schemaVersion = 0;
            return false;
        }

        public static bool Exists(int slotId)
        {
            ValidateSlotId(slotId);
            return PlayerPrefs.HasKey(GetSaveKey(slotId))
                || PlayerPrefs.HasKey(GetBackupKey(slotId));
        }

        public static IReadOnlyList<SaveSlotInfo> GetSlots()
        {
            SaveIndex index = LoadIndex();
            index.slots.Sort((left, right) =>
                right.modifiedAtUtcTicks.CompareTo(left.modifiedAtUtcTicks));
            return index.slots;
        }

        public static void Delete(int slotId)
        {
            ValidateSlotId(slotId);

            PlayerPrefs.DeleteKey(GetSaveKey(slotId));
            PlayerPrefs.DeleteKey(GetBackupKey(slotId));

            SaveIndex index = LoadIndex();
            index.slots.RemoveAll(slot => slot.slotId == slotId);
            WriteIndex(index);
            PlayerPrefs.Save();
        }

        private static bool TryLoadEnvelope<T>(
            string key,
            out SaveEnvelope<T> envelope) where T : class
        {
            return TryDeserializeEnvelope(
                PlayerPrefs.GetString(key, string.Empty),
                out envelope);
        }

        private static bool TryDeserializeEnvelope<T>(
            string json,
            out SaveEnvelope<T> envelope) where T : class
        {
            envelope = null;
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            try
            {
                envelope = JsonUtility.FromJson<SaveEnvelope<T>>(json);
                return envelope != null
                    && envelope.format == Format
                    && envelope.schemaVersion >= 1
                    && envelope.data != null;
            }
            catch (ArgumentException)
            {
                envelope = null;
                return false;
            }
        }

        private static SaveIndex LoadIndex()
        {
            if (TryDeserializeIndex(
                PlayerPrefs.GetString(IndexKey, string.Empty),
                out SaveIndex index))
            {
                return index;
            }

            if (TryDeserializeIndex(
                PlayerPrefs.GetString(IndexBackupKey, string.Empty),
                out index))
            {
                PlayerPrefs.SetString(IndexKey, JsonUtility.ToJson(index));
                PlayerPrefs.Save();
                return index;
            }

            return new SaveIndex();
        }

        private static bool TryDeserializeIndex(string json, out SaveIndex index)
        {
            index = null;
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            try
            {
                index = JsonUtility.FromJson<SaveIndex>(json);
                return index != null
                    && index.format == Format
                    && index.slots != null;
            }
            catch (ArgumentException)
            {
                index = null;
                return false;
            }
        }

        private static void WriteIndex(SaveIndex index)
        {
            string existingJson = PlayerPrefs.GetString(IndexKey, string.Empty);
            if (TryDeserializeIndex(existingJson, out SaveIndex _))
            {
                PlayerPrefs.SetString(IndexBackupKey, existingJson);
            }

            PlayerPrefs.SetString(IndexKey, JsonUtility.ToJson(index));
        }

        private static SaveSlotInfo FindSlot(SaveIndex index, int slotId)
        {
            return index.slots.Find(slot => slot.slotId == slotId);
        }

        private static string GetSaveKey(int slotId)
        {
            return $"{KeyPrefix}Slot.{slotId}";
        }

        private static string GetBackupKey(int slotId)
        {
            return $"{GetSaveKey(slotId)}.Backup";
        }

        private static void ValidateSlotId(int slotId)
        {
            if (slotId < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slotId),
                    "Slot ID cannot be negative.");
            }
        }

        [Serializable]
        private sealed class SaveEnvelope<T>
        {
            public string format;
            public int schemaVersion;
            public long savedAtUtcTicks;
            public T data;
        }

        [Serializable]
        private sealed class SaveIndex
        {
            public string format = Format;
            public List<SaveSlotInfo> slots = new List<SaveSlotInfo>();
        }
    }

    [Serializable]
    public sealed class SaveSlotInfo
    {
        public int slotId;
        public string displayName;
        public int schemaVersion;
        public long createdAtUtcTicks;
        public long modifiedAtUtcTicks;

        public DateTime CreatedAtUtc => new DateTime(createdAtUtcTicks, DateTimeKind.Utc);
        public DateTime ModifiedAtUtc => new DateTime(modifiedAtUtcTicks, DateTimeKind.Utc);
    }
}
