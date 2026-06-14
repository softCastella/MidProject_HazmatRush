using System.Collections;
using TMPro;
using UnityEngine;

public class Player : MonoBehaviour
{
    private enum PlayerState
    {
        Idle = 0,
        Move = 1,
        Die = 2
    }

    public float moveSpeed = 400f; // 플레이어 이동 속도
    [Tooltip("배경 스크롤 잠금 중(PollutantManager) moveSpeed 배율. 1이면 변화 없음.")]
    public float bgLockedSpeedMultiplier = 1.5f;
    public float returnMoveSpeed = 900f; // 시작 지점 복귀 속도 (씬에서 10000 등 과하게 올리지 않기)
    public float returnStopDistance = 0.2f; // 이 거리 안이면 복귀 완료
    public float leftLimit = -785f; // 왼쪽 이동 제한
    public float rightLimit = -403f; // 오른쪽 이동 제한
    public float mapAdvanceRightX = 769f; // 오염원 중화 후 맵 전환 구간 끝 X
    public float dieAnimWait = 0.55f; // Player_Die 클립 길이에 맞춤
    public float dieFadeDuration = 0.5f;
    public float maxProtection = 100f; // 방호복 최대 수치
    public float curProtection; // 현재 방호복 수치

    public ProtectionHpBarUI protectionHpBar;

    public bool isMoving; // 이동 중인지
    public bool hasInput; // 입력이 들어왔는지
    public bool canMove = false; // 이동 가능 여부
    public ItemSelectManager itemSelectManager;

    [Header("중화모드 VFX (자식 CFXR 등)")]
    public GameObject neutralizationVfx;

    private Animator anim; // 애니메이터 컴포넌트
    private Rigidbody2D rb;
    private float startLeft; // 기본 왼쪽 이동 범위 저장
    private float startRight; // 기본 오른쪽 이동 범위 저장
    private Vector3 startPosition; // 게임 시작 시 플레이어 위치
    public float StartGroundY => startPosition.y;
    private PlayerState currentState = PlayerState.Idle;
    private bool isReturning = false;
    private bool dieAnimPlayed = false;
    private bool isDeathSequenceRunning = false;
    private Coroutine _damageFlashRoutine;

    [Header("피격 플래시")]
    public Color damageFlashColor = new Color(1f, 0f, 0f, 0.75f); // 붉은 반투명
    public float damageFlashDuration = 0.08f;  // 빨간색 켜짐 시간
    public float damageFlashCooldown = 0.25f;  // 다음 플래시까지 대기 시간 (간격)
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer[] childSpriteRenderers;
    private ParticleSystem[] neutralizationParticles;
    private int pollutantTouchCount;
    private bool valveAnimActive = false;
    private bool clearAnimActive = false;
    private float moveInput;

    public bool IsDead => currentState == PlayerState.Die;
    public bool IsValveAnimActive => valveAnimActive;

    void Awake()
    {
        startLeft = leftLimit;
        startRight = rightLimit;
        startPosition = transform.position;
        curProtection = maxProtection;
        anim = GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>(true);

        if (anim == null)
            Debug.LogWarning("Player: Animator를 찾지 못했습니다. 이동 애니메이션(State)이 재생되지 않습니다.");

        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            childSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        InitNeutralizationVfx();
    }

    void OnEnable()
    {
        InitNeutralizationVfx();
    }

    void Start()
    {
        pollutantTouchCount = 0;
        InitNeutralizationVfx();

        if (itemSelectManager == null)
            itemSelectManager = FindAnyObjectByType<ItemSelectManager>();

        GuideTxt guide = FindAnyObjectByType<GuideTxt>();
        if (guide == null || string.IsNullOrEmpty(guide.defaultMessage))
            canMove = true;

        if (protectionHpBar == null)
        {
            GameObject hpObj = GameObject.Find("protectionHp");
            if (hpObj != null)
                protectionHpBar = hpObj.GetComponent<ProtectionHpBarUI>();
        }

        if (protectionHpBar != null)
            protectionHpBar.ResolveRefs();

        UpdateProtectionBar();
    }

    public void UpdateProtectionBar()
    {
        if (protectionHpBar == null)
            return;

        protectionHpBar.SetProtection(curProtection, maxProtection);
    }

