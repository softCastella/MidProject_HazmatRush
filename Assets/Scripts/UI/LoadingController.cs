using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingController : MonoBehaviour
{
    [Header("씬 전체 페이드 (Canvas 루트 CanvasGroup)")]
    [Tooltip("Canvas에 붙인 CanvasGroup. Bg·Misc18·문구가 함께 페이드됩니다. Misc18 bl_LoadingEffect 페이드와 별개.")]
    public CanvasGroup sceneFade;
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;

    [Header("Misc18 / Loading Effect")]
    public bl_LoadingEffect loadingEffect;
    [Tooltip("bl_LoadingEffect Loading UI Speed (1~100). 회전만 조절, 씬 페이드 아님")]
    public float effectRotateSpeed = 12f;

    [Header("Load")]
    public Slider progressBar;
    public float minShowSeconds = 1f;
    public GameObject[] warmupPrefabs;
    public string fallbackSceneName = "GameScene";

    private float loadStartTime;

    void Awake()
    {
        Camera cam = Camera.main;
        if (cam != null)
            cam.backgroundColor = Color.black;

        if (sceneFade == null)
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                sceneFade = canvas.GetComponent<CanvasGroup>();
                if (sceneFade == null)
                    sceneFade = canvas.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (loadingEffect == null)
            loadingEffect = FindAnyObjectByType<bl_LoadingEffect>();

        if (loadingEffect != null)
        {
            loadingEffect.isLoading = true;
            loadingEffect.FadeSpeed = 99f;
            loadingEffect.SetRotateSpeed(effectRotateSpeed);
        }
    }

    void Start()
    {
        loadStartTime = Time.realtimeSinceStartup;
        StartCoroutine(LoadRoutine());
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
