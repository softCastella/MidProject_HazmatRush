using System.Collections;
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
    public PollutantManager pollutantManager;

    [Header("Result Panels")]
    public GameObject clearSet;     // HUD_Canvas/Result_HUD/ClearSet
    public ClearPanelUI clearPanelUI;
    public GameObject gameOverSet;  // HUD_Canvas/Result_HUD/GameOverSet
    public GameObject nextStageButton; // ClearSet/nextStageBtn

    [Header("Stage Score")]
    public StageScoreTracker stageScoreTracker;

    [Header("Pause")]
    public GameObject pauseSet;     // HUD_Canvas/Pause_HUD

    [Header("Wrong Item Penalty (Stage 1-1 튜토리얼 전용)")]
    public GuideTxt guideTxt;                       // 패널티 안내 문구 표시
    public ItemManager itemManager;                 // 아이템 창 전체 dim
    public ItemSelectManager itemSelectManager;     // 패널티 종료 후 아이템 UI 복구
    public RecoveryItemInventoryUI recoveryInventoryUI;
    [TextArea]
    public string wrongItemPenaltyMessage = "잘못된 아이템 선택으로 2초 후에 아이템선택이 가능합니다.";
    public float wrongItemPenaltySeconds = 2f;

    [Header("Clear")]
    [Tooltip("클리어 애니 재생 후 결과 패널까지 대기(초).")]
    public float clearAnimDelay = 2f;

    [Header("Game Over")]
    [Tooltip("사망 Die 연출 유지(초). 이후 Player 페이드 → 페이드 후 대기 → 게임오버 패널.")]
    public float dieAnimDelay = 2f;
    [Tooltip("사망 페이드 종료 후 게임오버 패널까지 추가 대기(초).")]
    public float diePanelDelayAfterFade = 1f;
    [Tooltip("사망 패널 코루틴 실패 시 비상 대기(초).")]
    public float dieGameOverFallbackDelay = 5f;

    [Header("Debug")]
    public bool enableDebugKeys = true; // F1: 강제 클리어, F2: 강제 게임오버
    public bool enableDebugSaveKeys = true; // F3: 저장 로그, F4: 저장 로드 후 스테이지 적용

    private bool gameEnded = false;
    public bool GameEnded => gameEnded;

    private bool gameOverPending = false;
    public bool IsGameOverPending => gameOverPending;
    private GameOverCause pendingDeathCause = GameOverCause.ProtectionDepleted;
    private Coroutine gameOverDelayRoutine;
    private Coroutine stageClearRoutine;

    private bool isPaused = false;
    public bool IsPaused => isPaused;

    private bool isPenalty = false;
    public bool IsPenalty => isPenalty;

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
        if (pollutantManager == null)
            pollutantManager = FindAnyObjectByType<PollutantManager>();
        if (guideTxt == null)
            guideTxt = FindAnyObjectByType<GuideTxt>();
        if (itemManager == null)
            itemManager = FindAnyObjectByType<ItemManager>();
        if (itemSelectManager == null)
            itemSelectManager = FindAnyObjectByType<ItemSelectManager>();
        if (recoveryInventoryUI == null)
            recoveryInventoryUI = FindAnyObjectByType<RecoveryItemInventoryUI>();
        if (stageScoreTracker == null)
            stageScoreTracker = FindAnyObjectByType<StageScoreTracker>();
        if (clearPanelUI == null && clearSet != null)
            clearPanelUI = clearSet.GetComponent<ClearPanelUI>();

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

        if (enableDebugKeys)
        {
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

        if (!enableDebugSaveKeys || stageManager == null)
            return;

        if (Input.GetKeyDown(KeyCode.F3))
        {
            if (GameSaveManager.HasSave())
                Debug.Log($"[GameManager] (디버그) 저장 있음: {GameSaveManager.SaveFilePath}");
            else
                Debug.Log("[GameManager] (디버그) 저장 파일 없음");
        }
        else if (Input.GetKeyDown(KeyCode.F4))
        {
            GameSaveData data = GameSaveManager.Load();
            if (data == null)
            {
                Debug.Log("[GameManager] (디버그) 로드할 저장 없음");
                return;
            }

            stageManager.LoadStage(data.continueStageIndex);
            if (gameEnded)
                ResumeAfterResult();
            else
                ResetStagePlay(false);

            Debug.Log($"[GameManager] (디버그) 저장 로드 → 스테이지 index={data.continueStageIndex} ({data.lastStageLabel})");
        }
    }

    // ESC 또는 UI 버튼으로 일시정지/재개 토글
    public void TogglePause()
    {
        if (isPenalty)
            return;

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

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetBgmPauseDim(true);

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

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetBgmPauseDim(false);

        Debug.Log("[GameManager] 재개");
    }

    // 중화모드에서 틀린 아이템으로 접촉했을 때 호출. (스테이지 1-1 튜토리얼 전용)
    public bool TriggerWrongItemPenalty()
    {
        if (isPenalty || gameEnded || isPaused)
            return false;

        // 첫 스테이지(1-1)에서만 패널티 안내가 동작합니다.
        if (stageManager == null || stageManager.currentStageIndex != 0)
            return false;

        StartCoroutine(WrongItemPenaltyRoutine());
        return true;
    }

    // 2초간 키 입력·타이머 정지 + HUD dim. 접촉 중 방호복 감소는 계속됩니다.
    private IEnumerator WrongItemPenaltyRoutine()
    {
        isPenalty = true;

        if (itemManager != null)
            itemManager.SetAllDim(true);
        if (recoveryInventoryUI != null)
            recoveryInventoryUI.SetAllDim(true);
        if (guideTxt != null)
            guideTxt.ShowGuideImmediate(wrongItemPenaltyMessage);

        Debug.Log("[GameManager] 오대응 패널티 시작 - 키·타이머 정지, 방호복 감소 유지");

        yield return new WaitForSeconds(wrongItemPenaltySeconds);

        if (guideTxt != null)
            guideTxt.HideGuide();
        if (itemSelectManager != null)
            itemSelectManager.RefreshUI();
        else if (itemManager != null)
            itemManager.SetAllDim(false);

        if (recoveryInventoryUI != null)
        {
            recoveryInventoryUI.SetAllDim(false);
            RecoveryItemInventory inventory = FindAnyObjectByType<RecoveryItemInventory>();
            if (inventory != null)
                recoveryInventoryUI.Refresh(inventory);
        }

        isPenalty = false;
        Debug.Log("[GameManager] 오대응 패널티 종료 - 키·타이머 재개");
    }

    // 모든 오염원 중화 + 방호구 1 이상 + 타이머 잔여 시 클리어
    public void ScheduleStageClear(float pollutantFadeDelay)
    {
        if (gameEnded)
            return;

        if (stageClearRoutine != null)
            StopCoroutine(stageClearRoutine);

        if (timer != null)
            timer.StopCountdown();

        stageClearRoutine = StartCoroutine(ScheduleStageClearRoutine(pollutantFadeDelay));
    }

    private IEnumerator ScheduleStageClearRoutine(float pollutantFadeDelay)
    {
        if (pollutantFadeDelay > 0f)
            yield return new WaitForSecondsRealtime(pollutantFadeDelay);

        stageClearRoutine = null;
        TriggerClear();
    }

    public void TriggerClear()
    {
        if (gameEnded)
            return;

        bool protectionOk = player == null || player.curProtection >= 1f;
        bool timerOk = timer == null || timer.currentSeconds > 0f;
        if (!protectionOk || !timerOk)
            return;

        ShowClear(false);
    }

    // 디버그: 조건 무시하고 강제 클리어 (별 3개)
    public void ForceClear()
    {
        if (gameEnded)
            return;
        ShowClear(true);
    }

    public void RestartCurrentStage()
    {
        if (!gameEnded)
            return;

        if (stageManager != null)
            stageManager.RestartCurrentStage();
        ResetStagePlay(true);
        ResumeAfterResult();
        Debug.Log("[GameManager] 현재 스테이지 재시작");
    }

    public void GoToNextStage()
    {
        if (!gameEnded || stageManager == null)
            return;
        if (!stageManager.HasNextStage())
            return;

        stageManager.GoToNextStage();
        ResetStagePlay(false);
        ResumeAfterResult();
        Debug.Log("[GameManager] 다음 스테이지");
    }

    public void GoToTitleFromClear()
    {
        Debug.Log("[GameManager] GoToTitleFromClear 클릭");
        if (SceneLoadManager.Instance == null)
        {
            Debug.LogWarning("[GameManager] SceneLoadManager 없음");
            return;
        }

        SceneLoadManager.Instance.TitleButton();
    }

    private void ResumeAfterResult()
    {
        CancelPendingGameOver();
        CancelStageClearRoutine();
        gameEnded = false;
        isPaused = false;
        Time.timeScale = 1f;

        if (clearSet != null)
            clearSet.SetActive(false);
        if (gameOverSet != null)
            gameOverSet.SetActive(false);
        if (pauseSet != null)
            pauseSet.SetActive(false);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBgmPauseDim(false);
            if (stageManager != null)
                AudioManager.Instance.PlayStageBgm(stageManager.GetCurrentBgmIndex());
            else
                AudioManager.Instance.PlayGameBGM();
        }
    }

    private void ResetStagePlay(bool clearRecoveryInventory)
    {
        if (stageScoreTracker != null)
            stageScoreTracker.Reset();

        if (player != null)
            player.ResetForStage();
        if (pollutantManager != null)
            pollutantManager.ResetForStage(clearRecoveryInventory);
        if (timer != null && stageManager != null)
        {
            timer.SetStartTime(stageManager.GetCurrentTimeLimit());
            timer.StartCountdown();
        }
    }

    private void FreezePlayOnResult()
    {
        if (timer != null)
            timer.StopCountdown();
        if (player != null)
            player.StopMovement();
        if (pollutantManager != null)
            pollutantManager.StopReturnFlow();
    }

    private void ShowClear(bool perfectStars)
    {
        gameEnded = true;
        FreezePlayOnResult();

        RecoveryItemManager recoveryManager = FindAnyObjectByType<RecoveryItemManager>();
        if (recoveryManager != null)
        {
            recoveryManager.AutoCollectMapRecoveryItems();
            recoveryManager.ClearMapRecoveryItems();
        }

        if (player != null)
            player.PlayClearAnim();
        StartCoroutine(ShowClearPanelAfterDelay(perfectStars));
    }

    private IEnumerator ShowClearPanelAfterDelay(bool perfectStars)
    {
        if (clearAnimDelay > 0f)
            yield return new WaitForSeconds(clearAnimDelay);

        if (player != null)
            player.FreezeClearAnim();

        if (clearSet != null)
            clearSet.SetActive(true);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
            AudioManager.Instance.PlayClearSfx();
        }
        if (nextStageButton != null)
            nextStageButton.SetActive(stageManager != null && stageManager.HasNextStage());

        StageClearResult result = new StageClearResult();
        if (stageScoreTracker != null)
        {
            if (perfectStars)
                result = stageScoreTracker.BuildPerfectResult(player, timer, stageManager);
            else
                result = stageScoreTracker.BuildResult(player, timer, stageManager);
        }

        if (clearPanelUI != null)
            clearPanelUI.Apply(result);

        if (perfectStars)
            Debug.Log($"[GameManager] (디버그) 강제 클리어 - 별 {result.starCount}/3");
        else
            Debug.Log($"[GameManager] 스테이지 클리어 - 별 {result.starCount}/3 / 오염원 {result.clearedPollutants}/{result.totalPollutants} / 방호복 {result.protectionPercent}% / 틀린 아이템 {result.wrongItemCount}회 / 남은 시간 {result.remainSeconds:00}초");

        if (stageManager != null)
        {
            GameSaveData saveData = GameSaveManager.BuildSaveFromClear(
                stageManager.currentStageIndex,
                stageManager.GetStageCount(),
                stageManager.stageLabel,
                stageManager.HasNextStage());
            GameSaveManager.Save(saveData);
        }
    }

    // 방호구 0 / 타임오버 / 시간 내 미정화 시 게임오버
    public void TriggerGameOver(GameOverCause cause)
    {
        if (gameEnded || gameOverPending)
            return;

        if (cause == GameOverCause.ProtectionDepleted)
            return;

        if (cause == GameOverCause.TimeOver && player != null && !player.IsDead)
        {
            BeginPlayerDeathSequence(GameOverCause.TimeOver);
            player.StartDeathSequence();
            return;
        }

        FinishGameOver(cause);
    }

    // Die 애니 연출 시작 (패널은 dieAnimDelay + 페이드 후 ShowGameOverPanelAfterDeathAnim에서)
    public void BeginPlayerDeathSequence(GameOverCause cause = GameOverCause.ProtectionDepleted)
    {
        if (gameEnded || gameOverPending)
            return;

        pendingDeathCause = cause;
        gameOverPending = true;
        FreezePlayOnResult();
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopNeutralizationSfx();
            AudioManager.Instance.StopValveSfx();
        }

        if (gameOverDelayRoutine != null)
            StopCoroutine(gameOverDelayRoutine);
        gameOverDelayRoutine = StartCoroutine(ShowGameOverPanelAfterDeathAnim());

        if (dieGameOverFallbackDelay > 0f)
            StartCoroutine(PlayerDeathFallbackRoutine());
    }

    private IEnumerator ShowGameOverPanelAfterDeathAnim()
    {
        if (dieAnimDelay > 0f)
            yield return new WaitForSeconds(dieAnimDelay);

        float fadeDuration = 0.5f;
        if (player != null)
            fadeDuration = player.dieFadeDuration;
        if (fadeDuration > 0f)
            yield return new WaitForSeconds(fadeDuration);

        if (diePanelDelayAfterFade > 0f)
            yield return new WaitForSeconds(diePanelDelayAfterFade);

        gameOverDelayRoutine = null;
        if (gameEnded || !gameOverPending)
            yield break;

        CompletePlayerDeathSequence();
    }

    public void CompletePlayerDeathSequence()
    {
        if (gameEnded)
            return;

        CancelPendingGameOver();
        FinishGameOver(pendingDeathCause);
    }

    private IEnumerator PlayerDeathFallbackRoutine()
    {
        yield return new WaitForSeconds(dieGameOverFallbackDelay);
        gameOverDelayRoutine = null;
        if (gameEnded || !gameOverPending)
            yield break;

        Debug.LogWarning("[GameManager] 사망 연출 타임아웃 → 게임오버 패널 표시");
        CompletePlayerDeathSequence();
    }

    private void FinishGameOver(GameOverCause cause)
    {
        if (gameEnded)
            return;

        gameEnded = true;
        FreezePlayOnResult();
        if (gameOverSet != null)
            gameOverSet.SetActive(true);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
            AudioManager.Instance.StopNeutralizationSfx();
            AudioManager.Instance.StopValveSfx();
            AudioManager.Instance.PlayGameOverSfx();
        }

        Debug.Log($"[GameManager] 게임 오버 - 원인: {GetCauseText(cause)}");
    }

    private void CancelPendingGameOver()
    {
        gameOverPending = false;
        if (gameOverDelayRoutine != null)
        {
            StopCoroutine(gameOverDelayRoutine);
            gameOverDelayRoutine = null;
        }
    }

    private void CancelStageClearRoutine()
    {
        if (stageClearRoutine == null)
            return;

        StopCoroutine(stageClearRoutine);
        stageClearRoutine = null;
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
