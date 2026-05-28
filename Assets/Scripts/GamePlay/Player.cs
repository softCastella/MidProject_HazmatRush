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
    public float leftLimit = -785f; // 왼쪽 이동 제한
    public float rightLimit = -403f; // 오른쪽 이동 제한
    public float maxProtection = 100f; // 방호복 최대 수치
    public float curProtection; // 현재 방호복 수치
    public TMP_Text protectionNumText; // 방호복 수치 표시 텍스트

    public bool isMoving; // 이동 중인지
    public bool hasInput; // 입력이 들어왔는지
    public bool canMove = false; // 이동 가능 여부
    public ItemSelectManager itemSelectManager;

    private Animator anim; // 애니메이터 컴포넌트
    private float startLeft; // 기본 왼쪽 이동 범위 저장
    private float startRight; // 기본 오른쪽 이동 범위 저장
    private PlayerState currentState = PlayerState.Idle;

    void Awake()
    {
        startLeft = leftLimit;
        startRight = rightLimit;
        curProtection = maxProtection;
        anim = GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>(true);

        if (anim == null)
            Debug.LogWarning("Player: Animator를 찾지 못했습니다. 이동 애니메이션(State)이 재생되지 않습니다.");
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
    }

    void Update()
    {
        if (!canMove)
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

        Vector3 pos = transform.position;
        pos.x += h * moveSpeed * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, Mathf.Min(leftLimit, rightLimit), Mathf.Max(leftLimit, rightLimit));
        transform.position = pos;

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
        Debug.Log($"[Player] 방호복 HP 감소: -{damage:F2} (pollutantDps={pollutantDps:F2}) / 현재 HP: {curProtection:F2}");

        if (curProtection <= 0)
        {
            Debug.Log("플레이어가 사망했습니다.");
            canMove = false;
            SetState(PlayerState.Die);
            Destroy(gameObject);
        }
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
