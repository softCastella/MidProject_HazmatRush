using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    private enum PlayerState
    {
        Idle = 0,
        Move = 1,
        Die = 2
    }

    public float moveSpeed = 400f; // 플레이어 이동 속도
    public float returnMoveSpeed = 600f; // 시작 지점 복귀(3차 이동) 속도
    public float leftLimit = -785f; // 왼쪽 이동 제한
    public float rightLimit = -403f; // 오른쪽 이동 제한
    public float maxProtection = 100f; // 방호복 최대 수치
    public float curProtection; // 현재 방호복 수치
    public TMP_Text protectionNumText; // 방호복 수치 표시 텍스트

    public Slider protectionSlider;

    private TMP_Text protectionHpText;

    public bool isMoving; // 이동 중인지
    public bool hasInput; // 입력이 들어왔는지
    public bool canMove = false; // 이동 가능 여부
    public ItemSelectManager itemSelectManager;

    private Animator anim; // 애니메이터 컴포넌트
    private Rigidbody2D rb;
    private float startLeft; // 기본 왼쪽 이동 범위 저장
    private float startRight; // 기본 오른쪽 이동 범위 저장
    private Vector3 startPosition; // 게임 시작 시 플레이어 위치
    private PlayerState currentState = PlayerState.Idle;

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
    }

    void Start()
    {
        if (protectionNumText == null)
        {
            GameObject protectionObj = GameObject.Find("ProtectionNum");
            if (protectionObj != null)
                protectionNumText = protectionObj.GetComponent<TMP_Text>();
        }

        if (itemSelectManager == null)
            itemSelectManager = FindAnyObjectByType<ItemSelectManager>();

        GuideTxt guide = FindAnyObjectByType<GuideTxt>();
        if (guide == null || string.IsNullOrEmpty(guide.defaultMessage))
            canMove = true;

        UpdateProtectionText();

        if (protectionSlider != null)
        {
            protectionSlider.minValue = 0f;
            protectionSlider.maxValue = 1f;
            protectionSlider.value = 1f;
            protectionSlider.gameObject.SetActive(false);
            protectionHpText = protectionSlider.GetComponentInChildren<TMP_Text>(true);
        }
    }

    public void UpdateProtectionBar()
    {
        if (protectionSlider != null)
            protectionSlider.value = curProtection / maxProtection;
        if (protectionHpText != null)
            protectionHpText.text = Mathf.FloorToInt(curProtection).ToString();
    }

    void Update()
    {
        if (!canMove || (GameManager.Instance != null && GameManager.Instance.IsPaused))
        {
            isMoving = false;
            hasInput = false;
            SetState(currentState == PlayerState.Die ? PlayerState.Die : PlayerState.Idle);
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

        float newX = transform.position.x + h * moveSpeed * Time.deltaTime;
        newX = Mathf.Clamp(newX, Mathf.Min(leftLimit, rightLimit), Mathf.Max(leftLimit, rightLimit));

        if (rb != null)
            rb.MovePosition(new Vector2(newX, transform.position.y));
        else
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);

        if (h > 0f)
            transform.localScale = new Vector3(1f, 1f, 1f);
        else if (h < 0f)
            transform.localScale = new Vector3(-1f, 1f, 1f);

        isMoving = h != 0f;
        hasInput = isMoving;
        SetState(isMoving ? PlayerState.Move : PlayerState.Idle);
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

    // 오염원 중화 후 시작 지점으로 자동 복귀시키고 1차 이동 범위로 되돌립니다.
    public IEnumerator AutoReturnToStart()
    {
        if (currentState == PlayerState.Die)
            yield break;

        canMove = false;
        ResetRange();

        // 복귀 중에도 항상 오른쪽을 향하도록 고정합니다.
        transform.localScale = new Vector3(1f, 1f, 1f);

        SetState(PlayerState.Move);

        while (Mathf.Abs(transform.position.x - startPosition.x) > 1f)
        {
            float newX = Mathf.MoveTowards(transform.position.x, startPosition.x, returnMoveSpeed * Time.deltaTime);
            if (rb != null)
                rb.MovePosition(new Vector2(newX, transform.position.y));
            else
                transform.position = new Vector3(newX, transform.position.y, transform.position.z);
            yield return null;
        }

        if (rb != null)
            rb.MovePosition(new Vector2(startPosition.x, transform.position.y));
        else
            transform.position = new Vector3(startPosition.x, transform.position.y, transform.position.z);

        SetState(PlayerState.Idle);
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

    private void UpdateProtectionText()
    {
        if (protectionNumText != null)
            protectionNumText.text = $"{Mathf.FloorToInt(curProtection)}%";
    }

    public void ApplyPollutantDamage(float pollutantDps)
    {
        if (currentState == PlayerState.Die)
            return;

        float damage = pollutantDps * Time.deltaTime;
        if (damage <= 0f)
            return;

        curProtection = Mathf.Max(0, curProtection - damage);
        UpdateProtectionText();
        UpdateProtectionBar();
        Debug.Log($"[Player] 방호복 HP 감소: -{damage:F2} (pollutantDps={pollutantDps:F2}) / 현재 HP: {curProtection:F2}");

        if (curProtection <= 0)
        {
            Debug.Log("플레이어가 사망했습니다.");
            canMove = false;
            SetState(PlayerState.Die);
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerGameOver(GameManager.GameOverCause.ProtectionDepleted);
        }
    }

    public void ResetForStage()
    {
        StopAllCoroutines();

        curProtection = maxProtection;
        UpdateProtectionText();
        UpdateProtectionBar();
        if (protectionSlider != null)
            protectionSlider.gameObject.SetActive(false);

        ResetRange();
        SnapToStartPosition();

        transform.localScale = new Vector3(1f, 1f, 1f);
        SetState(PlayerState.Idle);
        canMove = true;
        isMoving = false;
        hasInput = false;
        gameObject.SetActive(true);
    }

    void SnapToStartPosition()
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

        currentState = nextState;
        if (anim != null)
            anim.SetInteger("State", (int)currentState);
    }
}
