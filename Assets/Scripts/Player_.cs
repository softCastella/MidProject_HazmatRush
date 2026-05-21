using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed = 400f; // 플레이어 이동 속도입니다.
    public bool isMoving; // 플레이어가 실제로 이동 중인지 여부입니다.
    public bool hasInput; // 플레이어 입력 여부입니다.
    private enum State { Idle = 0, Move = 1 } // 애니메이터 상태를 정의하는 열거형입니다.
    private State currentState = State.Idle;
    Animator anim; // 플레이어 애니메이터입니다.

    void Start()
    {
        anim = GetComponent<Animator>(); // 애니메이터 컴포넌트를 가져옵니다.
    }

    void Update()
    {
        // 좌우 입력값을 즉시 가져옴(왼쪽 화살표 또는 A : -1/입력 없음 : 0/오른쪽 화살표 또는 D : 1)
        float h = Input.GetAxisRaw("Horizontal");
        if (h == 0f)
        {
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
                h = -1f;
            else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
                h = 1f;
        }

        // 이동 (월드 x축으로 이동)
        Vector3 currentPosition = transform.position;
        Vector3 targetPosition = currentPosition + Vector3.right * h * moveSpeed * Time.deltaTime;
        targetPosition.x = Mathf.Clamp(targetPosition.x, -785f, -403f);
        transform.position = targetPosition;

        // 방향 전환 처리
        if (h > 0f)
        {
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
        else if (h < 0f)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f);
        }

        // 애니메이터 상태 전환 처리
        if (h == 0f)
        {
            currentState = State.Idle;
            isMoving = false;
            hasInput = false;
        }
        else
        {
            currentState = State.Move;
            isMoving = true;
            hasInput = true;
        }

        if (anim != null)
            anim.SetInteger("State", (int)currentState);
    }
}
