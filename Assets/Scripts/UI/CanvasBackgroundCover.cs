using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(RectTransform))]
public class CanvasBackgroundCover : MonoBehaviour
{
    [SerializeField] private float designWidth = 1920f;
    [SerializeField] private float designHeight = 1080f;

    private RectTransform canvasRect;
    private int lastWidth;
    private int lastHeight;

    void Awake()
    {
        canvasRect = GetComponent<RectTransform>();
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
        if (canvasRect == null)
            return;

        lastWidth = Screen.width;
        lastHeight = Screen.height;

        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        // Canvas Scaler(Match Width) 기준 논리 크기 — rect가 0인 첫 프레임에도 동작
        float windowAspect = (float)Screen.width / Screen.height;
        float parentW = designWidth;
        float parentH = designWidth / windowAspect;
        float designAspect = designWidth / designHeight;

        Camera cam = Camera.main;
        if (cam != null)
            cam.backgroundColor = Color.black;

        CoverIfFound(parentW, parentH, designAspect, windowAspect, FindChild(canvasRect, "Bg"));
        CoverIfFound(parentW, parentH, designAspect, windowAspect, FindChild(canvasRect, "TitleImage"));
    }

    private static RectTransform FindChild(RectTransform root, string childName)
    {
        if (root == null)
            return null;

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != root && all[i].name == childName)
                return all[i] as RectTransform;
        }

        return null;
    }

    private static void CoverIfFound(float parentW, float parentH, float designAspect, float parentAspect, RectTransform child)
    {
        if (child == null)
            return;

        child.anchorMin = Vector2.zero;
        child.anchorMax = Vector2.one;
        child.pivot = new Vector2(0.5f, 0.5f);
        child.anchoredPosition = Vector2.zero;

        if (parentAspect >= designAspect)
        {
            float coverHeight = parentW / designAspect;
            float extra = (coverHeight - parentH) * 0.5f;
            child.offsetMin = new Vector2(0f, -extra);
            child.offsetMax = new Vector2(0f, extra);
        }
        else
        {
            float coverWidth = parentH * designAspect;
            float extra = (coverWidth - parentW) * 0.5f;
            child.offsetMin = new Vector2(-extra, 0f);
            child.offsetMax = new Vector2(extra, 0f);
        }
    }
}
