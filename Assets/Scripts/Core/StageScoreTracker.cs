using UnityEngine;

[System.Serializable]
public struct StageClearResult
{
    public bool starClear;
    public bool starSafety;
    public bool starAccuracy;
    public int starCount;

    public int clearedPollutants;
    public int totalPollutants;
    public int protectionPercent;
    public int safetyTargetPercent;
    public int wrongItemCount;
    public int wrongItemTarget;
    public int pollutantResetCount;
    public int remainSeconds;

    public string[] breakdownLines;
}

public class StageScoreTracker : MonoBehaviour
{
    public int safetyTargetPercent = 50;
    public int wrongItemTarget = 0;

    private int wrongItemCount;
    private int pollutantResetCount;

    void Awake()
    {
        Reset();
    }

    public void Reset()
    {
        wrongItemCount = 0;
        pollutantResetCount = 0;
    }

    public void RegisterWrongItem()
    {
        wrongItemCount++;
    }

    public void RegisterPollutantReset()
    {
        pollutantResetCount++;
    }

    public StageClearResult BuildResult(Player player, Timer timer, StageManager stageManager)
    {
        StageClearResult result = new StageClearResult();

        result.clearedPollutants = stageManager != null ? stageManager.clearedPollutants : 0;
        result.totalPollutants = stageManager != null ? stageManager.totalPollutants : 0;
        result.protectionPercent = player != null ? Mathf.FloorToInt(player.curProtection) : 0;
        result.remainSeconds = timer != null ? Mathf.CeilToInt(timer.currentSeconds) : 0;
        result.wrongItemCount = wrongItemCount;
        result.pollutantResetCount = pollutantResetCount;
        result.safetyTargetPercent = safetyTargetPercent;
        result.wrongItemTarget = wrongItemTarget;

        result.starClear = result.clearedPollutants >= result.totalPollutants && result.totalPollutants > 0;
        result.starSafety = result.protectionPercent >= safetyTargetPercent;
        result.starAccuracy = result.wrongItemCount <= result.wrongItemTarget;

        result.starCount = 0;
        if (result.starClear)
            result.starCount++;
        if (result.starSafety)
            result.starCount++;
        if (result.starAccuracy)
            result.starCount++;

        result.breakdownLines = BuildBreakdownLines(result);
        return result;
    }

    public StageClearResult BuildPerfectResult(Player player, Timer timer, StageManager stageManager)
    {
        StageClearResult result = BuildResult(player, timer, stageManager);

        if (result.totalPollutants > 0)
            result.clearedPollutants = result.totalPollutants;
        if (result.protectionPercent < safetyTargetPercent)
            result.protectionPercent = safetyTargetPercent;
        result.wrongItemCount = 0;

        result.starClear = true;
        result.starSafety = true;
        result.starAccuracy = true;
        result.starCount = 3;
        result.breakdownLines = BuildBreakdownLines(result);
        return result;
    }

    private string[] BuildBreakdownLines(StageClearResult result)
    {
        string clearMark = result.starClear ? "[달성]" : "[미달]";
        string safetyMark = result.starSafety ? "[달성]" : "[미달]";
        string accuracyMark = result.starAccuracy ? "[달성]" : "[미달]";

        return new[]
        {
            $"{clearMark} 오염원: {result.clearedPollutants}/{result.totalPollutants}",
            $"{safetyMark} 방호복: {result.protectionPercent}% (목표 {result.safetyTargetPercent}%)",
            $"{accuracyMark} 틀린 아이템: {result.wrongItemCount}회 (목표 {result.wrongItemTarget}회)"
        };
    }
}
