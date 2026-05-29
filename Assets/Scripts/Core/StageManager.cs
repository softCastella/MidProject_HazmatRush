using UnityEngine;
using TMPro;

public class StageManager : MonoBehaviour
{
    public string stageLabel = "Stage 1-1 IF A area";
    public int totalPollutants = 3;
    public int clearedPollutants = 0;
    public StageUI stageUI;

    void Start()
    {
        UpdateUI();
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