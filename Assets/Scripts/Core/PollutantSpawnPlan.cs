using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

// 로딩 화면에서 스테이지별 오염원 배치·등장 순서를 확정합니다 (RecoveryItemSpawnPlan과 동일 패턴).
public static class PollutantSpawnPlan
{
    public struct Entry
    {
        public Pollutant.PollutantType type;
        public int spawnPointIndex;
        public bool useTypeDPoints;
    }

    public static Entry[] abcQueue;
    public static Entry[] dQueue;
    // 등장 순서 큐 (A~C 먼저, D는 그 다음 — 로딩 시 확정, 런타임에 하나씩 공개)
    public static Entry[] spawnQueue;
    // 맵(화면)마다 오염원 수 — 합이 totalPollutant와 같음
    public static int[] mapPollutantCounts;
    public static int currentMapIndex;

    public static bool HasPlan()
    {
        return spawnQueue != null && spawnQueue.Length > 0;
    }

    public static void Clear()
    {
        abcQueue = null;
        dQueue = null;
        spawnQueue = null;
        mapPollutantCounts = null;
        currentMapIndex = 0;
    }

    public static void ResetMapProgress()
    {
        currentMapIndex = 0;
    }

    public static void AdvanceMap()
    {
        if (mapPollutantCounts == null)
            return;
        if (currentMapIndex + 1 < mapPollutantCounts.Length)
            currentMapIndex++;
    }

    public static bool HasMoreMaps()
    {
        return mapPollutantCounts != null && currentMapIndex + 1 < mapPollutantCounts.Length;
    }

    public static int GetSegmentStart()
    {
        if (mapPollutantCounts == null)
            return 0;

        int start = 0;
        for (int i = 0; i < currentMapIndex; i++)
            start += mapPollutantCounts[i];
        return start;
    }

    public static int GetCurrentMapCount()
    {
        if (mapPollutantCounts == null || mapPollutantCounts.Length == 0)
            return spawnQueue != null ? spawnQueue.Length : 0;

        if (currentMapIndex < 0 || currentMapIndex >= mapPollutantCounts.Length)
            return 0;

        return mapPollutantCounts[currentMapIndex];
    }

    public static bool HasMoreInCurrentMap(int localRevealIndex)
    {
        return localRevealIndex < GetCurrentMapCount();
    }

