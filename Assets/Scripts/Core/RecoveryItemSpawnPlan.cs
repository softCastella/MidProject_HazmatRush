using UnityEngine;

public static class RecoveryItemSpawnPlan
{
    public struct Entry
    {
        public int pointIndex;
        public RecoveryItem.ItemType type;
    }

    public static Entry[] entries;

    public static void PrepareRandom(int pointCount, int minCount, int maxCount)
    {
        if (pointCount <= 0)
        {
            entries = null;
            return;
        }

        int count = Random.Range(minCount, maxCount + 1);
        count = Mathf.Clamp(count, 0, pointCount);

        bool[] used = new bool[pointCount];
        entries = new Entry[count];

        for (int i = 0; i < count; i++)
        {
            int pointIndex = PickUnusedIndex(used, pointCount);
            used[pointIndex] = true;

            entries[i].pointIndex = pointIndex;
            entries[i].type = Random.value < 0.5f
                ? RecoveryItem.ItemType.Protection
                : RecoveryItem.ItemType.Time;
        }

        Debug.Log($"[RecoveryItemSpawnPlan] 로딩 확정 - {count}개 배치");
    }

    public static void Clear()
    {
        entries = null;
    }

    private static int PickUnusedIndex(bool[] used, int pointCount)
    {
        int safety = 0;
        while (safety < 32)
        {
            int index = Random.Range(0, pointCount);
            if (!used[index])
                return index;
            safety++;
        }

        for (int i = 0; i < pointCount; i++)
        {
            if (!used[i])
                return i;
        }

        return 0;
    }
}
