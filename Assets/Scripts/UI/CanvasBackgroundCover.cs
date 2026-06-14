using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasScaler))]
public class CanvasBackgroundCover : MonoBehaviour
{
    [SerializeField] private float designWidth = 1920f;
    [SerializeField] private float designHeight = 1080f;

    private RectTransform canvasRect;
    private CanvasScaler canvasScaler;
    private int lastWidth;
    private int lastHeight;

    void Awake()
    {
        canvasRect = GetComponent<RectTransform>();
        canvasScaler = GetComponent<CanvasScaler>();
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

        ApplyDynamicReferenceResolution();

        float parentW;
        float parentH;
        GetCanvasLogicalSize(out parentW, out parentH);

        float windowAspect = (float)Screen.width / Screen.height;
        float designAspect = designWidth / designHeight;

        CoverIfFound(parentW, parentH, designAspect, windowAspect, FindChild(canvasRect, "Bg"));
        CoverIfFound(parentW, parentH, designAspect, windowAspect, FindChild(canvasRect, "TitleImage"));
    }

    private void ApplyDynamicReferenceResolution()
    {
        if (canvasScaler == null || canvasScaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            return;

        canvasScaler.matchWidthOrHeight = 0f;

        float screenAspect = (float)Screen.width / Screen.height;
        float designAspect = designWidth / designHeight;

        // 세로가 긴 화면: 가로 기준 스케일 + reference 높이만 늘려 하단 UI가 화면 밑에 붙음
        if (screenAspect < designAspect)
            canvasScaler.referenceResolution = new Vector2(designWidth, designWidth / screenAspect);
        else
            canvasScaler.referenceResolution = new Vector2(designWidth, designHeight);
    }

    private void GetCanvasLogicalSize(out float parentW, out float parentH)
    {
        parentW = designWidth;
        parentH = designHeight;

        if (canvasScaler != null && canvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            Vector2 refRes = canvasScaler.referenceResolution;
            float scaleW = Screen.width / refRes.x;
            float scaleH = Screen.height / refRes.y;
            float scale = Mathf.Lerp(scaleW, scaleH, canvasScaler.matchWidthOrHeight);
            if (scale > 0f)
            {
                parentW = Screen.width / scale;
                parentH = Screen.height / scale;
                return;
            }
        }

        Rect rect = canvasRect.rect;
        if (rect.width > 1f && rect.height > 1f)
        {
            parentW = rect.width;
            parentH = rect.height;
            return;
        }

        float windowAspect = (float)Screen.width / Screen.height;
        parentH = designHeight;
        parentW = parentH * windowAspect;
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