    public static void Prepare(TextAsset stageDataJson, int stageIndex, int abcPointCount, int dPointCount)
    {
        Clear();

        if (abcPointCount < 0)
            abcPointCount = 0;
        if (dPointCount < 0)
            dPointCount = 0;

        if (stageDataJson == null || string.IsNullOrEmpty(stageDataJson.text))
        {
            Debug.LogWarning("[PollutantSpawnPlan] stage_data 없음 - 게임 씬에서 런타임 생성으로 폴백");
            return;
        }

        List<StageJsonRow> rows = JsonConvert.DeserializeObject<List<StageJsonRow>>(stageDataJson.text);
        if (rows == null || rows.Count == 0)
            return;

        int index = Mathf.Clamp(stageIndex, 0, rows.Count - 1);
        StageJsonRow row = rows[index];
        string types = row.pollutantTypes;
        if (string.IsNullOrEmpty(types))
            return;

        List<Pollutant.PollutantType> abcTypes = new List<Pollutant.PollutantType>();
        bool hasD = false;
        string[] parts = types.Split('|');
        for (int i = 0; i < parts.Length; i++)
        {
            string token = parts[i].Trim();
            if (token.Length == 0)
                continue;

            char c = char.ToUpperInvariant(token[0]);
            if (c == 'D')
                hasD = true;
            else if (c == 'A' || c == 'B' || c == 'C')
                abcTypes.Add(CharToType(c));
        }

        int dCount = hasD ? 1 : 0;
        int abcCount = row.totalPollutant - dCount;
        if (abcCount < 0)
            abcCount = 0;

        if (abcCount > 0 && abcTypes.Count == 0)
            abcCount = 0;

        // A~C 타입이 없고 D만 있을 때: totalPollutant 전부 D로 배정
        if (hasD && abcTypes.Count == 0)
        {
            dCount = row.totalPollutant;
            abcCount = 0;
        }

        if (abcCount > 0 && abcPointCount > 0)
        {
            bool[] used = new bool[abcPointCount];
            abcQueue = new Entry[abcCount];
            for (int i = 0; i < abcCount; i++)
            {
                Pollutant.PollutantType pick = abcTypes[Random.Range(0, abcTypes.Count)];
                int point = PickUnusedIndex(used, abcPointCount);
                abcQueue[i].type = pick;
                abcQueue[i].spawnPointIndex = point;
                abcQueue[i].useTypeDPoints = false;
            }
        }

        if (dCount > 0 && dPointCount > 0)
        {
            bool[] usedD = new bool[dPointCount];
            dQueue = new Entry[dCount];
            for (int i = 0; i < dCount; i++)
            {
                // 포인트가 모두 소진됐으면 재사용 허용
                bool allUsed = true;
                for (int j = 0; j < usedD.Length; j++) if (!usedD[j]) { allUsed = false; break; }
                if (allUsed) System.Array.Clear(usedD, 0, usedD.Length);

                int point = PickUnusedIndex(usedD, dPointCount);
                dQueue[i].type = Pollutant.PollutantType.TypeD;
                dQueue[i].spawnPointIndex = point;
                dQueue[i].useTypeDPoints = true;
            }
        }

        BuildSpawnQueue();
        mapPollutantCounts = BuildMapCounts(row);
        currentMapIndex = 0;
        int queueLen = spawnQueue != null ? spawnQueue.Length : 0;
        int mapLen = mapPollutantCounts != null ? mapPollutantCounts.Length : 0;
        Debug.Log($"[PollutantSpawnPlan] 스테이지 {row.label} 확정 - A~C {abcCount}개, D {dCount}개, 등장 {queueLen}개, 맵 {mapLen}구간");
    }

    private static int[] BuildMapCounts(StageJsonRow row)
    {
        if (row.mapPollutants != null && row.mapPollutants.Length > 0)
        {
            int sum = 0;
            for (int i = 0; i < row.mapPollutants.Length; i++)
                sum += row.mapPollutants[i];

            if (sum == row.totalPollutant)
                return row.mapPollutants;

            Debug.LogWarning($"[PollutantSpawnPlan] mapPollutants 합({sum}) != totalPollutant({row.totalPollutant}) — 한 맵으로 폴백");
        }

        return new int[] { row.totalPollutant };
    }

    private static void BuildSpawnQueue()
    {
        int abcLen = abcQueue != null ? abcQueue.Length : 0;
        int dLen = dQueue != null ? dQueue.Length : 0;
        if (abcLen + dLen <= 0)
        {
            spawnQueue = null;
            return;
        }

        spawnQueue = new Entry[abcLen + dLen];
        int index = 0;
        for (int i = 0; i < abcLen; i++)
            spawnQueue[index++] = abcQueue[i];
        for (int i = 0; i < dLen; i++)
            spawnQueue[index++] = dQueue[i];
    }

    private static Pollutant.PollutantType CharToType(char c)
    {
        switch (char.ToUpperInvariant(c))
        {
            case 'B': return Pollutant.PollutantType.TypeB;
            case 'C': return Pollutant.PollutantType.TypeC;
            case 'D': return Pollutant.PollutantType.TypeD;
            default: return Pollutant.PollutantType.TypeA;
        }
    }

    private static int PickUnusedIndex(bool[] used, int pointCount)
    {
        int safety = 0;
        while (safety < 32)
        {
            int index = Random.Range(0, pointCount);
            if (!used[index])
            {
                used[index] = true;
                return index;
            }
            safety++;
        }

        for (int i = 0; i < pointCount; i++)
        {
            if (!used[i])
            {
                used[i] = true;
                return i;
            }
        }

        return 0;
    }
}
