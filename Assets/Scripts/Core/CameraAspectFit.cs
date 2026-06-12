using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAspectFit : MonoBehaviour
{
    [SerializeField] private float designOrthographicSize = 512f;
    [SerializeField] private float designAspectWidth = 1920f;
    [SerializeField] private float designAspectHeight = 1080f;

    private Camera cam;
    private int lastWidth;
    private int lastHeight;

    void Awake()
    {
        cam = GetComponent<Camera>();
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

        float designAspect = designAspectWidth / designAspectHeight;
        float windowAspect = (float)Screen.width / Screen.height;

        // 화면을 꽉 채우도록(cover). 넓은 창은 ortho 유지, 좁은 창만 ortho 확대
        if (windowAspect >= designAspect)
            cam.orthographicSize = designOrthographicSize;
        else
            cam.orthographicSize = designOrthographicSize * designAspect / windowAspect;
    }
}
