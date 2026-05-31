using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// PollutantManager는 오염원 생성만 담당하는 간단한 클래스입니다.
public class PollutantManager : MonoBehaviour
{
    public Player player;
    public Timer timer;
    public StageManager stageManager;
    public ItemSelectManager itemSelectManager;
    public WarningTxt warningTxt;
    public GuideTxt guideTxt;
    public float itemSelectHintDuration = 2f;
    public GameObject[] pollutants; // 등록된 오염원 프리팹 목록
    public PollutantSpawner spawner;
    public Background scroll;
    public PopupUI popupUI;
    public Slider pollutantSlider;
    public float rangeBuffer = 0.5f;
    public Vector2 timeRange = new Vector2(2f, 3f);
    public float spawnFadeDuration = 0.7f;
    public float despawnFadeDuration = 0.7f;
    public float popupShowDuration = 1.5f;
    private bool awaitingSpawn = false;
    private bool pollutantSpawned = false;
    private bool returningToStart = false;

    // 현재 누적된 이동 시간. Player가 이동 중일 때만 시간 누적을 합니다.
    private float moveTime = 0f;

    // 다음 생성 시점까지 필요한 시간
    private float nextSpawnTime;

    public void StopReturnFlow()
    {
        StopAllCoroutines();
        if (player != null)
            player.StopAllCoroutines();
        returningToStart = false;
        awaitingSpawn = false;
        pollutantSpawned = false;
    }

    public void ResetForStage()
    {
        StopReturnFlow();
        moveTime = 0f;
        nextSpawnTime = Random.Range(timeRange.x, timeRange.y);

        Pollutant[] activePollutants = FindObjectsByType<Pollutant>(FindObjectsSortMode.None);
        for (int i = 0; i < activePollutants.Length; i++)
        {
            if (activePollutants[i] != null)
                Destroy(activePollutants[i].gameObject);
        }

        if (warningTxt != null)
            warningTxt.HideWarning();
        if (guideTxt != null)
            guideTxt.HideGuide();
        if (scroll != null)
            scroll.ResumeScroll();
        if (itemSelectManager != null)
            itemSelectManager.ResetToDefault();
    }

    void Awake()
    {
        // Player를 Inspector에 할당하지 않았다면 씬에서 자동으로 검색합니다.
        if (player == null)
            player = FindAnyObjectByType<Player>();

        // Timer을 Inspector에 할당하지 않았다면 씬에서 자동으로 검색합니다.
        if (timer == null)
            timer = FindAnyObjectByType<Timer>();

        // StageManager를 Inspector에 할당하지 않았다면 씬에서 자동으로 검색합니다.
        if (stageManager == null)
            stageManager = FindAnyObjectByType<StageManager>();

        // ItemSelectManager를 Inspector에 할당하지 않았다면 씬에서 자동으로 검색합니다.
        if (itemSelectManager == null)
            itemSelectManager = FindAnyObjectByType<ItemSelectManager>();

        // WarningTxt를 Inspector에 할당하지 않았다면 씬에서 자동으로 검색합니다.
        if (warningTxt == null)
            warningTxt = FindAnyObjectByType<WarningTxt>();
        if (warningTxt == null)
            Debug.LogWarning("PollutantManager: WarningTxt를 찾을 수 없습니다. Inspector에 할당하거나 이름이 정확한지 확인하세요.");

        if (guideTxt == null)
            guideTxt = FindAnyObjectByType<GuideTxt>();

        if (spawner == null)
            spawner = FindAnyObjectByType<PollutantSpawner>();

        if (scroll == null)
            scroll = FindAnyObjectByType<Background>();

        nextSpawnTime = Random.Range(timeRange.x, timeRange.y);
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.GameEnded)
            return;

        // Player가 없으면 생성 로직을 실행하지 않습니다.
        if (player == null)
            return;

        // 시작 지점으로 복귀 중이면 다른 로직을 멈춥니다.
        if (returningToStart)
            return;

        // Player가 이동 중일 때만 누적 시간을 증가시킵니다.
        if (player.isMoving)
        {
            moveTime += Time.deltaTime;
        }
        else
        {
            // Player가 멈추면 누적 시간을 초기화합니다.
            moveTime = 0f;
        }

        // 이미 활성화된 오염물이 있으면 재생성을 대기합니다.
        if (Pollutant.activeCount > 0)
        {
            moveTime = 0f;
            pollutantSpawned = true;
            return;
        }

