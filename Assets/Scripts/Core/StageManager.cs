using UnityEngine;

[System.Serializable]
public class StageData
{
    public string stageLabel = "Stage 1-1";
    public int totalPollutants = 3;
    public float timeLimitSeconds = 60f;
}

public class StageManager : MonoBehaviour
{
    public StageData[] stages;
    public int currentStageIndex = 0;

    public string stageLabel;
    public int totalPollutants = 3;
    public int clearedPollutants = 0;
    public StageUI stageUI;

    void Start()
    {
        if (stages == null || stages.Length == 0)
        {
            stages = new StageData[1];
            stages[0] = new StageData();
            stages[0].stageLabel = stageLabel;
            stages[0].totalPollutants = totalPollutants;
            stages[0].timeLimitSeconds = 60f;
        }

        LoadStage(currentStageIndex);
    }

    public void LoadStage(int index)
    {
        if (stages == null || stages.Length == 0)
            return;

        currentStageIndex = Mathf.Clamp(index, 0, stages.Length - 1);
        StageData data = stages[currentStageIndex];

        stageLabel = data.stageLabel;
        totalPollutants = data.totalPollutants;
        clearedPollutants = 0;
        UpdateUI();

        Debug.Log($"[StageManager] 스테이지 로드: {stageLabel} ({currentStageIndex + 1}/{stages.Length})");
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
        if (stageUI != null)
            stageUI.SetStageInfo(stageLabel, clearedPollutants, totalPollutants);
    }
}
