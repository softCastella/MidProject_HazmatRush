using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

[System.Serializable]
public class StageData
{
    public string stageLabel = "Stage 1-1";
    public string placeName = "1F A area 1-1";
    public int totalPollutants = 3;
    public float timeLimitSeconds = 60f;
    public string pollutantTypes = "A";
    public int backgroundIndex = 0;
    public int bgmIndex = 0;
}

[System.Serializable]
public class StageJsonRow
{
    public int stageId;
    public string label;
    public string placeName;
    public string pollutantTypes;
    public int totalPollutant;
    public int[] mapPollutants;
    public int timeLimit;
    public int bgIndex;
    public int bgmIndex;
}

public class StageManager : MonoBehaviour
{
    public TextAsset stageDataJson;
    public StageData[] stages;
    public int currentStageIndex = 0;

    public string stageLabel;
    public string placeName = "1F A area 1-1";
    public int totalPollutants = 3;
    public int clearedPollutants = 0;

    [Header("HUD (StageInfo 하위 텍스트)")]
    public TMP_Text stageText;
    public TMP_Text pollutantCountText;

    void Start()
    {
        TryLoadStagesFromJson();

        if (stages == null || stages.Length == 0)
        {
            stages = new StageData[1];
            stages[0] = new StageData();
            stages[0].stageLabel = stageLabel;
            stages[0].placeName = placeName;
            stages[0].totalPollutants = totalPollutants;
            stages[0].timeLimitSeconds = 60f;
        }

        int startIndex = 0;
        if (SceneLoadManager.Instance != null && SceneLoadManager.Instance.pendingStageIndex >= 0)
            startIndex = SceneLoadManager.Instance.pendingStageIndex;

        LoadStage(startIndex);

        if (SceneLoadManager.Instance != null)
            SceneLoadManager.Instance.pendingStageIndex = -1;
    }

    private void TryLoadStagesFromJson()
    {
        if (stageDataJson == null || string.IsNullOrEmpty(stageDataJson.text))
            return;

        List<StageJsonRow> rows = JsonConvert.DeserializeObject<List<StageJsonRow>>(stageDataJson.text);
        if (rows == null || rows.Count == 0)
        {
            Debug.LogWarning("[StageManager] stage_data.json 파싱 결과가 비어 있습니다.");
            return;
        }

        stages = new StageData[rows.Count];
        for (int i = 0; i < rows.Count; i++)
        {
            StageJsonRow row = rows[i];
            stages[i] = new StageData();
            stages[i].stageLabel = row.label;
            stages[i].placeName = row.placeName;
            stages[i].totalPollutants = row.totalPollutant;
            stages[i].timeLimitSeconds = row.timeLimit;
            stages[i].pollutantTypes = row.pollutantTypes;
            stages[i].backgroundIndex = row.bgIndex;
            stages[i].bgmIndex = row.bgmIndex;
        }

        Debug.Log($"[StageManager] JSON에서 스테이지 {stages.Length}개 로드");
    }

    public void LoadStage(int index)
    {
        if (stages == null || stages.Length == 0)
            return;

        currentStageIndex = Mathf.Clamp(index, 0, stages.Length - 1);
        StageData data = stages[currentStageIndex];

        stageLabel = data.stageLabel;
        placeName = data.placeName;
        totalPollutants = data.totalPollutants;
        clearedPollutants = 0;
        UpdateUI();

        Timer timer = FindAnyObjectByType<Timer>();
        if (timer != null)
            timer.SetStartTime(data.timeLimitSeconds);

        Background background = FindAnyObjectByType<Background>();
        if (background != null)
            background.ChangeBackgroundByIndex(data.backgroundIndex);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayStageBgm(data.bgmIndex);

        Debug.Log($"[StageManager] 스테이지 로드: {stageLabel} {placeName} ({currentStageIndex + 1}/{stages.Length}) bg={data.backgroundIndex} bgm={data.bgmIndex} types={data.pollutantTypes}");
    }

    public int GetCurrentBgmIndex()
    {
        if (stages == null || stages.Length == 0)
            return 0;
        return stages[currentStageIndex].bgmIndex;
    }

    public string GetCurrentPollutantTypes()
    {
        if (stages == null || stages.Length == 0)
            return "";

        return stages[currentStageIndex].pollutantTypes;
    }

    public int GetStageCount()
    {
        if (stages == null)
            return 0;
        return stages.Length;
    }

    public float GetCurrentTimeLimit()
    {
        if (stages == null || stages.Length == 0)
            return 60f;
        return stages[currentStageIndex].timeLimitSeconds;
    }

    public bool HasNextStage()
    {
        if (stages == null)
            return false;
        return currentStageIndex < stages.Length - 1;
    }

    public void RestartCurrentStage()
    {
        LoadStage(currentStageIndex);
    }

    public void GoToNextStage()
    {
        if (!HasNextStage())
            return;
        LoadStage(currentStageIndex + 1);
    }

    public void AddClearedPollutant()
    {
        clearedPollutants = Mathf.Min(clearedPollutants + 1, totalPollutants);
        UpdateUI();
    }

    public bool IsAllCleared()
    {
        return clearedPollutants >= totalPollutants;
    }

    private void UpdateUI()
    {
        if (stageText != null)
            stageText.text = $"{stageLabel} {placeName}";

        if (pollutantCountText != null)
            pollutantCountText.text = $"오염원: {clearedPollutants}/{totalPollutants}";
    }
}
