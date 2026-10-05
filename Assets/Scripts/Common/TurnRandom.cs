using System;

/// <summary>基于当前回合种子和稳定事件键生成确定性随机值</summary>
public static class TurnRandom
{
    public static bool Chance(string eventKey, float probability)
    {
        return Value(eventKey) < Math.Max(0f, Math.Min(1f, probability));
    }

    public static float Value(string eventKey)
    {
        return (float)(GetUInt(eventKey) / 4294967296d);
    }

    public static int Range(string eventKey, int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        }

        uint range = (uint)(maxExclusive - minInclusive);
        return minInclusive + (int)(GetUInt(eventKey) % range);
    }

    public static float Range(string eventKey, float minInclusive, float maxInclusive)
    {
        if (maxInclusive < minInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(maxInclusive));
        }

        return minInclusive + (maxInclusive - minInclusive) * Value(eventKey);
    }

    public static long Range(string eventKey, long maxExclusive)
    {
        if (maxExclusive <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        }

        return (long)(GetUInt(eventKey) % (ulong)maxExclusive);
    }

    private static uint GetUInt(string eventKey)
    {
        if (string.IsNullOrEmpty(eventKey))
        {
            throw new ArgumentException("随机事件键不能为空", nameof(eventKey));
        }

        unchecked
        {
            uint hash = 2166136261;
            uint seed = (uint)PlayerInfoManager.GetInstance().TurnRandomSeed;
            hash = (hash ^ seed) * 16777619;
            for (int i = 0; i < eventKey.Length; i++)
            {
                hash = (hash ^ eventKey[i]) * 16777619;
            }

            hash ^= hash >> 16;
            hash *= 0x7feb352d;
            hash ^= hash >> 15;
            hash *= 0x846ca68b;
            return hash ^ (hash >> 16);
        }
    }
}
