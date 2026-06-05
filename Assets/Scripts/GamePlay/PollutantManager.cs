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
    private bool abcClearedPending = false;
    private bool deferredMapAdvance = false;
    private bool returningToStart = false;
    private bool mapTransitioning = false;

    // 활성 오염원 없을 때만 이동 시간 누적 → 다음 큐 항목 등장
    private float moveTime = 0f;
    private float nextSpawnTime;
    private bool queueSpawnReady = false;
    private bool segmentMovementUnlocked = false;

    // 로딩 시 미리 만든 오염원 (등장 순서대로, 등장 시 SetActive만)
    private GameObject[] preloadedQueue;
    private int revealIndex;
    private int preparedStageIndex = -1;

    public void StopReturnFlow()
    {
        StopAllCoroutines();
        if (player != null)
            player.StopAllCoroutines();
        returningToStart = false;
        mapTransitioning = false;
        awaitingSpawn = false;
        abcClearedPending = false;
        deferredMapAdvance = false;
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
        queueSpawnReady = false;
        segmentMovementUnlocked = false;
        PollutantSpawnPlan.Clear();
        preparedStageIndex = -1;

        DestroyPreloadedPollutants();
        Pollutant[] activePollutants = FindObjectsByType<Pollutant>(FindObjectsSortMode.None);
        for (int i = 0; i < activePollutants.Length; i++)
        {
            if (activePollutants[i] != null)
                Destroy(activePollutants[i].gameObject);
        }

        revealIndex = 0;
        BuildPreloadedPollutants();

        if (warningTxt != null)
            warningTxt.HideWarning();
        if (guideTxt != null)
            guideTxt.HideGuide();
        if (scroll != null)
            scroll.ResumeScroll();
        if (itemSelectManager != null)
            itemSelectManager.ResetToDefault();

        RecoveryItemManager recoveryManager = FindAnyObjectByType<RecoveryItemManager>();
        if (recoveryManager != null)
            recoveryManager.ResetForStage();
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
        EnsureMapFadeOverlay();
    }

    void Start()
    {
        BuildPreloadedPollutants();
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

        if (player.isMoving)
            moveTime += Time.deltaTime;
        else if (!queueSpawnReady)
            moveTime = 0f;

        // A~C 중화 후, 이 구간 예정 오염원(중화+가스) 전부 처리 전까지 맵 전환 불가
        if (abcClearedPending && !Pollutant.HasActiveAbc())
        {
            abcClearedPending = false;
            if (!CanLeaveCurrentSegment())
                deferredMapAdvance = true;
            else
                StartSegmentAfterAbcClear();
        }

        if (deferredMapAdvance && CanLeaveCurrentSegment() && !returningToStart && !mapTransitioning)
        {
            deferredMapAdvance = false;
            StartSegmentAfterAbcClear();
        }

        // A~C·D 공통: 활성 오염원 없을 때만 다음 큐 항목 등장 (로딩 확정 순서)
        if (Pollutant.HasAnyActive())
        {
            moveTime = 0f;
            abcClearedPending = true;
        }
        else if ((moveTime >= nextSpawnTime || queueSpawnReady) && !awaitingSpawn && HasMorePreloaded() && StageHasQueuedPollutant())
        {
            GuideTxt guide = FindAnyObjectByType<GuideTxt>();
            if (guide != null && !guide.introFinished)
                return;

            queueSpawnReady = false;
            StartCoroutine(WarningAndSpawn());
        }

        // 배경: 활성 오염원·경고 코루틴 중에만 멈춤 (이동 잠금·프리로드 대기와 무관)
        if (scroll != null)
        {
            if (Pollutant.HasAnyActive() || awaitingSpawn)
                scroll.PauseScroll();
            else
                scroll.ResumeScroll();
        }
    }

    // 활성 오염원 + 아직 등장·처리 안 한 예정(중화 A~C, 가스 D)이 있으면 이 화면에서 나갈 수 없음
    private bool CanLeaveCurrentSegment()
    {
        if (Pollutant.HasAnyActive())
            return false;

        if (!PollutantSpawnPlan.HasPlan())
            return true;

        if (HasMorePreloaded())
            return false;

        if (HasAnyPreloadedRemaining())
            return false;

        return true;
    }

    private bool HasAnyPreloadedRemaining()
    {
        return HasObjectInPreloadedArray(preloadedQueue);
    }

    private bool HasObjectInPreloadedArray(GameObject[] list)
    {
        if (list == null)
            return false;

        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] != null)
                return true;
        }

        return false;
    }

    private void StartSegmentAfterAbcClear()
    {
        if (GameManager.Instance != null && GameManager.Instance.GameEnded)
            return;

        bool allCleared = stageManager != null && stageManager.IsAllCleared();
        if (allCleared)
            StartCoroutine(TriggerClearAfterDelay());
        else if (PollutantSpawnPlan.HasMoreMaps())
            StartCoroutine(MapAdvanceRoutine());
    }

    private IEnumerator TriggerClearAfterDelay()
    {
        if (clearPanelDelay > 0f)
            yield return new WaitForSeconds(clearPanelDelay);

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

        // 다음 맵이 있을 때만 호출됨 — 마지막 맵(스테이지 클리어)은 TriggerClearAfterDelay 경로
        returningToStart = true;
        mapTransitioning = true;

        if (scroll != null)
            scroll.ResumeScroll();

        if (player != null)
        {
            player.mapAdvanceRightX = mapEndX;
            player.PrepareMapAdvanceWalk();
        }

        if (guideTxt != null)
            guideTxt.ShowGuideImmediate("오른쪽으로 이동하세요");

        float reachX = mapEndX - Mathf.Max(0.1f, mapEndReachDistance);
        while (player != null)
        {
            if (GameManager.Instance != null && GameManager.Instance.GameEnded)
            {
                if (guideTxt != null)
                    guideTxt.HideGuide();
                mapTransitioning = false;
                returningToStart = false;
                yield break;
            }

            float posX = player.transform.position.x;
            if (posX >= reachX)
                break;

            yield return null;
        }

        if (guideTxt != null)
            guideTxt.HideGuide();

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
            if (!Pollutant.HasAnyActive())
                scroll.ResumeScroll();
            else
                scroll.PauseScroll();
        }

        yield return FadeMapOverlay(0f, mapFadeDuration);

        PollutantSpawnPlan.AdvanceMap();
        segmentMovementUnlocked = false;
        revealIndex = 0;
        BuildPreloadedPollutants();

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

        bool hasPlan = PollutantSpawnPlan.HasPlan();
        PollutantSpawnPlan.Entry planEntry = GetPendingPlanEntry();
        bool isGas = hasPlan && planEntry.useTypeDPoints;
        bool spawnTypeD = hasPlan && planEntry.type == Pollutant.PollutantType.TypeD;

        GameObject selectedPrefab = null;
        if (hasPlan)
            selectedPrefab = FindPrefabForType(planEntry.type);

        if (selectedPrefab == null)
        {
            GameObject[] pool = spawnTypeD ? GetTypeDPool() : GetAbcPool();
            if (pool == null || pool.Length == 0)
            {
                Debug.LogError($"PollutantManager: {(spawnTypeD ? "D(가스)" : "A~C")} 프리팹을 찾지 못해 생성을 중단합니다.");
                awaitingSpawn = false;
                yield break;
            }

            selectedPrefab = pool[Random.Range(0, pool.Length)];
            if (selectedPrefab == null)
            {
                Debug.LogError("PollutantManager: 등록된 오염원 프리팹 중 하나가 비어있습니다.");
                awaitingSpawn = false;
                yield break;
            }
        }

        Pollutant prefabPoll = selectedPrefab.GetComponent<Pollutant>();

        if (spawner == null)
        {
            Debug.LogWarning("PollutantManager: PollutantSpawner가 할당되지 않았습니다.");
            awaitingSpawn = false;
            yield break;
        }

        if (!spawner.isActive)
        {
            Debug.LogWarning("PollutantManager: PollutantSpawner가 비활성화되어 있습니다.");
            awaitingSpawn = false;
            yield break;
        }

        if (timer != null)
            timer.StopCountdown();

        if (prefabPoll != null && itemSelectManager != null)
            itemSelectManager.OnWarningShown();

        if (warningTxt != null && prefabPoll != null)
        {
            string warningText = $"[경고] {prefabPoll.TypeLabel} 오염물질 발견";
            Debug.Log(warningText);
            yield return StartCoroutine(warningTxt.ShowWarningRoutine(warningText));
        }

        if (guideTxt != null && GuideTxt.IsTutorialStage())
        {
            yield return StartCoroutine(guideTxt.ShowItemSelectHintRoutine(
                "Z키(왼쪽)/X키(오른쪽)로 대응 아이템을 골라주세요", itemSelectHintDuration));
        }
        else
        {
            yield return new WaitForSeconds(itemSelectHintDuration);
        }

        if (Pollutant.HasAnyActive())
        {
            Debug.Log("PollutantManager: 활성 오염원이 남아 있어 새 오염원 생성을 취소합니다.");
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

        GameObject created = TryRevealPreloaded();
        if (created == null)
            created = spawner.Spawn(selectedPrefab, isGas);

        if (created != null)
        {
            Pollutant poll = created.GetComponent<Pollutant>();
            if (poll != null)
            {
                poll.appearDuration = spawnFadeDuration;
                poll.disappearDuration = despawnFadeDuration;
                poll.pollutantSlider = pollutantSlider;

                string popupMsg = poll.PopupText;
                Debug.Log(popupMsg);
                if (popupUI != null)
                    StartCoroutine(ShowPopupAfterFadeIn(popupMsg, spawnFadeDuration));
            }

            if (scroll != null)
            {
                scroll.PauseScroll();
                Debug.Log($"PollutantManager: 오염원 등장 후 배경 스크롤 일시정지 ({(spawnTypeD ? "D" : "A~C")}).");
            }

            if (player != null && !segmentMovementUnlocked)
            {
                player.UnlockMapSegmentMovement();
                segmentMovementUnlocked = true;
            }

            Debug.Log($"PollutantManager: {created.name} 등장 ({(spawnTypeD ? "D" : "A~C")})");

            if (hasPlan)
                revealIndex++;
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
        abcClearedPending = true;
    }

    // BlinkWarning 로직은 WarningTxt로 이동

    private IEnumerator ShowPopupAfterFadeIn(string message, float fadeInDuration)
    {
        // 페이드인이 거의 끝날 때 (80% 시점) 팝업 표시
        yield return new WaitForSeconds(fadeInDuration * 0.8f);
        if (popupUI != null)
            popupUI.Show(message, popupShowDuration);
    }

    private GameObject[] GetAbcPool()
    {
        return BuildSpawnPool(false);
    }

    private GameObject[] GetTypeDPool()
    {
        return BuildSpawnPool(true);
    }

    private bool StageHasTypeD()
    {
        if (stageManager == null)
            return false;

        string types = stageManager.GetCurrentPollutantTypes();
        if (string.IsNullOrEmpty(types))
            return false;

        string[] parts = types.Split('|');
        for (int i = 0; i < parts.Length; i++)
        {
            string token = parts[i].Trim();
            if (token.Length > 0 && char.ToUpperInvariant(token[0]) == 'D')
                return true;
        }

        return false;
    }

    private bool StageHasQueuedPollutant()
    {
        if (PollutantSpawnPlan.HasPlan())
            return HasMorePreloaded();

        return HasAbcInStage() || StageHasTypeD();
    }

    private bool HasAbcInStage()
    {
        if (stageManager == null)
            return true;

        string types = stageManager.GetCurrentPollutantTypes();
        if (string.IsNullOrEmpty(types))
            return true;

        string[] parts = types.Split('|');
        for (int i = 0; i < parts.Length; i++)
        {
            string token = parts[i].Trim();
            if (token.Length == 0)
                continue;

            char c = char.ToUpperInvariant(token[0]);
            if (c == 'A' || c == 'B' || c == 'C')
                return true;
        }

        return false;
    }

    private GameObject[] BuildSpawnPool(bool onlyTypeD)
    {
        if (pollutants == null || pollutants.Length == 0)
            return pollutants;

        if (stageManager == null)
            return FilterPoolByKind(pollutants, onlyTypeD);

        string types = stageManager.GetCurrentPollutantTypes();
        if (string.IsNullOrEmpty(types))
            return FilterPoolByKind(pollutants, onlyTypeD);

        string[] parts = types.Split('|');
        GameObject[] temp = new GameObject[pollutants.Length];
        int count = 0;

        for (int p = 0; p < parts.Length; p++)
        {
            string token = parts[p].Trim();
            if (token.Length == 0)
                continue;

            Pollutant.PollutantType wantType = CharToPollutantType(token[0]);
            if (onlyTypeD)
            {
                if (wantType != Pollutant.PollutantType.TypeD)
                    continue;
            }
            else if (wantType == Pollutant.PollutantType.TypeD)
            {
                continue;
            }

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
            return FilterPoolByKind(pollutants, onlyTypeD);

        GameObject[] result = new GameObject[count];
        for (int i = 0; i < count; i++)
            result[i] = temp[i];

        return result;
    }

    private GameObject[] FilterPoolByKind(GameObject[] source, bool onlyTypeD)
    {
        if (source == null || source.Length == 0)
            return source;

        GameObject[] temp = new GameObject[source.Length];
        int count = 0;

        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] == null)
                continue;

            Pollutant pol = source[i].GetComponent<Pollutant>();
            if (pol == null)
                continue;

            if (onlyTypeD)
            {
                if (pol.type != Pollutant.PollutantType.TypeD)
                    continue;
            }
            else if (pol.type == Pollutant.PollutantType.TypeD)
            {
                continue;
            }

            temp[count++] = source[i];
        }

        if (count == 0)
            return source;

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
            case 'D': return Pollutant.PollutantType.TypeD;
            default: return Pollutant.PollutantType.TypeA;
        }
    }

    private void EnsureSpawnPlan()
    {
        if (stageManager == null)
            stageManager = FindAnyObjectByType<StageManager>();
        if (stageManager == null || stageManager.stageDataJson == null)
            return;

        int stageIndex = stageManager.currentStageIndex;
        if (SceneLoadManager.Instance != null && SceneLoadManager.Instance.pendingStageIndex >= 0)
            stageIndex = SceneLoadManager.Instance.pendingStageIndex;

        if (PollutantSpawnPlan.HasPlan() && preparedStageIndex == stageIndex)
            return;

        int abcCount = 3;
        int dCount = 4;
        if (spawner != null)
        {
            abcCount = Mathf.Max(1, spawner.normalSpawnPointCount);
            if (spawner.typeDSpawnIndices != null && spawner.typeDSpawnIndices.Length > 0)
                dCount = spawner.typeDSpawnIndices.Length;
        }

        PollutantSpawnPlan.Prepare(stageManager.stageDataJson, stageIndex, abcCount, dCount);
        preparedStageIndex = stageIndex;
    }

    private void BuildPreloadedPollutants()
    {
        DestroyPreloadedPollutants();
        EnsureSpawnPlan();

        if (!PollutantSpawnPlan.HasPlan() || spawner == null)
            return;

        Pollutant.suppressEnableForPreload = true;

        int mapCount = PollutantSpawnPlan.GetCurrentMapCount();
        int segmentStart = PollutantSpawnPlan.GetSegmentStart();
        if (PollutantSpawnPlan.spawnQueue != null && mapCount > 0)
        {
            preloadedQueue = new GameObject[mapCount];
            for (int i = 0; i < mapCount; i++)
            {
                int queueIndex = segmentStart + i;
                if (queueIndex >= PollutantSpawnPlan.spawnQueue.Length)
                    break;

                PollutantSpawnPlan.Entry entry = PollutantSpawnPlan.spawnQueue[queueIndex];
                GameObject prefab = FindPrefabForType(entry.type);
                if (prefab == null)
                {
                    Debug.LogWarning($"[PollutantManager] 프리로드 실패 - 타입 {entry.type} (PollutantManager.pollutants에 프리팹 등록 확인)");
                    continue;
                }

                GameObject obj = spawner.SpawnAt(prefab, entry.useTypeDPoints, entry.spawnPointIndex);
                SetupPreloadedObject(obj);
                preloadedQueue[i] = obj;
            }
        }

        Pollutant.suppressEnableForPreload = false;

        revealIndex = 0;
        Debug.Log($"[PollutantManager] 맵 {PollutantSpawnPlan.currentMapIndex + 1} 오염원 준비 - 이 구간 {mapCount}개");
    }

    private void SetupPreloadedObject(GameObject obj)
    {
        if (obj == null)
            return;

        Pollutant poll = obj.GetComponent<Pollutant>();
        if (poll != null)
        {
            poll.appearDuration = spawnFadeDuration;
            poll.disappearDuration = despawnFadeDuration;
            poll.pollutantSlider = pollutantSlider;
        }

        Collider2D col = obj.GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        obj.SetActive(false);
    }

    private void DestroyPreloadedPollutants()
    {
        DestroyPreloadedArray(preloadedQueue);
        preloadedQueue = null;
    }

    private void DestroyPreloadedArray(GameObject[] list)
    {
        if (list == null)
            return;

        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] != null)
                Destroy(list[i]);
        }
    }

    private bool HasMorePreloaded()
    {
        if (!PollutantSpawnPlan.HasPlan())
            return true;

        return PollutantSpawnPlan.HasMoreInCurrentMap(revealIndex);
    }

    private void NotifyQueueAdvanceReady()
    {
        if (Pollutant.HasAnyActive())
            return;
        if (!HasMorePreloaded())
            return;

        moveTime = nextSpawnTime;
        queueSpawnReady = true;
    }

    private GameObject TryRevealPreloaded()
    {
        if (!PollutantSpawnPlan.HasPlan())
            return null;

        if (!PollutantSpawnPlan.HasMoreInCurrentMap(revealIndex))
            return null;

        if (preloadedQueue == null || revealIndex >= preloadedQueue.Length)
            return null;

        GameObject obj = preloadedQueue[revealIndex];
        if (obj == null)
            return null;

        Pollutant poll = obj.GetComponent<Pollutant>();
        bool isTypeD = poll != null && poll.type == Pollutant.PollutantType.TypeD;

        // D(가스): 페이드 끝날 때까지 콜라이더는 Pollutant.AppearRoutine에서 켬
        if (!isTypeD)
        {
            Collider2D col = obj.GetComponent<Collider2D>();
            if (col != null)
                col.enabled = true;
        }

        obj.SetActive(true);
        return obj;
    }

    private PollutantSpawnPlan.Entry GetPendingPlanEntry()
    {
        PollutantSpawnPlan.Entry empty = new PollutantSpawnPlan.Entry();
        if (!PollutantSpawnPlan.HasMoreInCurrentMap(revealIndex))
            return empty;

        int queueIndex = PollutantSpawnPlan.GetSegmentStart() + revealIndex;
        if (PollutantSpawnPlan.spawnQueue == null || queueIndex >= PollutantSpawnPlan.spawnQueue.Length)
            return empty;

        return PollutantSpawnPlan.spawnQueue[queueIndex];
    }

    private GameObject FindPrefabForType(Pollutant.PollutantType wantType)
    {
        if (pollutants == null)
            return null;

        for (int i = 0; i < pollutants.Length; i++)
        {
            if (pollutants[i] == null)
                continue;

            Pollutant pol = pollutants[i].GetComponent<Pollutant>();
            if (pol != null && pol.type == wantType)
                return pollutants[i];
        }

        return null;
    }

    public void RefreshMoveRangeForRemainingPollutants(Pollutant exclude)
    {
        if (player == null)
            return;

        if (segmentMovementUnlocked)
            player.EnsureRangeIncludesPosition(rangeBuffer);

        NotifyQueueAdvanceReady();
    }
}