    void Update()
    {
        if (!canMove || (GameManager.Instance != null && (GameManager.Instance.IsPaused || GameManager.Instance.GameEnded || GameManager.Instance.IsPenalty)))
        {
            moveInput = 0f;
            if (!isReturning)
            {
                isMoving = false;
                hasInput = false;
                if (currentState != PlayerState.Die)
                    SetState(PlayerState.Idle);
            }
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");

        if (h == 0f)
        {
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
                h = -1f;
            else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
                h = 1f;
        }

        moveInput = h;

        if (h > 0f)
            transform.localScale = new Vector3(1f, 1f, 1f);
        else if (h < 0f)
            transform.localScale = new Vector3(-1f, 1f, 1f);

        isMoving = h != 0f;
        hasInput = isMoving;
        SetState(isMoving ? PlayerState.Move : PlayerState.Idle);
    }

    void FixedUpdate()
    {
        if (!canMove || isReturning)
            return;
        if (GameManager.Instance != null && (GameManager.Instance.IsPaused || GameManager.Instance.GameEnded || GameManager.Instance.IsPenalty))
            return;
        if (moveInput == 0f)
            return;

        float step = moveInput * GetMoveSpeed() * Time.fixedDeltaTime;
        float newX = transform.position.x + step;
        newX = Mathf.Clamp(newX, Mathf.Min(leftLimit, rightLimit), Mathf.Max(leftLimit, rightLimit));

        if (rb != null)
            rb.MovePosition(new Vector2(newX, transform.position.y));
        else
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);
    }

    private float GetMoveSpeed()
    {
        if (bgLockedSpeedMultiplier <= 1f)
            return moveSpeed;

        PollutantManager mgr = null;
        if (GameManager.Instance != null)
            mgr = GameManager.Instance.pollutantManager;
        if (mgr == null)
            mgr = FindAnyObjectByType<PollutantManager>();
        if (mgr != null && mgr.IsBackgroundScrollLocked())
            return moveSpeed * bgLockedSpeedMultiplier;

        return moveSpeed;
    }

    public void GrowRange(float targetX, float buffer = 0.5f)
    {
        if (targetX < leftLimit)
            leftLimit = targetX - buffer;
        if (targetX > rightLimit)
            rightLimit = targetX + buffer;
    }

    public void ResetRange()
    {
        leftLimit = startLeft;
        rightLimit = startRight;
    }

    // 이 맵에서 첫 오염원 등장 후 — 1차(-403) 해제, 맵 안 좌우 자유
    public void UnlockMapSegmentMovement()
    {
        leftLimit = startLeft;
        rightLimit = mapAdvanceRightX;
    }

    // 범위 재설정 후에도 현재 위치에서 강제로 밀리지 않게 한계를 맞춤
    public void EnsureRangeIncludesPosition(float buffer = 0.5f)
    {
        float posX = transform.position.x;
        GrowRange(posX, buffer);
    }

    // 오염원 중화 후 맵 끝까지 우측 이동만 허용합니다.
    public void PrepareMapAdvanceWalk()
    {
        ResetRange();
        rightLimit = mapAdvanceRightX;
        canMove = true;
        isReturning = false;
    }

    public void StopMovement()
    {
        isReturning = false;
        canMove = false;
        isMoving = false;
        hasInput = false;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        if (currentState != PlayerState.Die)
            SetState(PlayerState.Idle);
    }

    // 오염원 중화 후 시작 지점으로 자동 복귀시키고 1차 이동 범위로 되돌립니다.
    public IEnumerator AutoReturnToStart()
    {
        if (currentState == PlayerState.Die || isReturning)
            yield break;

        canMove = false;
        isReturning = true;
        isMoving = false;
        hasInput = false;
        ResetRange();

        transform.localScale = new Vector3(1f, 1f, 1f);
        SetState(PlayerState.Idle);

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        float stopDist = Mathf.Max(0.05f, returnStopDistance);

        while (true)
        {
            if (GameManager.Instance != null && GameManager.Instance.GameEnded)
            {
                isReturning = false;
                isMoving = false;
                if (rb != null)
                    rb.linearVelocity = Vector2.zero;
                yield break;
            }

            float posX = rb != null ? rb.position.x : transform.position.x;
            float dist = Mathf.Abs(posX - startPosition.x);
            if (dist <= stopDist)
                break;

            float step = returnMoveSpeed * Time.fixedDeltaTime;
            float newX;
            if (step >= dist)
                newX = startPosition.x;
            else
                newX = Mathf.MoveTowards(posX, startPosition.x, step);

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.MovePosition(new Vector2(newX, rb.position.y));
                yield return new WaitForFixedUpdate();
            }
            else
            {
                transform.position = new Vector3(newX, transform.position.y, transform.position.z);
                yield return null;
            }
        }

