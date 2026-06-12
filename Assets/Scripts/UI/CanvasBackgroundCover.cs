using UnityEngine;

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

        float parentW = canvasRect.rect.width;
        float parentH = canvasRect.rect.height;
        if (parentW <= 0f || parentH <= 0f)
            return;

        float designAspect = designWidth / designHeight;
        float parentAspect = parentW / parentH;

        CoverIfFound(parentW, parentH, designAspect, parentAspect, FindChild(canvasRect, "Bg"));
        CoverIfFound(parentW, parentH, designAspect, parentAspect, FindChild(canvasRect, "TitleImage"));
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
