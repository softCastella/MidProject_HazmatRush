using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameOverCause
    {
        ProtectionDepleted, // 방호복 내구도 소진
        TimeOver,           // 제한 시간 초과(시간 내 미정화 포함)
        Debug               // 디버그 강제 종료
    }

    [Header("References")]
    public StageManager stageManager;
    public Timer timer;
    public Player player;

    [Header("Result Panels")]
    public GameObject clearSet;     // HUD_Canvas/Result_HUD/ClearSet
    public GameObject gameOverSet;  // HUD_Canvas/Result_HUD/GameOverSet

    [Header("Pause")]
    public GameObject pauseSet;     // HUD_Canvas/Pause_HUD

    [Header("Debug")]
    public bool enableDebugKeys = true; // F1: 강제 클리어, F2: 강제 게임오버

    private bool gameEnded = false;
    public bool GameEnded => gameEnded;

    private bool isPaused = false;
    public bool IsPaused => isPaused;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;

        if (stageManager == null)
            stageManager = FindAnyObjectByType<StageManager>();
        if (timer == null)
            timer = FindAnyObjectByType<Timer>();
        if (player == null)
            player = FindAnyObjectByType<Player>();

        if (clearSet != null)
            clearSet.SetActive(false);
        if (gameOverSet != null)
            gameOverSet.SetActive(false);
        if (pauseSet != null)
            pauseSet.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (gameEnded)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();

        if (isPaused)
            return;

        if (!enableDebugKeys)
            return;

        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log("[GameManager] (디버그) 강제 클리어");
            ForceClear();
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("[GameManager] (디버그) 강제 게임오버");
            TriggerGameOver(GameOverCause.Debug);
        }
    }

    // ESC 또는 UI 버튼으로 일시정지/재개 토글
    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        if (gameEnded || isPaused)
            return;

        isPaused = true;
        Time.timeScale = 0f;
        if (pauseSet != null)
            pauseSet.SetActive(true);

        Debug.Log("[GameManager] 일시정지");
    }

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        isPaused = false;
        Time.timeScale = 1f;
        if (pauseSet != null)
            pauseSet.SetActive(false);

        Debug.Log("[GameManager] 재개");
    }

    // 모든 오염원 중화 + 방호구 1 이상 + 타이머 잔여 시 클리어
    public void TriggerClear()
    {
        if (gameEnded)
            return;

        bool protectionOk = player == null || player.curProtection >= 1f;
        bool timerOk = timer == null || timer.currentSeconds > 0f;
        if (!protectionOk || !timerOk)
            return;

        ShowClear();
    }

    // 디버그: 조건 무시하고 강제 클리어
    public void ForceClear()
    {
        if (gameEnded)
            return;
        ShowClear();
    }

    private void ShowClear()
    {
        gameEnded = true;
        if (timer != null)
            timer.StopCountdown();
        if (player != null)
            player.canMove = false;
        if (clearSet != null)
            clearSet.SetActive(true);

        int cleared = stageManager != null ? stageManager.clearedPollutants : 0;
        int total = stageManager != null ? stageManager.totalPollutants : 0;
        int remainSec = timer != null ? Mathf.CeilToInt(timer.currentSeconds) : 0;
        int protection = player != null ? Mathf.FloorToInt(player.curProtection) : 0;

        Debug.Log($"[GameManager] 스테이지 클리어 - 오염원수 {cleared}/{total} / 남은 시간: {remainSec:00}초 / 방호복 내구도: {protection:00}%");
    }

    // 방호구 0 / 타임오버 / 시간 내 미정화 시 게임오버
    public void TriggerGameOver(GameOverCause cause)
    {
        if (gameEnded)
            return;

        gameEnded = true;
        if (timer != null)
            timer.StopCountdown();
        if (player != null)
            player.canMove = false;
        if (gameOverSet != null)
            gameOverSet.SetActive(true);

        Debug.Log($"[GameManager] 게임 오버 - 원인: {GetCauseText(cause)}");
    }

    private string GetCauseText(GameOverCause cause)
    {
        return cause switch
        {
            GameOverCause.ProtectionDepleted => "방호복 내구도 소진",
            GameOverCause.TimeOver => "시간 초과 (제한 시간 내 정화 실패)",
            GameOverCause.Debug => "디버그 강제 종료",
            _ => "알 수 없음"
        };
    }
}