        // 오염원이 중화되어 사라졌으면 처리합니다.
        if (pollutantSpawned)
        {
            pollutantSpawned = false;

            // 마지막 오염원을 중화했다면 복귀하지 않고 그 자리에서 클리어 처리합니다.
            if (stageManager != null && stageManager.IsAllCleared())
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.TriggerClear();
            }
            else
            {
                if (GameManager.Instance == null || !GameManager.Instance.GameEnded)
                    StartCoroutine(ReturnToStartRoutine());
            }
            return;
        }

        // 누적 시간이 다음 생성 시점을 넘어섰으면 새로운 오염원을 준비합니다.
        if (moveTime >= nextSpawnTime && !awaitingSpawn)
        {
            GuideTxt guide = FindAnyObjectByType<GuideTxt>();
            if (guide != null && !guide.introFinished)
                return;

            StartCoroutine(WarningAndSpawn());
        }

        // 오염원이 사라지면 배경을 다시 움직이도록 합니다.
        if (scroll != null && Pollutant.activeCount == 0)
            scroll.ResumeScroll();
    }

    // 오염원 중화 후 플레이어를 시작 지점으로 복귀시키고 다음 생성 루프를 준비합니다.
    private IEnumerator ReturnToStartRoutine()
    {
        if (GameManager.Instance != null && GameManager.Instance.GameEnded)
            yield break;
        if (returningToStart)
            yield break;

        returningToStart = true;

        if (scroll != null)
            scroll.PauseScroll();

        if (player != null)
            yield return StartCoroutine(player.AutoReturnToStart());

        if (scroll != null)
            scroll.ResumeScroll();

        moveTime = 0f;
        nextSpawnTime = Random.Range(timeRange.x, timeRange.y);
        returningToStart = false;
    }

    //오염원 생성 전 경고 메시지를 보여주고, 일정 시간이 지난 후 오염원을 생성하는 코루틴입니다.
    private IEnumerator WarningAndSpawn()
    {
        awaitingSpawn = true;

        if (pollutants == null || pollutants.Length == 0)
        {
            Debug.LogError("PollutantManager: 오염원 프리팹을 등록하세요.");
            awaitingSpawn = false;
            yield break;
        }

        if (spawner == null)
        {
            Debug.LogWarning("PollutantManager: PollutantSpawner가 할당되지 않았습니다.");
            awaitingSpawn = false;
            yield break;
        }

        if (!spawner.isActive)
        {
            Debug.LogWarning("PollutantManager: 할당된 스포너가 비활성화되어 있습니다.");
            awaitingSpawn = false;
            yield break;
        }

        GameObject selectedPrefab = pollutants[Random.Range(0, pollutants.Length)];
        if (selectedPrefab == null)
        {
            Debug.LogError("PollutantManager: 등록된 오염원 프리팹 중 하나가 비어있습니다.");
            awaitingSpawn = false;
            yield break;
        }

        Pollutant prefabPoll = selectedPrefab.GetComponent<Pollutant>();

        // 경고 중에도 플레이어 이동을 막지 않도록 변경했습니다.
        // 대신 텍스트와 타이머 동작만 보여주기 위함입니다.
        if (timer != null)
            timer.StopCountdown();

        // 1단계: 경고 깜빡임
        if (warningTxt != null && prefabPoll != null)
        {
            string warningText = $"[경고]\n{prefabPoll.TypeLabel} 오염물질 발견";
            if (itemSelectManager != null)
                itemSelectManager.OnWarningShown();
            Debug.Log(warningText);
            yield return StartCoroutine(warningTxt.ShowWarningRoutine(warningText));
        }

        // 2단계: GuideTxt에 Z키 안내 문구 표시
        if (guideTxt != null)
        {
            yield return StartCoroutine(guideTxt.ShowItemSelectHintRoutine(
                "Z키로 대응 아이템을 골라주세요", itemSelectHintDuration));
        }
        else
        {
            yield return new WaitForSeconds(itemSelectHintDuration);
        }

        if (Pollutant.activeCount > 0)
        {
            Debug.Log("PollutantManager: 기존 오염물이 남아 있어 새로운 오염원 생성을 취소합니다.");
            if (GameManager.Instance == null || !GameManager.Instance.GameEnded)
            {
                if (player != null)
                    player.canMove = true;
                if (timer != null)
                    timer.isRunning = true;
            }
            awaitingSpawn = false;
            yield break;
        }

        // 2단계: 오염원 생성 (페이드인 연출)
        GameObject created = spawner.Spawn(selectedPrefab);
        if (created != null)
        {
            Pollutant poll = created.GetComponent<Pollutant>();
            if (poll != null)
            {
                poll.appearDuration = spawnFadeDuration;
                poll.disappearDuration = despawnFadeDuration;
                poll.pollutantSlider = pollutantSlider;

                // 3단계: 페이드인이 거의 끝날 때 팝업 표시
                string popupMsg = poll.PopupText;
                Debug.Log(popupMsg);
                if (popupUI != null)
                    StartCoroutine(ShowPopupAfterFadeIn(popupMsg, spawnFadeDuration));
            }

            if (scroll != null)
            {
                scroll.PauseScroll();
                Debug.Log("PollutantManager: Pollutant 생성 후 배경 스크롤 일시정지.");
            }

            if (player != null)
            {
                float targetX = GetEdgeX(created);
                player.GrowRange(targetX, rangeBuffer);
            }

            Debug.Log($"PollutantManager: {selectedPrefab.name}이 생성되었습니다.");
        }

        if (GameManager.Instance == null || !GameManager.Instance.GameEnded)
        {
            if (player != null)
                player.canMove = true;
            if (timer != null)
                timer.isRunning = true;
        }

        moveTime = 0f;
        nextSpawnTime = Random.Range(timeRange.x, timeRange.y);
        awaitingSpawn = false;
    }

    // BlinkWarning 로직은 WarningTxt로 이동

    private IEnumerator ShowPopupAfterFadeIn(string message, float fadeInDuration)
    {
        // 페이드인이 거의 끝날 때 (80% 시점) 팝업 표시
        yield return new WaitForSeconds(fadeInDuration * 0.8f);
        if (popupUI != null)
            popupUI.Show(message, popupShowDuration);
    }

    private float GetEdgeX(GameObject obj)
    {
        if (obj == null || player == null)
            return player != null ? player.transform.position.x : 0f;

        float centerX = obj.transform.position.x;
        float halfWidth = 0.5f;
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null)
            halfWidth = rend.bounds.extents.x;

        float playerX = player.transform.position.x;
        float buffer = 0.1f;

        if (centerX >= playerX)
            return centerX - halfWidth - buffer;
        else
            return centerX + halfWidth + buffer;
    }
}
