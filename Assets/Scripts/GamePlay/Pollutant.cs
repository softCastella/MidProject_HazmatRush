using System.Collections;
using UnityEngine;

public class Pollutant : MonoBehaviour
{
    public enum PollutantType
    {
        TypeA,
        TypeB,
        TypeC
    }

    private static readonly string[] TypeLabels =
    {
        "부식성",
        "유류",
        "혼합화학액"
    };

    private static readonly string[] RecommendedItems =
    {
        "중화제",
        "오일 흡착패드",
        "일반 흡착 패드"
    };

    private static readonly string[][] Substances =
    {
        new[] { "염산", "황산", "질산" },
        new[] { "폐유", "윤활유", "기계유", "연료유" },
        new[] { "폐산 혼합액", "세정 폐액", "공정 폐액", "화학 슬러지 액상" },
    };

    public PollutantType type = PollutantType.TypeA;
    public string TypeLabel => TypeLabels[(int)type];
    public string WarningText => $"{TypeLabel} 오염물질 발견";
    public string RecommendedItem => RecommendedItems[(int)type];
    public ItemType RecommendedItemType => type switch
    {
        PollutantType.TypeA => ItemType.Neutralizer,
        PollutantType.TypeB => ItemType.OilPad,
        PollutantType.TypeC => ItemType.GeneralPad,
        _ => ItemType.Scanner
    };
    public int RecommendedSlotIndex => (int)RecommendedItemType;
    public string PopupText
    {
        get
        {
            var list = Substances[(int)type];
            var substance = list[Random.Range(0, list.Length)];
            return $"{substance}에 {RecommendedItem}을 사용하세요";
        }
    }

    public int hp;
    private float damagePerSecond;
    public float edgeHitRatio = 0.7f; // 이미지 가장자리 영역만 맞히도록 거리 기준

    public static int activeCount = 0;
    //페이드인 아웃 속도
    public float appearDuration = 0.7f;
    public float disappearDuration = 0.7f;

    private float halfWidth = 0.5f;
    private bool isFadingOut = false;
    private SpriteRenderer spriteRenderer;
    private Renderer meshRenderer;
    private Renderer[] childRenderers;

    void Awake()
    {
        SetHpByType();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (spriteRenderer == null)
            meshRenderer = GetComponent<Renderer>();

        if (spriteRenderer == null && meshRenderer == null)
            childRenderers = GetComponentsInChildren<Renderer>(true);

        if (spriteRenderer == null && meshRenderer == null && (childRenderers == null || childRenderers.Length == 0))
            Debug.LogWarning($"{name}: Pollutant에서 렌더러를 찾지 못했습니다. SpriteRenderer 또는 Renderer가 필요합니다.");

        SetAlpha(0f);
    }

    void OnEnable()
    {
        activeCount++;
        Debug.Log($"{name}: OnEnable 호출, appearDuration={appearDuration}, spriteRenderer={(spriteRenderer != null)}");
        StartCoroutine(FadeTo(1f, appearDuration));
    }

    void SetHpByType()
    {
        switch (type)
        {
            case PollutantType.TypeA: // 산성오염원
                hp = 30;
                damagePerSecond = 3;
                break;
            case PollutantType.TypeB: // 오일오염원
                hp = 20;
                damagePerSecond = 2;
                break;
            case PollutantType.TypeC: // 혼합오염원
                hp = 50;
                damagePerSecond = 5;
                break;
        }
    }

    void OnDestroy()
    {
        activeCount = Mathf.Max(0, activeCount - 1);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (!IsEdgeContact(other.transform.position))
            return;

        hp -= Mathf.RoundToInt(damagePerSecond * Time.deltaTime);

        if (hp <= 0f && !isFadingOut)
        {
            StartCoroutine(FadeOutAndDestroy());
        }
    }

    // 플레이어와의 충돌이 가장자리에서만 유효하도록 거리 계산
    bool IsEdgeContact(Vector3 playerPos)
    {
        float dist = Vector2.Distance(transform.position, playerPos);
        return dist >= halfWidth * edgeHitRatio;
    }

    // 투명도 설정 함수
    private void SetAlpha(float alpha)
    {
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
            return;
        }

        if (meshRenderer != null && meshRenderer.material != null)
        {
            Color color = meshRenderer.material.color;
            color.a = alpha;
            meshRenderer.material.color = color;
            return;
        }

        if (childRenderers != null)
        {
            foreach (var rend in childRenderers)
            {
                if (rend == null || rend.material == null)
                    continue;

                Color color = rend.material.color;
                color.a = alpha;
                rend.material.color = color;
            }
        }
    }

    //오염원 등장 페이드인, 사망 페이드 아웃 로직
    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (duration <= 0f)
        {
            SetAlpha(targetAlpha);
            yield break;
        }

        float startAlpha = spriteRenderer != null ? spriteRenderer.color.a : (meshRenderer != null ? meshRenderer.material.color.a : 1f);
        Debug.Log($"{name}: FadeTo 시작, startAlpha={startAlpha}, targetAlpha={targetAlpha}, duration={duration}");
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private IEnumerator FadeOutAndDestroy()
    {
        isFadingOut = true;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        yield return StartCoroutine(FadeTo(0f, disappearDuration));
        Destroy(gameObject);
    }

}
