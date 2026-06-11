using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PollutantArrowUI : MonoBehaviour
{
    [Tooltip("오염원 스프라이트 위쪽 여백(월드 Y). HP바 WorldSpaceUIFollower와 동일 단위.")]
    public float worldOffsetY = 120f;

    [Header("등장 연출 (코드)")]
    [Tooltip("위에서 시작할 때 추가 Y(픽셀).")]
    public float dropStartOffsetY = 90f;
    public float dropDownDuration = 0.35f;
    [Tooltip("내려찍은 뒤 다시 올라갈 Y(픽셀).")]
    public float riseOffsetY = 35f;
    public float riseDuration = 0.28f;
    [Tooltip("내려찍기+올라가기 1세트 반복 횟수.")]
    public int bounceCount = 3;

    private WorldSpaceUIFollower follower;
    private RectTransform visualRect;
    private Coroutine showRoutine;

    void Awake()
    {
        follower = GetComponent<WorldSpaceUIFollower>();
        if (follower == null)
            follower = gameObject.AddComponent<WorldSpaceUIFollower>();

        follower.placeAboveSprite = true;
        follower.worldOffset = new Vector3(0f, worldOffsetY, 0f);

        EnsureArrowVisual();
        gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (!gameObject.activeSelf || follower == null)
            return;

        follower.worldOffset = new Vector3(0f, worldOffsetY, 0f);
    }

    public void ShowAt(Transform pollutantRoot)
    {
        if (pollutantRoot == null || follower == null)
            return;

        StopShowRoutine();
        EnsureArrowVisual();

        follower.worldTarget = ResolveFollowTarget(pollutantRoot);
        follower.worldOffset = new Vector3(0f, worldOffsetY, 0f);

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        showRoutine = StartCoroutine(ShowRoutine());
    }

    public void Hide()
    {
        StopShowRoutine();

        if (follower != null)
            follower.worldTarget = null;

        if (visualRect != null)
            visualRect.anchoredPosition = Vector2.zero;

        gameObject.SetActive(false);
    }

    private IEnumerator ShowRoutine()
    {
        if (visualRect != null)
            yield return PlayDropEffect();

        showRoutine = null;
        Hide();
    }

    private IEnumerator PlayDropEffect()
    {
        int count = Mathf.Max(1, bounceCount);
        for (int i = 0; i < count; i++)
        {
            float startY = i == 0 ? dropStartOffsetY : riseOffsetY;
            yield return LerpVisualY(startY, 0f, dropDownDuration);
            yield return LerpVisualY(0f, riseOffsetY, riseDuration);
        }

        visualRect.anchoredPosition = new Vector2(0f, riseOffsetY);
    }

    private IEnumerator LerpVisualY(float fromY, float toY, float duration)
    {
        float time = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float y = Mathf.Lerp(fromY, toY, elapsed / time);
            visualRect.anchoredPosition = new Vector2(0f, y);
            yield return null;
        }

        visualRect.anchoredPosition = new Vector2(0f, toY);
    }

    private void StopShowRoutine()
    {
        if (showRoutine == null)
            return;

        StopCoroutine(showRoutine);
        showRoutine = null;
    }

    private void EnsureArrowVisual()
    {
        if (visualRect != null)
            return;

        Transform visual = transform.Find("ArrowVisual");
        if (visual != null)
        {
            SetupVisual(visual.gameObject);
            return;
        }

        Image rootImage = GetComponent<Image>();
        if (rootImage == null)
            return;

        GameObject childGo = new GameObject("ArrowVisual", typeof(RectTransform));
        childGo.layer = gameObject.layer;
        childGo.transform.SetParent(transform, false);

        RectTransform childRect = childGo.GetComponent<RectTransform>();
        childRect.anchorMin = Vector2.zero;
        childRect.anchorMax = Vector2.one;
        childRect.offsetMin = Vector2.zero;
        childRect.offsetMax = Vector2.zero;
        childRect.pivot = new Vector2(0.5f, 0.5f);
        childRect.localScale = Vector3.one;
        childRect.localRotation = Quaternion.identity;

        Image childImage = childGo.AddComponent<Image>();
        childImage.sprite = rootImage.sprite;
        childImage.color = rootImage.color;
        childImage.raycastTarget = false;
        childImage.maskable = rootImage.maskable;
        childGo.AddComponent<CanvasRenderer>();

        Animator rootAnim = GetComponent<Animator>();
        if (rootAnim != null)
            Destroy(rootAnim);

        Destroy(rootImage);
        CanvasRenderer rootRenderer = GetComponent<CanvasRenderer>();
        if (rootRenderer != null)
            Destroy(rootRenderer);

        SetupVisual(childGo);
    }

    private void SetupVisual(GameObject visualGo)
    {
        Animator anim = visualGo.GetComponent<Animator>();
        if (anim != null)
            Destroy(anim);

        visualRect = visualGo.GetComponent<RectTransform>();
        Image img = visualGo.GetComponent<Image>();
        if (img != null)
        {
            img.enabled = true;
            Color c = img.color;
            c.a = 1f;
            img.color = c;
        }
    }

    private static Transform ResolveFollowTarget(Transform pollutantRoot)
    {
        if (pollutantRoot == null)
            return null;

        SpriteRenderer sr = pollutantRoot.GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null)
            return sr.transform;

        return pollutantRoot;
    }
}
