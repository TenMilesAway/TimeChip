"""Create grow2.xlsx and register in __tables__.xlsx for Luban."""
from pathlib import Path

import openpyxl

ROOT = Path(__file__).resolve().parents[1]
DATAS = ROOT / "MiniTemplate" / "Datas"

HEADERS = [
    ("##var", "id", "name", "branch", "prerequisiteId", "memoryPointCost", "effect", "effectValue", "effectDesc", "posX", "posY"),
    ("##type", "int", "string", "string", "int", "int", "string", "int", "string", "int", "int"),
    ("##group", "c,s", "c,s", "c,s", "c,s", "c,s", "c,s", "c,s", "c,s", "c,s", "c,s"),
    (
        "##",
        "天赋节点 ID",
        "天赋名称",
        "分支：通用/生活/经营/探索",
        "前置天赋 ID；0 表示无前置",
        "解锁消耗回忆点数",
        "效果类型（程序用）",
        "效果数值",
        "收益描述（展示用）",
        "天赋树 UI 坐标 X",
        "天赋树 UI 坐标 Y",
    ),
]

ROWS = [
    (1001, "时光回响", "通用", 0, 30, "MemoryGainPct", 5, "全玩法获得的回忆点数 +5%", 0, 0),
    (1002, "慢活节律", "生活", 1001, 45, "StaminaRecoverPct", 10, "体力自然恢复速度 +10%", -280, -220),
    (1003, "职场启程", "经营", 1001, 45, "WorkIncomePct", 8, "打工收益 +8%", 0, -280),
    (1004, "街坊漫步", "探索", 1001, 45, "ExploreRewardPct", 8, "探索与随机事件奖励 +8%", 280, -220),
    (1005, "小家暖意", "生活", 1002, 75, "HomeSatisfaction", 8, "家具满意度基础 +8", -280, -480),
    (1006, "精打细算", "经营", 1003, 75, "ShopDiscountPct", 3, "便利店购物折扣 +3%", 0, -520),
    (1007, "渔夫直觉", "探索", 1004, 75, "FishRarePct", 5, "钓鱼稀有鱼概率 +5%", 280, -480),
    (1008, "安稳梦乡", "生活", 1005, 120, "OfflineCapHours", 1, "离线收益时长上限 +1 小时", -280, -720),
    (1009, "人脉生财", "经营", 1006, 120, "MissionRewardPct", 10, "任务奖励 +10%", 0, -760),
    (1010, "幸运星尘", "探索", 1007, 120, "LotteryLuck", 5, "抽奖幸运值 +5", 280, -720),
]


def create_grow2_sheet():
    path = DATAS / "grow2.xlsx"
    wb = openpyxl.Workbook()
    ws = wb.active
    ws.title = "Sheet1"
    for row in HEADERS:
        ws.append(list(row))
    for row in ROWS:
        ws.append([None] + list(row))
    wb.save(path)
    print(f"Wrote {path}")


def register_table():
    path = DATAS / "__tables__.xlsx"
    wb = openpyxl.load_workbook(path)
    ws = wb.active
    for row in ws.iter_rows(min_row=4, values_only=True):
        if row[1] == "Grow2Table":
            print("Grow2Table already registered")
            return
    ws.append(
        (
            None,
            "Grow2Table",
            "Grow2",
            1,
            "grow2.xlsx",
            "id",
            "map",
            "c,s",
            "天赋树 Grow2 节点配置",
            None,
            "grow2",
        )
    )
    wb.save(path)
    print(f"Updated {path}")


if __name__ == "__main__":
    create_grow2_sheet()
    register_table()
