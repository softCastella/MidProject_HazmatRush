using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Pollutant : MonoBehaviour
{
    //오염원 타입 열거형
    public enum PollutantType
    {
        TypeA, //부식성 오염원
        TypeB, //유류 오염원
        TypeC, //혼합화학액 오염원
    }

    //오염원 타입 (경고 메시지에 사용)
    private static readonly string[] TypeLabels =
    {
        "부식성",
        "유류",
        "혼합화학액"
    };

    //오염원 오염물질 구체적 이름 예(팝업 메시지에 사용)
    private static readonly string[][] Substances =
    {
        new[] { "염산", "황산", "질산" },
        new[] { "폐유", "윤활유", "기계유", "연료유" },
        new[] { "폐산 혼합액", "세정 폐액", "공정 폐액", "화학 슬러지 액상" },
    };
    
    //오염원 추천 아이템(팝업매세지에 사용)
    private static readonly string[] RecommendedItems =
    {
        "중화제",
        "오일 흡착패드",
        "범용 흡착 패드"
    };


    //필드 선언
    //오염원 타입
    public PollutantType type = PollutantType.TypeA;
    //오염원 타입 라벨
    public string TypeLabel => TypeLabels[(int)type];
    //오염원 추천 아이템
    public string RecommendedItem => RecommendedItems[(int)type];
    //오염원 추천 아이템 타입 맵핑
    public Item.ItemType RecommendedItemType => type switch
    {
        PollutantType.TypeA => Item.ItemType.Neutralizer, //부식성 오염원 추천 아이템 타입
        PollutantType.TypeB => Item.ItemType.OilPad, //유류 오염원 추천 아이템 타입
        PollutantType.TypeC => Item.ItemType.GeneralPad, //혼합화학액 오염원 추천 아이템 타입
        _ => Item.ItemType.Scanner, //기타 오염원 추천 아이템 타입
    };
    //오염원 추천 아이템 슬롯 인덱스
    public int RecommendedSlotIndex => (int)RecommendedItemType;
    //오염원 팝업 메시지(형식)
    public string PopupText
    {
        get
        {
            var list = Substances[(int)type];
            var substance = list[Random.Range(0, list.Length)];
            return $"{substance}에 {RecommendedItem}을 사용하세요";
        }
    }

    //오염원 체력 및 데미지 설정
    public float pollutanMaxHp;        //오염원 pollutanMaxHp
    public float pollutanCurHp;    //오염원 pollutanCurHp
    private float pollutanDps;    //오염원 pollutanDps
    public float edgeHitRatio = 0.7f; // 이미지 가장자리 영역만 맞히도록 거리 기준(충돌 판정 거리 비율)
    private float halfWidth = 0.5f;    //오염원 너비 절반

    //페이드 효과 속도
    public float appearDuration = 0.7f;//페이드인 아웃 속도
    public float disappearDuration = 0.7f;//페이드아웃 아웃 속도    
    private bool isFadingOut = false;    //페이드아웃 중인지
    private bool hasLoggedContactJudge = false; //현재 접촉 구간에서 판정 로그 출력 여부
    private bool lastJudgeMatched = false;      //직전 판정 결과
    private bool hasPlayedNeutralizationSfx = false;

    public Slider pollutantSlider;      // PollutantManager가 주입
    private TMP_Text pollutantHpText;
    private Player currentPlayer;      // 접촉 중인 플레이어 캐시

    public static int activeCount = 0;    //활성화된 오염원 개수
    private SpriteRenderer spriteRenderer;    //스프라이트 렌더러
    private Renderer meshRenderer;    //메시 렌더러
    private Renderer[] childRenderers;    //자식 렌더러

    void Awake()
    {
        SetHpByType();
        pollutanCurHp = pollutanMaxHp;
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

    //오염원 체력 및 데미지 설정
    void SetHpByType()
    {
        switch (type)
        {
            case PollutantType.TypeA: // 산성오염원
                pollutanMaxHp = 40;
                pollutanDps = 6;
                break;
            case PollutantType.TypeB: // 오일오염원
                pollutanMaxHp = 20;
                pollutanDps = 4;
                break;
            case PollutantType.TypeC: // 혼합오염원
                pollutanMaxHp = 55;
                pollutanDps = 9;
                break;
        }
    }

    //오염원 삭제 시 활성화된 오염원 개수 감소
    void OnDestroy()
    {
        activeCount = Mathf.Max(0, activeCount - 1);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Player player = other.GetComponent<Player>();
        if (player == null) return;

        currentPlayer = player;

        if (pollutantSlider != null)
        {
            var follower = pollutantSlider.GetComponent<WorldSpaceUIFollower>();
            if (follower != null)
                follower.worldTarget = spriteRenderer != null ? spriteRenderer.transform : transform;
            pollutantSlider.minValue = 0f;
            pollutantSlider.maxValue = 1f;
            pollutantSlider.gameObject.SetActive(true);
            UpdatePollutantHpBar();
        }

        if (player.protectionSlider != null)
        {
            var follower = player.protectionSlider.GetComponent<WorldSpaceUIFollower>();
            if (follower != null)
            {
                SpriteRenderer playerSprite = player.GetComponentInChildren<SpriteRenderer>(true);
                follower.worldTarget = playerSprite != null ? playerSprite.transform : player.transform;
            }
            player.protectionSlider.gameObject.SetActive(true);
            player.UpdateProtectionBar();
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Player player = other.GetComponent<Player>();
        if (player == null)
            return;

        // 1) 접촉 판정 로그를 먼저 출력 (처음 1회 + 결과가 바뀔 때)
        if (player.itemSelectManager != null)
        {
            Item.ItemType selectedType = player.itemSelectManager.SelectedItemType;
            bool isMatched = selectedType == RecommendedItemType;
            if (!hasLoggedContactJudge || isMatched != lastJudgeMatched)
            {
                Debug.Log($"{(isMatched ? "올바른 아이템입니다." : "틀린 아이템입니다.")} 추천 = {RecommendedItemType}, 선택 = {selectedType}");
                hasLoggedContactJudge = true;
                lastJudgeMatched = isMatched;
            }
        }

        // 2) 플레이어 방호복 HP: 접촉 중 계속 초당 감소
        player.ApplyPollutantDamage(pollutanDps);

        // 3) 오염원 현재 HP: 정답 아이템일 때만 초당 감소
        float itemDps = 0f;
        if (player.itemSelectManager != null)
        {
            Item.ItemType selectedType = player.itemSelectManager.SelectedItemType;
            if (selectedType == RecommendedItemType)
            {
                if (!hasPlayedNeutralizationSfx)
                {
                    hasPlayedNeutralizationSfx = true;
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayNeutralizationSfx();
                }

                GameObject selectedItemObject = player.itemSelectManager.GetSelectedItem();
                if (selectedItemObject != null)
                {
                    Item selectedItem = selectedItemObject.GetComponent<Item>();
                    if (selectedItem != null)
                        itemDps = selectedItem.GetDps();
                }
            }
            else
            {
                StopNeutralizationSfxLocal();
            }
        }

        float itemDamage = itemDps * Time.deltaTime;
        if (itemDamage > 0f)
            pollutanCurHp = Mathf.Max(0, pollutanCurHp - itemDamage);

        UpdatePollutantHpBar();

        Debug.Log($"[Pollutant] 오염원 HP 감소: -{itemDamage:F2} (itemDps={itemDps:F2}) / 현재 HP: {pollutanCurHp:F2}");

        if (pollutanCurHp <= 0f && !isFadingOut)
        {
            StopNeutralizationSfxLocal();
            if (player.itemSelectManager != null)
                player.itemSelectManager.ResetToDefault();
            StartCoroutine(FadeOutAndDestroy());
        }
    }

    //오염원과 플레이어 접촉 해제 시 오염원 체력 초기화
    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || isFadingOut)
            return;

        hasLoggedContactJudge = false;
        StopNeutralizationSfxLocal();
        pollutanCurHp = pollutanMaxHp;
        Debug.Log($"[Pollutant] 접촉 해제 -> HP 초기화: {pollutanCurHp:F2}/{pollutanMaxHp:F2}");

        Player player = other.GetComponent<Player>();
        HideBars(player);
        currentPlayer = null;
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

    private void StopNeutralizationSfxLocal()
    {
        if (!hasPlayedNeutralizationSfx)
            return;

        hasPlayedNeutralizationSfx = false;
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopNeutralizationSfx();
    }

    private IEnumerator FadeOutAndDestroy()
    {
        isFadingOut = true;
        StopNeutralizationSfxLocal();
        HideBars(currentPlayer);
        currentPlayer = null;

        StageManager stageManager = FindAnyObjectByType<StageManager>();
        if (stageManager != null)
            stageManager.AddClearedPollutant();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        yield return StartCoroutine(FadeTo(0f, disappearDuration));
        Destroy(gameObject);
    }

    private void HideBars(Player player)
    {
        if (pollutantSlider != null)
            pollutantSlider.gameObject.SetActive(false);

        if (player != null && player.protectionSlider != null)
            player.protectionSlider.gameObject.SetActive(false);
    }

    void UpdatePollutantHpBar()
    {
        if (pollutantSlider == null)
            return;

        pollutantSlider.value = pollutanCurHp / pollutanMaxHp;
        if (pollutantHpText == null)
            pollutantHpText = pollutantSlider.GetComponentInChildren<TMP_Text>(true);
        if (pollutantHpText != null)
            pollutantHpText.text = Mathf.FloorToInt(pollutanCurHp).ToString();
    }

}
