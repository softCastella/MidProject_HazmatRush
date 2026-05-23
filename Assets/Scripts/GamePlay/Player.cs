using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed = 200f; // 플레이어 이동 속도
    public float leftLimit = -785f; // 왼쪽 이동 제한
    public float rightLimit = -403f; // 오른쪽 이동 제한
    public int protection = 100; // 플레이어 방호복 수치(생존hp)

    public bool isMoving; // 이동 중인지
    public bool hasInput; // 입력이 들어왔는지

    private Animator anim; // 애니메이터 컴포넌트
    private float startLeft; // 기본 왼쪽 이동 범위 저장
    private float startRight; // 기본 오른쪽 이동 범위 저장

    void Start()
    {
        isMoving = false;
        anim = GetComponent<Animator>();

        startLeft = leftLimit;
        startRight = rightLimit;
    }

    void Update()
    {
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
        pos.x = Mathf.Clamp(pos.x, leftLimit, rightLimit);
        transform.position = pos;

        if (h > 0f)
            transform.localScale = new Vector3(1f, 1f, 1f);
        else if (h < 0f)
            transform.localScale = new Vector3(-1f, 1f, 1f);

        isMoving = h != 0f;
        hasInput = isMoving;

        if (anim != null)
            anim.SetInteger("State", isMoving ? 1 : 0);
    }

    // 오염원 가장자리까지만 움직일 수 있도록 범위를 확장합니다.
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

        protection -= 10;
        protection = Mathf.Max(0, protection);
        Debug.Log($"플레이어가 오염원과 충돌했습니다. 방호복 HP: {protection}");
    }
}
