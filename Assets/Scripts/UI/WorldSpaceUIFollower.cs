using UnityEngine;

public class WorldSpaceUIFollower : MonoBehaviour
{
    public Transform worldTarget;
    [Tooltip("체크 시 스프라이트 위쪽에 붙입니다. worldOffset은 그 위 여백입니다.")]
    public bool placeAboveSprite = true;
    public Vector3 worldOffset = new Vector3(0f, 0.15f, 0f);

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

        Vector3 worldPos = GetFollowWorldPosition();
        Vector2 screenPos = cam.WorldToScreenPoint(worldPos);
        Camera canvasCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.GetComponent<RectTransform>(), screenPos, canvasCam, out Vector2 localPos);

        // pivot이 가운데(0.5)면 바가 스프라이트 위에 겹침 → 아래쪽을 스프라이트 위에 맞춤
        float barBottomOffset = rectTransform.rect.height * rectTransform.pivot.y * rectTransform.lossyScale.y;
        localPos.y += barBottomOffset;

        rectTransform.localPosition = localPos;
    }

    Vector3 GetFollowWorldPosition()
    {
        if (!placeAboveSprite)
            return worldTarget.position + worldOffset;

        Bounds bounds = GetTargetBounds(worldTarget);
        return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z) + worldOffset;
    }

    Bounds GetTargetBounds(Transform target)
    {
        SpriteRenderer[] sprites = target.GetComponentsInChildren<SpriteRenderer>(true);
        if (sprites.Length > 0)
        {
            Bounds bounds = sprites[0].bounds;
            for (int i = 1; i < sprites.Length; i++)
                bounds.Encapsulate(sprites[i].bounds);
            return bounds;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        Collider2D collider = target.GetComponent<Collider2D>();
        if (collider != null)
            return collider.bounds;

        return new Bounds(target.position, Vector3.one);
    }
}
