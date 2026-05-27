using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    [Header("Countdown 설정")]
    public float startSeconds = 60f; // 시작 시간 (초)
    public bool isRunning = false; // 카운트 다운 동작 여부

    [Header("현재 시간")]
    public float currentSeconds = 0f; // 현재 남은 시간
    public TMP_Text timeText; // 표시할 TMP 텍스트

    void Start()
    {
        currentSeconds = startSeconds;
        isRunning = false;
        UpdateTimeText();
    }

    void Update()
    {
        if (currentSeconds <= 0f)
        {
            currentSeconds = 0f;
            isRunning = false;
        }

        if (isRunning)
        {
            currentSeconds -= UnityEngine.Time.deltaTime;
            if (currentSeconds < 0f)
                currentSeconds = 0f;
        }

        UpdateTimeText();
    }

    public void StartCountdown()
    {
        currentSeconds = startSeconds;
        isRunning = true;
        UpdateTimeText();
    }

    public void StopCountdown()
    {
        isRunning = false;
    }

    public void SetStartTime(float seconds)
    {
        startSeconds = Mathf.Max(0f, seconds);
        currentSeconds = startSeconds;
        UpdateTimeText();
    }

    private void UpdateTimeText()
    {
        if (timeText == null)
            return;

        int displaySeconds = Mathf.CeilToInt(currentSeconds);
        timeText.text = displaySeconds.ToString();
    }
}
