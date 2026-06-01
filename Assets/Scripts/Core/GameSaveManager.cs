using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public static class GameSaveManager
{
    private const string SaveFileName = "gamesave.json";

    public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static void Save(GameSaveData data)
    {
        if (data == null)
            return;

        string json = JsonConvert.SerializeObject(data);
        File.WriteAllText(SaveFilePath, json);
        Debug.Log($"[GameSaveManager] 저장: {SaveFilePath}");
    }

    public static GameSaveData Load()
    {
        if (!HasSave())
            return null;

        string json = File.ReadAllText(SaveFilePath);
        if (string.IsNullOrEmpty(json))
            return null;

        return JsonConvert.DeserializeObject<GameSaveData>(json);
    }

    public static bool HasSave()
    {
        return File.Exists(SaveFilePath);
    }

    public static void DeleteSave()
    {
        if (!File.Exists(SaveFilePath))
            return;

        File.Delete(SaveFilePath);
        Debug.Log("[GameSaveManager] 저장 파일 삭제");
    }

    public static GameSaveData BuildSaveFromClear(int clearedStageIndex, int stageCount, string label, bool hasNextStage)
    {
        int nextIndex = clearedStageIndex;
        if (hasNextStage && clearedStageIndex + 1 < stageCount)
            nextIndex = clearedStageIndex + 1;

        GameSaveData data = new GameSaveData();
        data.continueStageIndex = Mathf.Clamp(nextIndex, 0, Mathf.Max(0, stageCount - 1));
        data.highestClearedIndex = clearedStageIndex;
        data.lastStageLabel = label;
        return data;
    }
}
