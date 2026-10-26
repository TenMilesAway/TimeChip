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
    (1001, "时光回响", "通用", 0, 30, "MemoryGainPct", 2, "获得回忆点数+2%", 0, 0),
    (1002, "健壮", "健康", 1001, 45, "Health", 1, "健康消耗-1", -280, -220),
    (1003, "多金", "经营", 1001, 45, "WorkIncome", 10, "零工收益+10", 0, -280),
    (1004, "探索", "探索", 1001, 45, "CommunityRandomPct", 1, "社区随机事件概率小幅提升", 280, -220),
    (1005, "健壮II", "健康", 1002, 75, "HealthFree", 2, "2%概率不消耗健康值", -280, -480),
    (1006, "多金II", "经营", 1003, 75, "ShopDiscount", 10, "购物价格-10", 0, -520),
    (1007, "探索II", "探索", 1004, 75, "FishRarePct", 1, "钓鱼获得稀有鱼概率小幅提升", 280, -480),
    (1008, "健壮III", "健康", 1005, 120, "Health", 1, "健康消耗-1", -280, -720),
    (1009, "多金III", "经营", 1006, 120, "MissionReward", 10, "任务奖励+10", 0, -760),
    (1010, "探索III", "探索", 1007, 120, "LotteryLuck", 5, "抽奖获得品质大于3的物品概率小幅提升", 280, -720),
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
