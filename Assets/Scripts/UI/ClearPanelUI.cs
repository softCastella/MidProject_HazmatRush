using System.Collections;
using TMPro;
using UnityEngine;

public class ClearPanelUI : MonoBehaviour
{
    [Header("Stars (왼쪽부터 starCount개만 채움)")]
    public Animator[] starAnimators;

    [Tooltip("Star.controller 안 State 이름")]
    public string starFillStateName = "StarFilled";

    [Tooltip("한 별 애니가 끝난 뒤 다음 별까지 추가 대기(초). 0이면 바로 이어서")]
    public float starGapAfterFinish = 0f;

    [Tooltip("애니 길이를 못 읽을 때 사용할 대기 시간(초)")]
    public float starFillFallbackSeconds = 0.75f;

    [Tooltip("별 Image 가로 반전 (채우기 방향이 거꾸로일 때 켜기)")]
    public bool flipStarScaleX = true;

    [Header("Text")]
    public TMP_Text breakdownText;

    private static readonly string[] StarNames = { "Star_0", "Star_1", "Star_2" };

    private StageClearResult pendingResult;
    private bool hasPendingResult;

    void Awake()
    {
        TryFillStarAnimators();
        ApplyStarFlipX();
        ResetAllStarsEmpty();
    }

    void OnEnable()
    {
        if (!hasPendingResult)
            return;

        StopAllCoroutines();
        StartCoroutine(ApplyRoutine(pendingResult));
    }

    public void Apply(StageClearResult result)
    {
        pendingResult = result;
        hasPendingResult = true;

        if (breakdownText != null && result.breakdownLines != null)
            breakdownText.text = string.Join("\n", result.breakdownLines);

        if (!gameObject.activeInHierarchy)
            return;

        StopAllCoroutines();
        StartCoroutine(ApplyRoutine(result));
    }

    private void TryFillStarAnimators()
    {
        if (HasStarAnimators())
            return;

        Transform starRoot = transform.Find("StarFilled");
        if (starRoot == null)
            return;

        starAnimators = new Animator[StarNames.Length];
        for (int i = 0; i < StarNames.Length; i++)
        {
            Transform star = starRoot.Find(StarNames[i]);
            if (star != null)
                starAnimators[i] = star.GetComponent<Animator>();
        }
    }

    private bool HasStarAnimators()
    {
        if (starAnimators == null || starAnimators.Length < StarNames.Length)
            return false;

        for (int i = 0; i < StarNames.Length; i++)
        {
            if (starAnimators[i] == null)
                return false;
        }

        return true;
    }

    private void ApplyStarFlipX()
    {
        if (!flipStarScaleX || starAnimators == null)
            return;

        for (int i = 0; i < starAnimators.Length; i++)
        {
            if (starAnimators[i] == null)
                continue;

            Transform star = starAnimators[i].transform;
            Vector3 scale = star.localScale;
            scale.x = -Mathf.Abs(scale.x);
            star.localScale = scale;
        }
    }

    private IEnumerator ApplyRoutine(StageClearResult result)
    {
        yield return null;

        ResetAllStarsEmpty();

        int fillCount = Mathf.Clamp(result.starCount, 0, starAnimators.Length);
        for (int i = 0; i < fillCount; i++)
        {
            if (starAnimators[i] == null)
                continue;

            yield return PlayStarAndWait(starAnimators[i]);

            if (starGapAfterFinish > 0f)
                yield return new WaitForSeconds(starGapAfterFinish);
        }
    }

    private IEnumerator PlayStarAndWait(Animator anim)
    {
        SetupStar(anim, true);
        yield return null;

        float duration = starFillFallbackSeconds;
        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
        if (info.IsName(starFillStateName) && info.length > 0f)
            duration = info.length;

        yield return new WaitForSeconds(duration);
    }

    private void ResetAllStarsEmpty()
    {
        if (starAnimators == null)
            return;

        for (int i = 0; i < starAnimators.Length; i++)
        {
            if (starAnimators[i] != null)
                SetupStar(starAnimators[i], false);
        }
    }

    private void SetupStar(Animator anim, bool earned)
    {
        if (anim == null)
            return;

        anim.enabled = true;
        anim.speed = earned ? 1f : 0f;
        anim.Play(starFillStateName, 0, 0f);
        anim.Update(0f);

        if (!earned)
            anim.speed = 0f;
    }
}
