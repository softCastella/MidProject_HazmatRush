using UnityEngine;
using TMPro;

[DefaultExecutionOrder(100)]
public class Timer : MonoBehaviour
{
    [Header("Timer 설정")]
    public float startSeconds = 120f; // 시작 시간 (초)
    public bool isRunning = false; // 카운트 다운 동작 여부

    [Header("현재 시간")]
    public float currentSeconds = 0f; // 현재 남은 시간
    public TMP_Text timeText; // 표시할 TMP 텍스트

    [Header("잔여 시간 경고 SFX")]
    [Tooltip("HUD 표시 초가 이 값 이하(1 이상)일 때 1회 재생. 0이면 비활성.")]
    public int warningSfxStartSeconds = 16;
    public AudioClip warningSfxClip;
    [Range(0f, 3f)]
    public float warningSfxVolume = 1f;

    [Header("잔여 시간 경고 색상")]
    [Tooltip("warningSfxStartSeconds 이하일 때 숫자 색. 기본 흰색은 timeText 시작 색.")]
    public Color warningTextColor = Color.red;

    private bool timedOut = false; // 타임오버 1회 처리 여부
    private Color defaultTextColor = Color.white;
    private bool warningSfxStarted = false;
    private bool warningSfxPaused = false;
    private AudioSource warningSfxSource;

    void Start()
    {
        if (timeText != null)
            defaultTextColor = timeText.color;

        currentSeconds = startSeconds;
        UpdateTimeText();
    }

    void Update()
    {
        if (GameManager.Instance != null && (GameManager.Instance.GameEnded || GameManager.Instance.IsPenalty))
        {
            StopWarningSfx();
            return;
        }

        if (isRunning)
        {
            currentSeconds -= UnityEngine.Time.deltaTime;
            if (currentSeconds <= 0f)
            {
                currentSeconds = 0f;
                isRunning = false;
                UpdateTimeText();
                StopWarningSfx();

                if (!timedOut)
                {
                    timedOut = true;
                    if (GameManager.Instance != null)
                        GameManager.Instance.TriggerGameOver(GameManager.GameOverCause.TimeOver);
                }
                return;
            }
        }

        UpdateTimeText();
    }

    public void StartCountdown()
    {
        StopWarningSfx();
        currentSeconds = startSeconds;
        isRunning = true;
        timedOut = false;
        UpdateTimeText();
    }

    public void StopCountdown()
    {
        isRunning = false;
        StopWarningSfx();
    }

    public void PauseCountdown()
    {
        isRunning = false;
        PauseWarningSfx();
    }

    public void ResumeCountdown()
    {
        isRunning = true;
        UnpauseWarningSfx();
    }

    public void SetStartTime(float seconds)
    {
        startSeconds = Mathf.Max(0f, seconds);
        currentSeconds = startSeconds;
        UpdateTimeText();
    }

    public void AddSeconds(float seconds)
    {
        if (seconds <= 0f)
            return;
        if (GameManager.Instance != null && GameManager.Instance.GameEnded)
            return;

        currentSeconds += seconds;
        UpdateTimeText();
    }

    private void UpdateTimeText()
    {
        int displaySeconds = Mathf.CeilToInt(currentSeconds);

        if (timeText != null)
        {
            timeText.text = displaySeconds.ToString();

            bool inWarningZone = warningSfxStartSeconds > 0
                && displaySeconds <= warningSfxStartSeconds;
            timeText.color = inWarningZone ? warningTextColor : defaultTextColor;
        }

        UpdateWarningSfx();
    }

    private void UpdateWarningSfx()
    {
        if (warningSfxStartSeconds <= 0 || warningSfxClip == null)
        {
            StopWarningSfx();
            return;
        }

        int displaySeconds = Mathf.CeilToInt(currentSeconds);
        bool inWarningZone = displaySeconds >= 1 && displaySeconds <= warningSfxStartSeconds;

        if (inWarningZone && isRunning && !warningSfxStarted)
            StartWarningSfx();
        else if (displaySeconds > warningSfxStartSeconds)
            StopWarningSfx();
    }

    private void PauseWarningSfx()
    {
        if (warningSfxSource == null || !warningSfxStarted)
            return;

        if (warningSfxSource.isPlaying)
        {
            warningSfxSource.Pause();
            warningSfxPaused = true;
        }
    }

    private void UnpauseWarningSfx()
    {
        if (warningSfxSource == null || !warningSfxStarted || !warningSfxPaused)
            return;

        warningSfxSource.UnPause();
        warningSfxPaused = false;
    }

    private void EnsureWarningSfxSource()
    {
        if (warningSfxSource != null)
            return;

        warningSfxSource = gameObject.AddComponent<AudioSource>();
        warningSfxSource.playOnAwake = false;
        warningSfxSource.loop = false;
    }

    private void StartWarningSfx()
    {
        if (warningSfxClip == null || warningSfxStarted)
            return;

        EnsureWarningSfxSource();
        warningSfxSource.clip = warningSfxClip;
        warningSfxSource.volume = warningSfxVolume;
        warningSfxSource.loop = false;
        warningSfxSource.Play();
        warningSfxStarted = true;
        warningSfxPaused = false;
    }

    private void StopWarningSfx()
    {
        if (warningSfxSource != null)
        {
            warningSfxSource.Stop();
            warningSfxPaused = false;
        }

        warningSfxStarted = false;
    }
}
