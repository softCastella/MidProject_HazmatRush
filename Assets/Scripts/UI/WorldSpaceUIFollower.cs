using UnityEngine;

public class WorldSpaceUIFollower : MonoBehaviour
{
    public Transform worldTarget;
    public Vector3 worldOffset = new Vector3(0, 1.5f, 0);

    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private Camera cam;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>(true);
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (worldTarget == null || rootCanvas == null || cam == null)
            return;

        Vector3 worldPos = worldTarget.position + worldOffset;
        Vector2 screenPos = cam.WorldToScreenPoint(worldPos);
        Camera canvasCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.GetComponent<RectTransform>(), screenPos, canvasCam, out Vector2 localPos);
        rectTransform.localPosition = localPos;
    }
}
