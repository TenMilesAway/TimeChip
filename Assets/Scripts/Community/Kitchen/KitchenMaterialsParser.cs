using System.Collections.Generic;

public static class KitchenMaterialsParser
{
    public const int MaxMaterialSlots = 4;

    public static bool TryParse(
        string materials,
        out List<ExchangeCostEntry> slots,
        out string errorMessage)
    {
        slots = new List<ExchangeCostEntry>();
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(materials))
        {
            errorMessage = "配方材料不能为空。";
            return false;
        }

        string[] entries = materials.Split(';');
        if (entries.Length < 1 || entries.Length > MaxMaterialSlots)
        {
            errorMessage = $"配方材料条目数量无效，应为 1 至 {MaxMaterialSlots} 组。";
            return false;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            if (!TryParseItemAmount(entries[i], out int itemId, out int count))
            {
                errorMessage = $"配方材料格式无效: [{entries[i]}]";
                slots.Clear();
                return false;
            }

            slots.Add(new ExchangeCostEntry(itemId, count));
        }

        return true;
    }

    private static bool TryParseItemAmount(string entry, out int itemId, out int count)
    {
        itemId = 0;
        count = 0;
        if (string.IsNullOrWhiteSpace(entry))
        {
            return false;
        }

        string[] parts = entry.Split(',');
        if (parts.Length != 2)
        {
            return false;
        }

        return int.TryParse(parts[0], out itemId) &&
            itemId > 0 &&
            int.TryParse(parts[1], out count) &&
            count > 0;
    }
}