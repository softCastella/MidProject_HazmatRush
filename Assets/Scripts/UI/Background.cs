using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class Background : MonoBehaviour
{
    [Header("Scroll Settings")]
    [Range(0f, 5f)]
    public float scrollSpeed = 0.2f; // 배경 스크롤 속도, 값이 크면 더 빠르게 움직입니다.
    public bool scrollRight = true; // true면 오른쪽으로, false면 왼쪽으로 스크롤합니다.

    // 플레이어 입력이 있을 때만 배경이 돌아가도록 설정합니다.
    [Header("Pause Settings")]
    public bool pauseWhenPlayerStops = true; // 입력이 없으면 배경을 멈춥니다.
    public bool isPaused = true; // true면 배경이 멈춥니다.
    public float stopSmoothTime = 0.2f; // 멈출 때 부드럽게 감속하는 시간입니다.

    [Header("Background Textures")]
    public Texture[] backgroundTextures; // 인덱스로 관리할 이미지 목록입니다.
    public int currentBackgroundIndex = 0; // 현재 선택된 배경 번호입니다.

    private Renderer rend; // 이 오브젝트의 Renderer입니다.
    private string textureProperty = ""; // 머티리얼이 사용하는 텍스처 이름입니다.
    private float offsetX; // 텍스처 오프셋 X 값입니다.
    private float currentScrollSpeed; // 실제로 현재 적용되는 속도입니다.
    private float scrollVelocity; // SmoothDamp 함수 내부에서 사용하는 값입니다.
    private Vector3 lastPlayerPosition; // 플레이어 위치 변화를 계산하기 위한 지난 위치입니다.

    void Start()
    {
        // Renderer 컴포넌트를 가져옵니다.
        rend = GetComponent<Renderer>();

        if (rend.material == null)
            return; // 머티리얼이 없으면 더 이상 실행하지 않습니다.

        // 머티리얼이 어떤 텍스처 이름을 사용하는지 확인합니다.
        if (rend.material.HasProperty("_BaseMap"))
        {
            textureProperty = "_BaseMap"; // URP/Lit 같은 셰이더에서 사용
        }
        else if (rend.material.HasProperty("_MainTex"))
        {
            textureProperty = "_MainTex"; // 일반 셰이더에서 사용
        }

        if (!string.IsNullOrEmpty(textureProperty))
        {
            Texture tex = rend.material.GetTexture(textureProperty);
            if (tex != null)
                tex.wrapMode = TextureWrapMode.Repeat; // 텍스처 반복 모드로 설정합니다.
        }

    }

    void Update()
    {
        if (rend == null || rend.material == null || string.IsNullOrEmpty(textureProperty))
            return; // 준비가 안 된 경우 아무 것도 하지 않습니다.

        float horizontal = Input.GetAxisRaw("Horizontal");
        float deadZone = 0.3f;
        bool axisInput = Mathf.Abs(horizontal) > deadZone;
        bool keyInput = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow)
            || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D);
        bool playerInput = axisInput || keyInput;

        float inputDirection = 0f;
        if (playerInput)
        {
            if (axisInput)
                inputDirection = horizontal;
            else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
                inputDirection = -1f;
            else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
                inputDirection = 1f;
        }

        bool shouldStop = isPaused;
        if (pauseWhenPlayerStops)
            shouldStop = shouldStop || !playerInput;

        float targetSpeed = shouldStop ? 0f : scrollSpeed;
        currentScrollSpeed = Mathf.SmoothDamp(currentScrollSpeed, targetSpeed, ref scrollVelocity, stopSmoothTime);

        float direction = 0f;
        if (!shouldStop && playerInput)
        {
            direction = Mathf.Sign(inputDirection); // 플레이어 입력 방향에 따라 배경 시각 이동을 반대로 보이도록 합니다.
        }

        offsetX += direction * currentScrollSpeed * Time.deltaTime; // 오프셋을 시간에 맞춰 누적합니다.
        offsetX = Mathf.Repeat(offsetX, 1f); // 오프셋을 0~1 범위로 유지합니다.

        Vector2 offset = new Vector2(offsetX, 0f);
        rend.material.SetTextureOffset(textureProperty, offset); // 실제 머티리얼에 적용합니다.
    }

    // 배경 스크롤을 즉시 멈춥니다.
    public void PauseScroll()
    {
        isPaused = true; // 배경 스크롤을 멈춥니다.
    }

    // 배경 스크롤을 다시 시작합니다.
    public void ResumeScroll()
    {
        isPaused = false; // 배경 스크롤을 다시 시작합니다.
        // 플레이어 정지 감지를 사용 중이면 마지막 위치를 초기화해야 합니다.
        // if (pauseWhenPlayerStops && player != null)
        //     lastPlayerPosition = player.position;
    }

    // 멈추거나 다시 시작하는 토글 함수입니다.
    public void ToggleScroll()
    {
        // 멈춘 상태(isPaused)와 스크롤 상태를 반전합니다.
        // 한 번 호출하면 멈추고, 다시 호출하면 다시 움직입니다.
        isPaused = !isPaused;

        // 플레이어 정지 감지를 추가할 때는 아래와 같이 마지막 위치를 다시 초기화해야 합니다.
        // if (!isPaused && pauseWhenPlayerStops && player != null)
        //     lastPlayerPosition = player.position;
    }

    // 텍스처를 직접 전달하여 현재 배경 이미지를 교체합니다.
    public void ChangeBackgroundTexture(Texture newTexture)
    {
        // 텍스처가 없거나 준비되지 않은 경우 아무 것도 하지 않습니다.
        if (rend == null || rend.material == null || string.IsNullOrEmpty(textureProperty) || newTexture == null)
            return;

        // 새 텍스처를 반복 모드로 설정합니다.
        newTexture.wrapMode = TextureWrapMode.Repeat;

        // 렌더러 머티리얼의 현재 텍스처를 새 텍스처로 바꿉니다.
        rend.material.SetTexture(textureProperty, newTexture);
    }

    // 인덱스로 등록된 배경 목록에서 특정 이미지를 선택합니다.
    public void ChangeBackgroundByIndex(int index)
    {
        // 텍스처 배열이 없거나 비어 있으면 아무 것도 하지 않습니다.
        if (backgroundTextures == null || backgroundTextures.Length == 0)
            return;

        // 잘못된 인덱스면 아무 것도 하지 않습니다.
        if (index < 0 || index >= backgroundTextures.Length)
            return;

        // 현재 인덱스를 바꾸고 그 인덱스의 텍스처로 교체합니다.
        currentBackgroundIndex = index;
        ChangeBackgroundTexture(backgroundTextures[index]);
    }

    // 등록된 배경 목록에서 다음 이미지를 선택합니다.
    public void ChangeBackgroundNext()
    {
        // 텍스처 배열이 없으면 아무 것도 하지 않습니다.
        if (backgroundTextures == null || backgroundTextures.Length == 0)
            return;

        // 다음 인덱스로 이동합니다. 마지막에서 다시 처음으로 돌아옵니다.
        currentBackgroundIndex = (currentBackgroundIndex + 1) % backgroundTextures.Length;
        ChangeBackgroundTexture(backgroundTextures[currentBackgroundIndex]);
    }

    // 등록된 배경 목록에서 이전 이미지를 선택합니다.
    public void ChangeBackgroundPrevious()
    {
        // 텍스처 배열이 없으면 아무 것도 하지 않습니다.
        if (backgroundTextures == null || backgroundTextures.Length == 0)
            return;

        // 이전 인덱스로 이동합니다. 처음에서 다시 마지막으로 돌아옵니다.
        currentBackgroundIndex = (currentBackgroundIndex - 1 + backgroundTextures.Length) % backgroundTextures.Length;
        ChangeBackgroundTexture(backgroundTextures[currentBackgroundIndex]);
    }
}
