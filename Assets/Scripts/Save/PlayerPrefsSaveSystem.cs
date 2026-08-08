using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimeChip.Save
{
    public static class PlayerPrefsSaveSystem
    {
        /// <summary>
        /// 所有存档相关 PlayerPrefs 键的统一前缀
        /// </summary>
        private const string KeyPrefix = "TimeChip.Save.";

        /// <summary>
        /// 存档槽位索引的主键
        /// </summary>
        private const string IndexKey = KeyPrefix + "Index";

        /// <summary>
        /// 存档槽位索引的备份键
        /// </summary>
        private const string IndexBackupKey = IndexKey + ".Backup";

        /// <summary>
        /// 用于校验存档和索引来源的固定格式标识
        /// </summary>
        private const string Format = "TimeChip.PlayerPrefsSave";

        /// <summary>
        /// 将指定数据写入存档槽位, 并保留上一次有效数据作为备份
        /// </summary>
        /// <typeparam name="T">需要序列化的引用类型存档数据</typeparam>
        /// <param name="slotId">非负的存档槽位编号</param>
        /// <param name="displayName">在存档列表中显示的名称, 为空时自动生成名称</param>
        /// <param name="data">要保存的存档数据, 不能为空</param>
        /// <param name="schemaVersion">数据结构版本号, 必须大于等于 1</param>
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
                    "Schema version 必须至少为 1");
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

        /// <summary>
        /// 从指定槽位读取存档, 主存档无效时自动尝试备份并恢复主存档
        /// </summary>
        /// <typeparam name="T">需要反序列化的引用类型存档数据</typeparam>
        /// <param name="slotId">非负的存档槽位编号</param>
        /// <param name="data">读取成功时返回存档数据, 失败时为 null</param>
        /// <param name="schemaVersion">读取成功时返回存档数据结构版本, 失败时为 0</param>
        /// <returns>成功读取有效主存档或备份存档时返回 true</returns>
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

        /// <summary>
        /// 检查指定槽位是否存在主存档或备份存档
        /// </summary>
        /// <param name="slotId">非负的存档槽位编号</param>
        /// <returns>存在任一存档键时返回 true</returns>
        public static bool Exists(int slotId)
        {
            ValidateSlotId(slotId);
            return PlayerPrefs.HasKey(GetSaveKey(slotId))
                || PlayerPrefs.HasKey(GetBackupKey(slotId));
        }

        /// <summary>
        /// 获取全部存档槽位信息, 并按最后修改时间从新到旧排序
        /// </summary>
        /// <returns>按最后修改时间降序排列的存档槽位信息列表</returns>
        public static IReadOnlyList<SaveSlotInfo> GetSlots()
        {
            SaveIndex index = LoadIndex();
            index.slots.Sort((left, right) =>
                right.modifiedAtUtcTicks.CompareTo(left.modifiedAtUtcTicks));
            return index.slots;
        }

        /// <summary>
        /// 删除指定槽位的主存档、备份存档及其索引记录
        /// </summary>
        /// <param name="slotId">非负的存档槽位编号</param>
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

        /// <summary>
        /// 从指定 PlayerPrefs 键读取并验证存档包装对象
        /// </summary>
        /// <typeparam name="T">存档数据的引用类型</typeparam>
        /// <param name="key">存档所在的 PlayerPrefs 键</param>
        /// <param name="envelope">读取成功时返回已验证的存档包装对象</param>
        /// <returns>键中包含有效存档时返回 true</returns>
        private static bool TryLoadEnvelope<T>(
            string key,
            out SaveEnvelope<T> envelope) where T : class
        {
            return TryDeserializeEnvelope(
                PlayerPrefs.GetString(key, string.Empty),
                out envelope);
        }

        /// <summary>
        /// 将 JSON 文本反序列化为存档包装对象, 并验证格式、版本和数据有效性
        /// </summary>
        /// <typeparam name="T">存档数据的引用类型</typeparam>
        /// <param name="json">待解析的 JSON 文本</param>
        /// <param name="envelope">解析并验证成功时返回存档包装对象</param>
        /// <returns>JSON 为有效存档包装对象时返回 true</returns>
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

        /// <summary>
        /// 读取存档槽位索引, 主索引无效时尝试备份索引并恢复主索引
        /// </summary>
        /// <returns>有效的存档槽位索引, 没有有效索引时返回空索引</returns>
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

        /// <summary>
        /// 将 JSON 文本反序列化为存档槽位索引, 并验证其格式和槽位列表
        /// </summary>
        /// <param name="json">待解析的索引 JSON 文本</param>
        /// <param name="index">解析并验证成功时返回槽位索引</param>
        /// <returns>JSON 为有效存档槽位索引时返回 true</returns>
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

        /// <summary>
        /// 写入最新槽位索引, 并在原索引有效时先保存其备份
        /// </summary>
        /// <param name="index">要写入的存档槽位索引</param>
        private static void WriteIndex(SaveIndex index)
        {
            string existingJson = PlayerPrefs.GetString(IndexKey, string.Empty);
            if (TryDeserializeIndex(existingJson, out SaveIndex _))
            {
                PlayerPrefs.SetString(IndexBackupKey, existingJson);
            }

            PlayerPrefs.SetString(IndexKey, JsonUtility.ToJson(index));
        }

        /// <summary>
        /// 在槽位索引中查找指定编号的存档信息
        /// </summary>
        /// <param name="index">待查找的存档槽位索引</param>
        /// <param name="slotId">要查找的槽位编号</param>
        /// <returns>匹配的槽位信息, 未找到时返回 null</returns>
        private static SaveSlotInfo FindSlot(SaveIndex index, int slotId)
        {
            return index.slots.Find(slot => slot.slotId == slotId);
        }

        /// <summary>
        /// 生成指定槽位主存档使用的 PlayerPrefs 键
        /// </summary>
        /// <param name="slotId">存档槽位编号</param>
        /// <returns>主存档的 PlayerPrefs 键</returns>
        private static string GetSaveKey(int slotId)
        {
            return $"{KeyPrefix}Slot.{slotId}";
        }

        /// <summary>
        /// 生成指定槽位备份存档使用的 PlayerPrefs 键
        /// </summary>
        /// <param name="slotId">存档槽位编号</param>
        /// <returns>备份存档的 PlayerPrefs 键</returns>
        private static string GetBackupKey(int slotId)
        {
            return $"{GetSaveKey(slotId)}.Backup";
        }

        /// <summary>
        /// 验证存档槽位编号合法, 非法编号会抛出异常
        /// </summary>
        /// <param name="slotId">待验证的槽位编号</param>
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
            /// <summary>
            /// 存档格式标识, 用于拒绝非本系统生成的数据
            /// </summary>
            public string format;

            /// <summary>
            /// 被包装存档数据的数据结构版本
            /// </summary>
            public int schemaVersion;

            /// <summary>
            /// 存档写入时的 UTC 时间刻度
            /// </summary>
            public long savedAtUtcTicks;

            /// <summary>
            /// 实际需要保存的业务数据
            /// </summary>
            public T data;
        }

        [Serializable]
        private sealed class SaveIndex
        {
            /// <summary>
            /// 索引格式标识，用于验证索引数据来源。
            /// </summary>
            public string format = Format;

            /// <summary>
            /// 已创建的全部存档槽位信息。
            /// </summary>
            public List<SaveSlotInfo> slots = new List<SaveSlotInfo>();
        }
    }

    [Serializable]
    public sealed class SaveSlotInfo
    {
        /// <summary>
        /// 存档槽位的非负唯一编号
        /// </summary>
        public int slotId;

        /// <summary>
        /// 用于存档列表展示的名称
        /// </summary>
        public string displayName;

        /// <summary>
        /// 该槽位存档数据的数据结构版本
        /// </summary>
        public int schemaVersion;

        /// <summary>
        /// 此槽位首次创建时的 UTC 时间刻度
        /// </summary>
        public long createdAtUtcTicks;

        /// <summary>
        /// 此槽位最后一次保存时的 UTC 时间刻度
        /// </summary>
        public long modifiedAtUtcTicks;

        /// <summary>
        /// 将创建时间刻度转换为 UTC 时间
        /// </summary>
        public DateTime CreatedAtUtc => new DateTime(createdAtUtcTicks, DateTimeKind.Utc);

        /// <summary>
        /// 将最后修改时间刻度转换为 UTC 时间
        /// </summary>
        public DateTime ModifiedAtUtc => new DateTime(modifiedAtUtcTicks, DateTimeKind.Utc);
    }
}
