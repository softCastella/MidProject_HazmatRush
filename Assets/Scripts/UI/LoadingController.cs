using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingController : MonoBehaviour
{
    [Header("씬 전체 페이드 (활성 Canvas 루트 CanvasGroup)")]
    [Tooltip("비우면 활성 Canvas에 CanvasGroup을 찾거나 추가합니다.")]
    public CanvasGroup sceneFade;
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;

    [Header("SpinnerRoot (Animator)")]
    [Tooltip("스피너 연출은 Unity Animator(SpinnerRoot)에서만 설정합니다. 코드는 재생 상태를 바꾸지 않습니다.")]
    public GameObject spinnerRoot;
    public Animator spinnerAnimator;
    public float spinnerAnimSpeed = 1f;

    [Header("Load")]
    public Slider progressBar;
    public float minShowSeconds = 1f;
    public GameObject[] warmupPrefabs;
    public string fallbackSceneName = "GameScene";

    [Header("오염원 배치 (로딩 시 확정)")]
    public TextAsset stageDataJson;
    public int pollutantAbcSpawnPointCount = 3;
    public int pollutantDSpawnPointCount = 4;

    private float loadStartTime;

    void Awake()
    {
        Camera cam = Camera.main;
        if (cam != null)
            cam.backgroundColor = Color.black;

        EnsureSceneFade();
        EnsureSpinner();
        DisableLegacyMisc18Spinner();
    }

    void Start()
    {
        loadStartTime = Time.realtimeSinceStartup;
        StartCoroutine(LoadRoutine());
    }

    void EnsureSceneFade()
    {
        if (sceneFade != null)
            return;

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (!canvases[i].gameObject.activeInHierarchy)
                continue;

            sceneFade = canvases[i].GetComponent<CanvasGroup>();
            if (sceneFade == null)
                sceneFade = canvases[i].gameObject.AddComponent<CanvasGroup>();
            return;
        }
    }

    void EnsureSpinner()
    {
        if (spinnerRoot == null)
        {
            GameObject found = GameObject.Find("SpinnerRoot");
            if (found != null)
                spinnerRoot = found;
        }

        if (spinnerRoot == null)
            return;

        spinnerRoot.SetActive(true);

        if (spinnerAnimator == null)
            spinnerAnimator = spinnerRoot.GetComponent<Animator>();

        if (spinnerAnimator != null)
            spinnerAnimator.speed = spinnerAnimSpeed;
    }

    void DisableLegacyMisc18Spinner()
    {
        GameObject misc18 = GameObject.Find("Misc18");
        if (misc18 != null)
            misc18.SetActive(false);
    }

    private IEnumerator LoadRoutine()
    {
        if (sceneFade != null)
            sceneFade.alpha = 0f;

        if (fadeInDuration > 0f)
            yield return FadeSceneTo(1f, fadeInDuration);
        else if (sceneFade != null)
            sceneFade.alpha = 1f;

        WarmupPrefabs();

        int stageIndex = 0;
        if (SceneLoadManager.Instance != null && SceneLoadManager.Instance.pendingStageIndex >= 0)
            stageIndex = SceneLoadManager.Instance.pendingStageIndex;
        PollutantSpawnPlan.Prepare(stageDataJson, stageIndex, pollutantAbcSpawnPointCount, pollutantDSpawnPointCount);

        string targetScene = fallbackSceneName;
        if (SceneLoadManager.Instance != null && !string.IsNullOrEmpty(SceneLoadManager.Instance.nextSceneName))
            targetScene = SceneLoadManager.Instance.nextSceneName;

        AsyncOperation op = SceneManager.LoadSceneAsync(targetScene);
        if (op == null)
        {
            Debug.LogError($"[LoadingController] 씬 로드 실패: {targetScene}");
            yield break;
        }

        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            if (progressBar != null)
                progressBar.value = op.progress / 0.9f;
            yield return null;
        }

        if (progressBar != null)
            progressBar.value = 1f;

        float elapsed = Time.realtimeSinceStartup - loadStartTime;
        if (elapsed < minShowSeconds)
            yield return new WaitForSeconds(minShowSeconds - elapsed);

        if (fadeOutDuration > 0f)
            yield return FadeSceneTo(0f, fadeOutDuration);
        else if (sceneFade != null)
            sceneFade.alpha = 0f;

        op.allowSceneActivation = true;
    }

    private IEnumerator FadeSceneTo(float targetAlpha, float duration)
    {
        if (sceneFade == null)
            yield break;

        float startAlpha = sceneFade.alpha;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            sceneFade.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }

        sceneFade.alpha = targetAlpha;
    }

    private void WarmupPrefabs()
    {
        if (warmupPrefabs == null)
            return;

        for (int i = 0; i < warmupPrefabs.Length; i++)
        {
            if (warmupPrefabs[i] == null)
                continue;

            GameObject obj = Instantiate(warmupPrefabs[i]);
            Destroy(obj);
        }
    }
}
