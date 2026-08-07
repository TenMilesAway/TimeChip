# PlayerPrefs多存档系统

- 日期：2026-08-07
- 任务：PlayerPrefs多存档系统

## 完成的工作

- 新增基于 PlayerPrefs 的多槽位存档系统，每个槽位保存一份完整 JSON 数据。
- 新增存档索引，记录槽位名称、数据版本、创建时间和最后修改时间，并按最后修改时间列出存档。
- 保存时保留上一份有效数据作为备份；主存档无法解析时自动读取备份并恢复主存档。
- 支持保存、读取、判断存在、列出槽位和删除存档。
- 存档外层包含格式标识和 `schemaVersion`，便于后续版本迁移与兼容处理。
- 提供玩家数据、关卡进度、关卡纪录和音频设置等基础数据模型。

## 修改文件

- `Assets/Scripts/Save/GameSaveData.cs`
- `Assets/Scripts/Save/PlayerPrefsSaveSystem.cs`

## 使用说明

引用命名空间：

```csharp
using TimeChip.Save;
```

创建并保存 0 号槽位：

```csharp
var data = new GameSaveData();
data.player.playerName = "玩家";
data.player.level = 10;
data.progress.currentChapterId = "chapter_01";

PlayerPrefsSaveSystem.Save(
    0,
    "存档一",
    data,
    GameSaveData.CurrentSchemaVersion);
```

读取存档：

```csharp
if (PlayerPrefsSaveSystem.TryLoad(
    0,
    out GameSaveData data,
    out int schemaVersion))
{
    Debug.Log(data.player.level);
}
```

列出和删除存档：

```csharp
IReadOnlyList<SaveSlotInfo> slots = PlayerPrefsSaveSystem.GetSlots();
bool exists = PlayerPrefsSaveSystem.Exists(0);
PlayerPrefsSaveSystem.Delete(0);
```

新增数据字段时应同步递增 `GameSaveData.CurrentSchemaVersion`，并在业务层根据读取到的旧版本执行迁移。PlayerPrefs 适合体积较小的本地存档，不提供加密、防篡改或跨设备同步能力。