        SnapToStartPosition();
        isReturning = false;
        isMoving = false;
        SetState(PlayerState.Idle);

        if (GameManager.Instance == null || !GameManager.Instance.GameEnded)
            canMove = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Pollutant"))
            return;

        Pollutant pollutant = other.GetComponent<Pollutant>();
        if (pollutant != null && itemSelectManager == null)
            itemSelectManager = FindAnyObjectByType<ItemSelectManager>();
    }

    public void ApplyPollutantDamage(float pollutantDps)
    {
        if (currentState == PlayerState.Die)
            return;
        if (GameManager.Instance != null && GameManager.Instance.GameEnded)
            return;

        float damage = pollutantDps * Time.deltaTime;
        if (damage <= 0f)
            return;

        curProtection = Mathf.Max(0, curProtection - damage);
        UpdateProtectionBar();

        TriggerDamageFlash();

        if (curProtection <= 0)
            StartDeathSequence();
    }

    private void ResolveNeutralizationVfx()
    {
        if (neutralizationVfx != null)
            return;

        Transform direct = transform.Find("CFXR Electrified 3");
        if (direct != null)
        {
            neutralizationVfx = direct.gameObject;
            return;
        }

        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == transform)
                continue;
            if (all[i].name.Contains("Electrified"))
            {
                neutralizationVfx = all[i].gameObject;
                return;
            }
        }
    }

    private void InitNeutralizationVfx()
    {
        ResolveNeutralizationVfx();
        if (neutralizationVfx == null)
            return;

        neutralizationParticles = neutralizationVfx.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < neutralizationParticles.Length; i++)
        {
            if (neutralizationParticles[i] == null)
                continue;

            ParticleSystem.MainModule main = neutralizationParticles[i].main;
            main.playOnAwake = false;
            neutralizationParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            neutralizationParticles[i].Clear(true);
        }

        neutralizationVfx.SetActive(false);
    }

    public bool IsNeutralizationItemSelected()
    {
        if (itemSelectManager == null)
            return false;
        if (GameManager.Instance != null && (GameManager.Instance.GameEnded || GameManager.Instance.IsPenalty))
            return false;

        return itemSelectManager.SelectedItemType != Item.ItemType.Scanner;
    }

    public void AddPollutantTouch()
    {
        pollutantTouchCount++;
        RefreshNeutralizationVfx();
    }

    public void RemovePollutantTouch()
    {
        if (pollutantTouchCount > 0)
            pollutantTouchCount--;
        RefreshNeutralizationVfx();
    }

    public void RefreshNeutralizationVfx()
    {
        bool play = false;

        if (pollutantTouchCount > 0 && !valveAnimActive && itemSelectManager != null)
        {
            if (GameManager.Instance == null || (!GameManager.Instance.GameEnded && !GameManager.Instance.IsPenalty))
            {
                Pollutant[] pollutants = FindObjectsByType<Pollutant>(FindObjectsSortMode.None);
                for (int i = 0; i < pollutants.Length; i++)
                {
                    Pollutant pollutant = pollutants[i];
                    if (pollutant == null || !pollutant.IsPlayerContactActive())
                        continue;
                    if (pollutant.type == Pollutant.PollutantType.TypeD)
                        continue;
                    if (itemSelectManager.IsSelectedItemRecommendedFor(pollutant))
                    {
                        play = true;
                        break;
                    }
                }
            }
        }

        SetNeutralizationVfx(play);
    }

    // 클리어 연출 전 사망 코루틴·Die 상태 정리 (타임오버와 클리어 경합 방지)
    public void CancelDeathForStageClear()
    {
        StopAllCoroutines();
        isDeathSequenceRunning = false;
        dieAnimPlayed = false;
        clearAnimActive = false;
        valveAnimActive = false;
        SetSpriteAlpha(1f);
        EnsureSpriteVisible();

        if (currentState != PlayerState.Die || anim == null)
            return;

        anim.ResetTrigger("Die");
        anim.ResetTrigger("Valve");
        anim.SetInteger("State", 0);
        currentState = PlayerState.Idle;
    }

    // 스테이지 클리어 시 Player_Clear (Animator 트리거 "Clear", Loop)
    public void PlayClearAnim()
    {
        if (currentState == PlayerState.Die || clearAnimActive)
            return;
        if (anim == null)
            return;

        clearAnimActive = true;
        valveAnimActive = false;
        pollutantTouchCount = 0;
        SetNeutralizationVfx(false);
        isMoving = false;
        hasInput = false;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        anim.ResetTrigger("Valve");
        anim.ResetTrigger("Die");
        EnsureSpriteVisible();
        anim.speed = 1f;
        anim.SetTrigger("Clear");
        anim.Play("Player_Clear", 0, 0f);
    }

    // clearAnimDelay(기본 2초) 경과 후 현재 프레임에서 클리어 애니 정지
    public void FreezeClearAnim()
    {
        if (!clearAnimActive || anim == null)
            return;

        anim.speed = 0f;
    }

    // 가스(D) + 가스밸브 접촉 시 Player_Valve (Animator 트리거 "Valve")
    public void SetValveAnimActive(bool active)
    {
        if (active)
        {
            if (currentState == PlayerState.Die)
                return;
            if (GameManager.Instance != null &&
                (GameManager.Instance.IsGameOverPending || GameManager.Instance.GameEnded))
                return;
            if (valveAnimActive)
                return;

            valveAnimActive = true;

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayValveSfx();

            if (anim == null)
                return;

            SetNeutralizationVfx(false);
            isMoving = false;
            hasInput = false;
            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            anim.ResetTrigger("Die");
            EnsureSpriteVisible();
            anim.SetTrigger("Valve");
            anim.Play("Player_Valve", 0, 0f);
            return;
        }

        bool hadValve = valveAnimActive;
        valveAnimActive = false;

        if (AudioManager.Instance != null)
            AudioManager.Instance.StopValveSfx();

        if (anim == null)
            return;

        anim.ResetTrigger("Valve");

        if (currentState == PlayerState.Die)
            return;

        if (!hadValve)
            return;

        ReturnToIdleAfterValve();
    }

    private void ReturnToIdleAfterValve()
    {
        currentState = PlayerState.Idle;
        isMoving = false;
        hasInput = false;
        EnsureSpriteVisible();
        if (anim != null)
        {
            anim.SetInteger("State", 0);
            anim.Play("Player_Idle", 0, 0f);
        }
    }

    private void EnsureSpriteVisible()
    {
        SetSpriteAlpha(1f);
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        if (childSpriteRenderers == null)
            return;

        for (int i = 0; i < childSpriteRenderers.Length; i++)
        {
            if (childSpriteRenderers[i] != null)
                childSpriteRenderers[i].enabled = true;
        }
    }

    public void SetNeutralizationVfx(bool play)
    {
        if (neutralizationVfx == null)
            return;

        if (!play)
        {
            if (neutralizationParticles != null)
            {
                for (int i = 0; i < neutralizationParticles.Length; i++)
                {
                    if (neutralizationParticles[i] == null)
                        continue;
                    neutralizationParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    neutralizationParticles[i].Clear(true);
                }
            }

            neutralizationVfx.SetActive(false);
            return;
        }

        if (!neutralizationVfx.activeSelf)
            neutralizationVfx.SetActive(true);

        if (neutralizationParticles == null)
            return;

        for (int i = 0; i < neutralizationParticles.Length; i++)
        {
            if (neutralizationParticles[i] != null && !neutralizationParticles[i].isPlaying)
                neutralizationParticles[i].Play();
        }
    }

    public void AddProtection(float amount)
    {
        if (amount <= 0f)
            return;
        if (currentState == PlayerState.Die)
            return;
        if (GameManager.Instance != null && GameManager.Instance.GameEnded)
            return;

        curProtection = Mathf.Min(maxProtection, curProtection + amount);
        UpdateProtectionBar();
    }

    public void StartDeathSequence()
    {
        if (isDeathSequenceRunning || currentState == PlayerState.Die)
            return;

        isDeathSequenceRunning = true;
        pollutantTouchCount = 0;
        SetNeutralizationVfx(false);
        Debug.Log("플레이어가 사망했습니다.");
        canMove = false;
        isMoving = false;
        hasInput = false;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        SetState(PlayerState.Die);
        SetValveAnimActive(false);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayDieSplattSfx();

        if (GameManager.Instance != null && !GameManager.Instance.IsGameOverPending)
            GameManager.Instance.BeginPlayerDeathSequence();

        StartCoroutine(DieAndFadeOutRoutine());
    }

    private IEnumerator DieAndFadeOutRoutine()
    {
        float hold = dieAnimWait;
        if (GameManager.Instance != null)
            hold = GameManager.Instance.dieAnimDelay;

        if (hold > 0f)
            yield return new WaitForSeconds(hold);

        yield return FadeAlphaTo(0f, dieFadeDuration);
        isDeathSequenceRunning = false;
    }

    private IEnumerator FadeAlphaTo(float targetAlpha, float duration)
    {
        if (duration <= 0f)
        {
            SetSpriteAlpha(targetAlpha);
            yield break;
        }

        float startAlpha = GetSpriteAlpha();
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            SetSpriteAlpha(alpha);
            yield return null;
        }

        SetSpriteAlpha(targetAlpha);
    }

    private float GetSpriteAlpha()
    {
        if (spriteRenderer != null)
            return spriteRenderer.color.a;
        if (childSpriteRenderers != null && childSpriteRenderers.Length > 0 && childSpriteRenderers[0] != null)
            return childSpriteRenderers[0].color.a;
        return 1f;
    }

    private void TriggerDamageFlash()
    {
        if (currentState == PlayerState.Die) return;
        if (_damageFlashRoutine != null) return; // 이미 반짝이는 중이면 무시
        _damageFlashRoutine = StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        SetSpriteColor(damageFlashColor);
        yield return new WaitForSeconds(damageFlashDuration);
        SetSpriteColor(Color.white);
        yield return new WaitForSeconds(damageFlashCooldown);
        _damageFlashRoutine = null;
    }

    private void SetSpriteColor(Color tint)
    {
        // tint.a = 0이면 원본 흰색, 1이면 완전 tint 색상으로 lerp
        Color blended = new Color(
            Mathf.Lerp(1f, tint.r, tint.a),
            Mathf.Lerp(1f, tint.g, tint.a),
            Mathf.Lerp(1f, tint.b, tint.a),
            1f
        );

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.r = blended.r; c.g = blended.g; c.b = blended.b;
            spriteRenderer.color = c;
        }

        if (childSpriteRenderers == null) return;
        for (int i = 0; i < childSpriteRenderers.Length; i++)
        {
            if (childSpriteRenderers[i] != null)
            {
                Color c = childSpriteRenderers[i].color;
                c.r = blended.r; c.g = blended.g; c.b = blended.b;
                childSpriteRenderers[i].color = c;
            }
        }
    }

    private void SetSpriteAlpha(float alpha)
    {
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }

        if (childSpriteRenderers == null)
            return;

        for (int i = 0; i < childSpriteRenderers.Length; i++)
        {
            if (childSpriteRenderers[i] == null)
                continue;
            Color color = childSpriteRenderers[i].color;
            color.a = alpha;
            childSpriteRenderers[i].color = color;
        }
    }

    public void ResetForStage()
    {
        StopAllCoroutines();
        isReturning = false;
        pollutantTouchCount = 0;
        valveAnimActive = false;
        clearAnimActive = false;
        SetNeutralizationVfx(false);

        curProtection = maxProtection;
        UpdateProtectionBar();

        ResetRange();
        SnapToStartPosition();

        transform.localScale = new Vector3(1f, 1f, 1f);
        dieAnimPlayed = false;
        isDeathSequenceRunning = false;
        SetSpriteAlpha(1f);
        currentState = PlayerState.Idle; // Die 가드 우회: 직접 초기화
        if (anim != null)
        {
            anim.speed = 1f;
            anim.ResetTrigger("Die");
            anim.ResetTrigger("Valve");
            anim.ResetTrigger("Clear");
            anim.SetInteger("State", 0);
            anim.Play("Player_Idle", 0, 0f);
        }
        SetState(PlayerState.Idle);
        canMove = true;
        isMoving = false;
        hasInput = false;
        gameObject.SetActive(true);
    }

    public void SnapToStartPosition()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = startPosition;
        }
        transform.position = startPosition;
    }

    private void SetState(PlayerState nextState)
    {
        if (currentState == nextState)
            return;

        if (nextState == PlayerState.Die)
        {
            currentState = PlayerState.Die;
            if (anim != null && !dieAnimPlayed)
            {
                dieAnimPlayed = true;
                anim.SetTrigger("Die");
            }
            return;
        }

        if (currentState == PlayerState.Die)
            return;

        if (valveAnimActive || clearAnimActive)
            return;

        currentState = nextState;
        if (anim != null)
            anim.SetInteger("State", (int)currentState);
    }
}
