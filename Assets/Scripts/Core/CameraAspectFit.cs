using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Camera))]
public class CameraAspectFit : MonoBehaviour
{
    [SerializeField] private float designOrthographicSize = 512f;
    [SerializeField] private float designAspectWidth = 1920f;
    [SerializeField] private float designAspectHeight = 1080f;

    [Header("월드 배경 fit")]
    [Tooltip("비우면 씬의 Background 컴포넌트를 찾습니다.")]
    [SerializeField] private Transform coverBackground;
    [SerializeField] private Vector2 coverDesignSize = new Vector2(1920f, 1080f);

    private Camera cam;
    private int lastWidth;
    private int lastHeight;

    void Awake()
    {
        cam = GetComponent<Camera>();
        Apply();
    }

    void Start()
    {
        Apply();
    }

    void LateUpdate()
    {
        if (Screen.width == lastWidth && Screen.height == lastHeight)
            return;

        Apply();
    }

    private void Apply()
    {
        if (cam == null || !cam.orthographic)
            return;

        lastWidth = Screen.width;
        lastHeight = Screen.height;

        cam.orthographicSize = designOrthographicSize;

        float windowAspect = (float)Screen.width / Screen.height;
        ApplyBackgroundFit(windowAspect);
    }

    private void ApplyBackgroundFit(float windowAspect)
    {
        if (coverBackground == null)
        {
            Background bg = FindAnyObjectByType<Background>();
            if (bg != null)
                coverBackground = bg.transform;
        }

        if (coverBackground == null || coverDesignSize.x <= 0f || coverDesignSize.y <= 0f)
            return;

        float visibleW = cam.orthographicSize * 2f * windowAspect;
        float visibleH = cam.orthographicSize * 2f;
        // cover: 화면 전체 채움(가로·세로 잘릴 수 있음). contain(Min)은 16:9보다 넓은 풀창에서 좌우 검정 여백.
        float mult = Mathf.Max(visibleW / coverDesignSize.x, visibleH / coverDesignSize.y);

        coverBackground.localScale = new Vector3(
            coverDesignSize.x * mult,
            coverDesignSize.y * mult,
            1f);
    }
}
