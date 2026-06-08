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
        TypeD, //가스 오염원
    }

    //오염원 타입 (경고 메시지에 사용)
    private static readonly string[] TypeLabels =
    {
        "부식성",
        "유류",
        "혼합화학액",
        "가스"
    };

    //오염원 오염물질 구체적 이름 예(팝업 메시지에 사용)
    private static readonly string[][] Substances =
    {
        new[] { "염산", "황산", "질산" },
        new[] { "폐유", "윤활유", "기계유", "연료유" },
        new[] { "폐산 혼합액", "세정 폐액", "공정 폐액", "화학 슬러지 액상" },
        new[] { "독성가스", "염소가스", "암모니아가스", "일산화탄소" },
    };
    
    //오염원 추천 아이템(팝업매세지에 사용)
    private static readonly string[] RecommendedItems =
    {
        "중화제",
        "오일패드", 
        "범용패드",
        "가스밸브"
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
        PollutantType.TypeD => Item.ItemType.GasValve, //가스 오염원 추천 아이템 타입
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
    public int spriteSortingOrder = 0; // 플레이어(5)보다 뒤에 그림 — A/B/C 동일
    private bool isFadingOut = false;    //페이드아웃 중인지
    private bool clearedActiveCount = false; // FadeOut 시작 시 activeCount 선차감 여부
    private bool hasLoggedContactJudge = false; //현재 접촉 구간에서 판정 로그 출력 여부
    private bool lastJudgeMatched = false;      //직전 판정 결과
    private bool hasPlayedNeutralizationSfx = false;

    public Slider pollutantSlider;      // PollutantManager가 주입
    private TMP_Text pollutantHpText;
    private Player currentPlayer;      // 접촉 중인 플레이어 캐시
    private bool playerInTrigger;      // 트리거 안에 플레이어가 있는지 (bounds 보조 판정용)
    private bool appearInProgress;       // 등장 페이드 중 — 접촉·데미지 금지
    private MaterialPropertyBlock particleAlphaBlock;
    private Coroutine _contactFlashRoutine;

    // 오염원 타입별 접촉 플래시 색상 (A=초록, B=검정, C=보라, D=없음)
    private static readonly Color[] TypeFlashColors =
    {
        new Color(0f, 1f, 0f, 0f),      // A: 초록 (RGB 틴트)
        new Color(1f, 1f, 1f, 0f),      // B: 흰색 → 알파 깜빡임으로 처리
        new Color(0.6f, 0f, 0.8f, 0f),  // C: 보라 (RGB 틴트)
        Color.clear,                      // D: 가스 — 파티클이라 생략
    };
    private const float ContactFlashInterval = 0.25f; // 반짝 주기(초)

    public static int activeCount = 0;    // 활성화된 오염원 개수 (A~D 전체)
    public static int activeAbcCount = 0; // A/B/C만 (맵 진행·동시 1개 제한용)
    public static bool suppressEnableForPreload = false;

    public static bool HasActiveAbc()
    {
        return activeAbcCount > 0;
    }

    public static bool HasAnyActive()
    {
        return activeCount > 0;
    }

    public bool IsPlayerContactActive()
    {
        return playerInTrigger && currentPlayer != null && !isFadingOut;
    }
    private SpriteRenderer spriteRenderer;    //스프라이트 렌더러
    private Renderer meshRenderer;    //메시 렌더러
    private Renderer[] childRenderers;    //자식 렌더러
    private ParticleSystem[] particleSystems; // TypeD: 3D 스모크 파티클
    private bool useParticleVisual;
    private float particleVisualStrength = 1f;

    void Awake()
    {
        SetHpByType();
        pollutanCurHp = pollutanMaxHp;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (type == PollutantType.TypeD)
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            if (particleSystems != null && particleSystems.Length > 0)
            {
                useParticleVisual = true;
                // SetActive(true) 순간 파티클 시뮬이 먼저 돌며 멈춤 방지 — 등장 시에만 켬
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    if (particleSystems[i] != null)
                        particleSystems[i].gameObject.SetActive(false);
                }
            }

            if (spriteRenderer != null && spriteRenderer.sprite == null)
                spriteRenderer.enabled = false;
        }

        if (!useParticleVisual)
        {
            if (spriteRenderer == null)
                meshRenderer = GetComponent<Renderer>();

            if (spriteRenderer == null && meshRenderer == null)
                childRenderers = GetComponentsInChildren<Renderer>(true);

            if (spriteRenderer == null && meshRenderer == null && (childRenderers == null || childRenderers.Length == 0))
                Debug.LogWarning($"{name}: Pollutant에서 렌더러를 찾지 못했습니다. SpriteRenderer 또는 Renderer가 필요합니다.");
        }

        ApplySpriteSortingOrder();
        SetVisualStrength(0f);
    }

    void ApplySpriteSortingOrder()
    {
        if (useParticleVisual || type == PollutantType.TypeD)
            return;

        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = spriteSortingOrder;

        if (childRenderers == null)
            return;

        for (int i = 0; i < childRenderers.Length; i++)
        {
            SpriteRenderer sr = childRenderers[i] as SpriteRenderer;
            if (sr != null)
                sr.sortingOrder = spriteSortingOrder;
        }
    }

    void OnEnable()
    {
        if (suppressEnableForPreload)
        {
            SetVisualStrength(0f);
            return;
        }

        activeCount++;
        if (type != PollutantType.TypeD)
            activeAbcCount++;
        StartCoroutine(AppearRoutine());
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
            case PollutantType.TypeD: // 가스 오염원
                pollutanMaxHp = 24;
                pollutanDps = 5;
                break;
        }
    }

    //오염원 삭제 시 활성화된 오염원 개수 감소
    void OnDestroy()
    {
        if (clearedActiveCount)
            return;

        activeCount = Mathf.Max(0, activeCount - 1);
        if (type != PollutantType.TypeD)
            activeAbcCount = Mathf.Max(0, activeAbcCount - 1);
    }

    bool CanProcessPlayerContact(Player player)
    {
        if (player == null || player.IsDead)
            return false;
        if (GameManager.Instance != null && (GameManager.Instance.GameEnded || GameManager.Instance.IsPenalty))
            return false;
        return true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (appearInProgress)
            return;
        if (!other.CompareTag("Player")) return;

        Player player = other.GetComponent<Player>();
        if (!CanProcessPlayerContact(player))
            return;

        playerInTrigger = true;
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

        player.AddPollutantTouch();
        // 플래시는 정답 아이템 접촉 시(HP 감소 시)에만 — ApplyPlayerContactDamage에서 시작

        if (type == PollutantType.TypeD)
            Debug.Log($"[Pollutant] 가스(D) 콜라이더 접촉: {name}");
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (appearInProgress)
            return;
        if (!other.CompareTag("Player"))
            return;

        Player player = other.GetComponent<Player>();
        if (player == null || !CanProcessPlayerContact(player))
        {
            // 사망·GameEnded 시에만 HP바 숨김 (패널티는 일시정지일 뿐이므로 제외)
            bool isDeadOrEnded = player == null || player.IsDead ||
                (GameManager.Instance != null && GameManager.Instance.GameEnded);
            if (isDeadOrEnded && !isFadingOut)
                HideBars(player);
            return;
        }

        playerInTrigger = true;
        currentPlayer = player;
        ApplyPlayerContactDamage(player);
    }

    private bool IsBoundsOverlappingPlayer(Player player, float padding = 0f)
    {
        if (player == null)
            return false;

        Collider2D pollutantCol = GetComponent<Collider2D>();
        Collider2D playerCol = player.GetComponent<Collider2D>();
        if (pollutantCol == null || playerCol == null || !pollutantCol.enabled)
            return false;

        Physics2D.SyncTransforms();
        Bounds zone = pollutantCol.bounds;
        if (padding > 0f)
            zone.Expand(padding);

        return zone.Intersects(playerCol.bounds);
    }

    private void EndPlayerContact(Player player)
    {
        playerInTrigger = false;

        if (player != null)
        {
            if (type == PollutantType.TypeD)
                player.SetValveAnimActive(false);
            player.RemovePollutantTouch();
        }

        StopContactFlash();

        if (isFadingOut)
            return;

        hasLoggedContactJudge = false;
        StopNeutralizationSfxLocal();

        pollutanCurHp = pollutanMaxHp;
        // Debug.Log($"[Pollutant] 접촉 해제 -> HP 초기화: {pollutanCurHp:F2}/{pollutanMaxHp:F2}");

        StageScoreTracker scoreTracker = FindAnyObjectByType<StageScoreTracker>();
        if (scoreTracker != null)
            scoreTracker.RegisterPollutantReset();

        HideBars(player);
        currentPlayer = null;
    }

    private void ApplyPlayerContactDamage(Player player)
    {
        if (!CanProcessPlayerContact(player))
        {
            StopNeutralizationSfxLocal();
            if (type == PollutantType.TypeD)
                player.SetValveAnimActive(false);
            player.RefreshNeutralizationVfx();
            return;
        }

        if (type == PollutantType.TypeD && player.itemSelectManager != null)
        {
            Item.ItemType selectedType = player.itemSelectManager.SelectedItemType;
            bool valveOn = selectedType == RecommendedItemType && selectedType == Item.ItemType.GasValve;
            player.SetValveAnimActive(valveOn);
        }
        else
        {
            player.RefreshNeutralizationVfx();
        }

        // 1) 접촉 판정 로그를 먼저 출력 (처음 1회 + 결과가 바뀔 때)
        if (player.itemSelectManager != null)
        {
            Item.ItemType selectedType = player.itemSelectManager.SelectedItemType;
            bool isMatched = selectedType == RecommendedItemType;
            if (!hasLoggedContactJudge || isMatched != lastJudgeMatched)
            {
                if (type == PollutantType.TypeD && selectedType == Item.ItemType.GasValve && isMatched)
                    Debug.Log("올바른 도구입니다.");
                else
                    Debug.Log($"{(isMatched ? "올바른 아이템입니다." : "틀린 아이템입니다.")} 추천 = {RecommendedItemType}, 선택 = {selectedType}");
                hasLoggedContactJudge = true;
                lastJudgeMatched = isMatched;

                // 틀린 아이템으로 접촉하면 오대응 패널티(스테이지 1-1 전용)를 발동합니다.
                if (!isMatched)
                {
                    StageScoreTracker scoreTracker = FindAnyObjectByType<StageScoreTracker>();
                    if (scoreTracker != null)
                        scoreTracker.RegisterWrongItem();

                    if (GameManager.Instance != null)
                        GameManager.Instance.TriggerWrongItemPenalty();
                }
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
                if (type != PollutantType.TypeD && !hasPlayedNeutralizationSfx)
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

                if (itemDps <= 0f && type == PollutantType.TypeD && selectedType == Item.ItemType.GasValve)
                    itemDps = 12f;
            }
            else
            {
                StopNeutralizationSfxLocal();
            }
        }

        float itemDamage = itemDps * Time.deltaTime;
        if (itemDamage > 0f)
        {
            pollutanCurHp = Mathf.Max(0, pollutanCurHp - itemDamage);
            StartContactFlash(); // HP 감소 중일 때만 번쩍
        }
        else
        {
            StopContactFlash(); // 틀린 아이템 or 데미지 없으면 번쩍 끔
        }

        UpdatePollutantHpBar();

        if (pollutanCurHp <= 0f && !isFadingOut)
        {
            StopContactFlash();
            StopNeutralizationSfxLocal();
            if (type == PollutantType.TypeD)
                player.SetValveAnimActive(false);
            player.RemovePollutantTouch();
            StartCoroutine(FadeOutAndDestroy());
        }
    }

    //오염원과 플레이어 접촉 해제 시 오염원 체력 초기화 (A~C, D 동일)
    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Player player = other.GetComponent<Player>();
        if (player != null && IsBoundsOverlappingPlayer(player))
            return;

        playerInTrigger = false;
        EndPlayerContact(player);
    }

    // 플레이어와의 충돌이 가장자리에서만 유효하도록 거리 계산
    bool IsEdgeContact(Vector3 playerPos)
    {
        float dist = Vector2.Distance(transform.position, playerPos);
        return dist >= halfWidth * edgeHitRatio;
    }

    private float GetVisualStrength()
    {
        if (useParticleVisual)
            return particleVisualStrength;

        if (spriteRenderer != null)
            return spriteRenderer.color.a;

        if (meshRenderer != null && meshRenderer.material != null)
            return meshRenderer.material.color.a;

        return 1f;
    }

    // 스프라이트 알파 또는 TypeD 파티클 강도(0~1)
    private void SetVisualStrength(float strength)
    {
        if (useParticleVisual)
        {
            particleVisualStrength = strength;
            SetParticleVisualStrength(strength);
            return;
        }

        SetAlpha(strength);
    }

    private void SetParticleVisualStrength(float strength)
    {
        if (particleSystems == null)
            return;

        strength = Mathf.Clamp01(strength);

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem ps = particleSystems[i];
            if (ps == null)
                continue;

            if (strength <= 0.01f)
            {
                if (ps.isPlaying)
                {
                    var emissionOff = ps.emission;
                    emissionOff.enabled = false;
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                SetParticleRendererAlpha(ps, 0f);
                continue;
            }

            if (!isFadingOut)
            {
                var emissionOn = ps.emission;
                if (!emissionOn.enabled)
                    emissionOn.enabled = true;
                if (!ps.isPlaying)
                    ps.Play();
            }

            SetParticleRendererAlpha(ps, strength);
        }

        if (spriteRenderer != null && spriteRenderer.enabled)
            SetAlpha(strength);
    }

    private void SetParticleRendererAlpha(ParticleSystem ps, float alpha)
    {
        if (ps == null)
            return;

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer == null)
            return;

        if (particleAlphaBlock == null)
            particleAlphaBlock = new MaterialPropertyBlock();

        renderer.GetPropertyBlock(particleAlphaBlock);
        Color color = particleAlphaBlock.GetColor("_Color");
        if (color.maxColorComponent <= 0f && alpha > 0f)
            color = Color.white;
        color.a = alpha;
        particleAlphaBlock.SetColor("_Color", color);
        particleAlphaBlock.SetColor("_BaseColor", color);
        renderer.SetPropertyBlock(particleAlphaBlock);
    }

    private void StopParticleEmission()
    {
        if (!useParticleVisual || particleSystems == null)
            return;

        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] == null)
                continue;

            var emission = particleSystems[i].emission;
            emission.enabled = false;
        }
    }

    // 투명도 설정 함수 (A/B/C 스프라이트용)
    private void StopContactFlash()
    {
        if (_contactFlashRoutine != null)
        {
            StopCoroutine(_contactFlashRoutine);
            _contactFlashRoutine = null;
        }
        ResetSpriteColor();
    }

    private void StartContactFlash()
    {
        if (type == PollutantType.TypeD) return;  // 가스는 파티클 비주얼이라 생략
        if (_contactFlashRoutine != null) return; // 이미 실행 중
        _contactFlashRoutine = StartCoroutine(ContactFlashLoop());
    }

    private IEnumerator ContactFlashLoop()
    {
        Color flash = TypeFlashColors[(int)type];
        while (true)
        {
            if (type == PollutantType.TypeB)
            {
                // B: 알파 깜빡임 (반투명 ↔ 정상)
                SetSpriteAlphaOnly(0.25f);
                yield return new WaitForSeconds(ContactFlashInterval * 0.5f);
                SetSpriteAlphaOnly(1f);
            }
            else
            {
                SetSpriteRGB(flash.r, flash.g, flash.b);
                yield return new WaitForSeconds(ContactFlashInterval * 0.5f);
                SetSpriteRGB(1f, 1f, 1f);
            }
            yield return new WaitForSeconds(ContactFlashInterval * 0.5f);
        }
    }

    private void ApplySpriteColorTint(Color tint)
    {
        SetSpriteRGB(tint.r, tint.g, tint.b);
    }

    private void SetSpriteAlphaOnly(float a)
    {
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = a;
            spriteRenderer.color = c;
            return;
        }
        if (childRenderers != null)
            for (int i = 0; i < childRenderers.Length; i++)
                if (childRenderers[i] != null && childRenderers[i].material != null)
                {
                    Color c = childRenderers[i].material.color;
                    c.a = a;
                    childRenderers[i].material.color = c;
                }
    }

    private void SetSpriteRGB(float r, float g, float b)
    {
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.r = r; c.g = g; c.b = b;
            spriteRenderer.color = c;
            return;
        }
        if (childRenderers != null)
            for (int i = 0; i < childRenderers.Length; i++)
                if (childRenderers[i] != null && childRenderers[i].material != null)
                {
                    Color c = childRenderers[i].material.color;
                    c.r = r; c.g = g; c.b = b;
                    childRenderers[i].material.color = c;
                }
    }

    private void ResetSpriteColor()
    {
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.r = 1f; c.g = 1f; c.b = 1f; c.a = 1f;
            spriteRenderer.color = c;
            return;
        }
        if (childRenderers != null)
            for (int i = 0; i < childRenderers.Length; i++)
                if (childRenderers[i] != null && childRenderers[i].material != null)
                {
                    Color c = childRenderers[i].material.color;
                    c.r = 1f; c.g = 1f; c.b = 1f; c.a = 1f;
                    childRenderers[i].material.color = c;
                }
    }

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

    [Header("TypeD 랜덤 크기 (가스)")]
    public float[] typeDScaleOptions = { 1.5f, 2f, 3f };

    private IEnumerator AppearRoutine()
    {
        appearInProgress = true;
        playerInTrigger = false;
        currentPlayer = null;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        if (type == PollutantType.TypeD)
        {
            ApplyTypeDRandomScale();
            yield return null;
            ShowTypeDVisual();
            if (col != null)
                col.enabled = true;
            appearInProgress = false;
            yield break;
        }

        yield return FadeTo(1f, appearDuration);

        if (col != null)
            col.enabled = true;

        appearInProgress = false;
    }

    private void ApplyTypeDRandomScale()
    {
        if (typeDScaleOptions == null || typeDScaleOptions.Length == 0)
            return;

        float scale = typeDScaleOptions[Random.Range(0, typeDScaleOptions.Length)];
        transform.localScale = new Vector3(scale, scale, scale);
    }

    private void ShowTypeDVisual()
    {
        particleVisualStrength = 1f;
        if (!useParticleVisual || particleSystems == null)
            return;

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem ps = particleSystems[i];
            if (ps == null)
                continue;

            ps.gameObject.SetActive(true);
            var emission = ps.emission;
            emission.enabled = true;
            if (!ps.isPlaying)
                ps.Play();
            SetParticleRendererAlpha(ps, 1f);
        }
    }

    //오염원 등장 페이드인, 사망 페이드 아웃 로직
    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (duration <= 0f)
        {
            SetVisualStrength(targetAlpha);
            yield break;
        }

        float startAlpha = GetVisualStrength();
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            SetVisualStrength(alpha);
            yield return null;
        }

        SetVisualStrength(targetAlpha);
    }

    private void StopNeutralizationSfxLocal()
    {
        if (type == PollutantType.TypeD)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.StopValveSfx();
            return;
        }

        if (!hasPlayedNeutralizationSfx)
            return;

        hasPlayedNeutralizationSfx = false;
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopNeutralizationSfx();
    }

    private IEnumerator FadeOutAndDestroy()
    {
        isFadingOut = true;

        // 페이드 중에도 다음 오염원 등장 대기가 풀리도록 카운트를 먼저 내림
        if (!clearedActiveCount)
        {
            activeCount = Mathf.Max(0, activeCount - 1);
            if (type != PollutantType.TypeD)
                activeAbcCount = Mathf.Max(0, activeAbcCount - 1);
            clearedActiveCount = true;
        }

        StopNeutralizationSfxLocal();
        HideBars(currentPlayer);
        currentPlayer = null;

        StageManager stageManager = FindAnyObjectByType<StageManager>();
        if (stageManager != null)
            stageManager.AddClearedPollutant();

        RecoveryItemManager recoveryManager = FindAnyObjectByType<RecoveryItemManager>();
        if (recoveryManager != null)
            recoveryManager.TryDropOnPollutantCleared(type, transform.position);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        PollutantManager pollutantManager = FindAnyObjectByType<PollutantManager>();
        if (pollutantManager != null)
            pollutantManager.RefreshMoveRangeForRemainingPollutants(this);

        if (useParticleVisual)
            StopParticleEmission();

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
