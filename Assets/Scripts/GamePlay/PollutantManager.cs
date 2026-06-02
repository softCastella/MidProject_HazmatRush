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

    [Header("스테이지 클리어")]
    public float clearPanelDelay = 1f; // 마지막 오염원 페이드아웃 후 클리어 패널까지 대기(초)

    [Header("맵 구간 전환 (오염원 중화 후)")]
    public float mapEndX = 769f;
    public float mapEndReachDistance = 2f;
    public float mapFadeDuration = 0.6f;
    public CanvasGroup mapFadeOverlay;

    private bool awaitingSpawn = false;
    private bool pollutantSpawned = false;
    private bool returningToStart = false;
    private bool mapTransitioning = false;

    // 맵 전환·스폰 준비 중에는 ESC 일시정지를 막습니다 (timeScale=0이면 코루틴이 멈춤).
    public bool BlocksPause => returningToStart || mapTransitioning || awaitingSpawn;

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
        mapTransitioning = false;
        awaitingSpawn = false;
        pollutantSpawned = false;
        if (mapFadeOverlay != null)
        {
            mapFadeOverlay.alpha = 0f;
            mapFadeOverlay.blocksRaycasts = false;
        }
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

        Pollutant.ResetActiveCount();

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
        Pollutant.ResetActiveCount();

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
        EnsureMapFadeOverlay();
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

            bool allCleared = stageManager != null && stageManager.IsAllCleared();
            if (GameManager.Instance != null && !GameManager.Instance.GameEnded)
            {
                if (allCleared)
                    StartCoroutine(TriggerClearAfterDelay());
                else
                    StartCoroutine(MapAdvanceRoutine());
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

    private IEnumerator TriggerClearAfterDelay()
    {
        if (clearPanelDelay > 0f)
            yield return new WaitForSecondsRealtime(clearPanelDelay);

        if (GameManager.Instance != null && !GameManager.Instance.GameEnded)
            GameManager.Instance.TriggerClear();
    }

    // 오염원 중화 후 x=mapEndX까지 이동 → 페이드아웃 → 시작 위치·배경 리셋 → 페이드인 (마지막 오염원 제외)
    private IEnumerator MapAdvanceRoutine()
    {
        if (GameManager.Instance != null && GameManager.Instance.GameEnded)
            yield break;
        if (returningToStart || mapTransitioning)
            yield break;

        returningToStart = true;
        mapTransitioning = true;

        if (scroll != null)
            scroll.ResumeScroll();

        if (player != null)
        {
            player.mapAdvanceRightX = mapEndX;
            player.PrepareMapAdvanceWalk();
        }

        float reachX = mapEndX - Mathf.Max(0.1f, mapEndReachDistance);
        while (player != null)
        {
            if (GameManager.Instance != null && GameManager.Instance.GameEnded)
            {
                mapTransitioning = false;
                returningToStart = false;
                yield break;
            }

            float posX = player.transform.position.x;
            if (posX >= reachX)
                break;

            yield return null;
        }

        yield return FadeMapOverlay(1f, mapFadeDuration);

        if (player != null)
        {
            player.ResetRange();
            player.SnapToStartPosition();
            player.canMove = true;
        }

        if (scroll != null)
        {
            scroll.ResetScrollOffset();
            scroll.ResumeScroll();
        }

        yield return FadeMapOverlay(0f, mapFadeDuration);

        moveTime = 0f;
        nextSpawnTime = Random.Range(timeRange.x, timeRange.y);
        mapTransitioning = false;
        returningToStart = false;
    }

    private void EnsureMapFadeOverlay()
    {
        if (mapFadeOverlay != null)
            return;

        Canvas hudCanvas = null;
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
            {
                hudCanvas = canvases[i];
                break;
            }
        }

        if (hudCanvas == null && canvases.Length > 0)
            hudCanvas = canvases[0];

        if (hudCanvas == null)
            return;

        GameObject fadeObj = new GameObject("MapTransitionFade");
        fadeObj.transform.SetParent(hudCanvas.transform, false);

        RectTransform rt = fadeObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = fadeObj.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = true;

        mapFadeOverlay = fadeObj.AddComponent<CanvasGroup>();
        mapFadeOverlay.alpha = 0f;
        mapFadeOverlay.interactable = false;
        mapFadeOverlay.blocksRaycasts = false;
        fadeObj.transform.SetAsLastSibling();
    }

    private IEnumerator FadeMapOverlay(float targetAlpha, float duration)
    {
        EnsureMapFadeOverlay();
        if (mapFadeOverlay == null)
            yield break;

        if (duration <= 0f)
        {
            mapFadeOverlay.alpha = targetAlpha;
            mapFadeOverlay.blocksRaycasts = targetAlpha > 0.01f;
            yield break;
        }

        float startAlpha = mapFadeOverlay.alpha;
        mapFadeOverlay.blocksRaycasts = true;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            mapFadeOverlay.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }

        mapFadeOverlay.alpha = targetAlpha;
        mapFadeOverlay.blocksRaycasts = targetAlpha > 0.01f;
    }

    //오염원 생성 전 경고 메시지를 보여주고, 일정 시간이 지난 후 오염원을 생성하는 코루틴입니다.
    private IEnumerator WarningAndSpawn()
    {
        awaitingSpawn = true;

        GameObject[] pool = GetActivePollutantPool();
        if (pool == null || pool.Length == 0)
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

        GameObject selectedPrefab = pool[Random.Range(0, pool.Length)];
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
                created.SetActive(true);

                // 3단계: 페이드인이 거의 끝날 때 팝업 표시
                string popupMsg = poll.PopupText;
                Debug.Log(popupMsg);
                if (popupUI != null)
                    StartCoroutine(ShowPopupAfterFadeIn(popupMsg, spawnFadeDuration));
            }
            else
            {
                created.SetActive(true);
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
        yield return new WaitForSecondsRealtime(fadeInDuration * 0.8f);
        if (popupUI != null)
            popupUI.Show(message, popupShowDuration);
    }

    private GameObject[] GetActivePollutantPool()
    {
        if (pollutants == null || pollutants.Length == 0)
            return pollutants;

        if (stageManager == null)
            return pollutants;

        string types = stageManager.GetCurrentPollutantTypes();
        if (string.IsNullOrEmpty(types))
            return pollutants;

        string[] parts = types.Split('|');
        GameObject[] temp = new GameObject[pollutants.Length];
        int count = 0;

        for (int p = 0; p < parts.Length; p++)
        {
            string token = parts[p].Trim();
            if (token.Length == 0)
                continue;

            Pollutant.PollutantType wantType = CharToPollutantType(token[0]);
            for (int i = 0; i < pollutants.Length; i++)
            {
                if (pollutants[i] == null)
                    continue;

                Pollutant pol = pollutants[i].GetComponent<Pollutant>();
                if (pol == null || pol.type != wantType)
                    continue;

                bool alreadyAdded = false;
                for (int j = 0; j < count; j++)
                {
                    if (temp[j] == pollutants[i])
                    {
                        alreadyAdded = true;
                        break;
                    }
                }

                if (!alreadyAdded)
                    temp[count++] = pollutants[i];
            }
        }

        if (count == 0)
            return pollutants;

        GameObject[] result = new GameObject[count];
        for (int i = 0; i < count; i++)
            result[i] = temp[i];

        return result;
    }

    private Pollutant.PollutantType CharToPollutantType(char c)
    {
        switch (char.ToUpperInvariant(c))
        {
            case 'A': return Pollutant.PollutantType.TypeA;
            case 'B': return Pollutant.PollutantType.TypeB;
            case 'C': return Pollutant.PollutantType.TypeC;
            default: return Pollutant.PollutantType.TypeA;
        }
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
