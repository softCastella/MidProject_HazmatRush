using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClearPanelUI : MonoBehaviour
{
    [Header("Stars (?쇱そ遺??starCount媛쒕쭔 梨꾩?)")]
    public Animator[] starAnimators;

    [Tooltip("StarFilled ?좊땲 ?꾨젅???쒖꽌 (00_54~00_58). 鍮꾩뼱 ?덉쑝硫?泥?蹂?Image ?ㅽ봽?쇱씠?몃쭔 ?ъ슜")]
    public Sprite[] starFillFrames;

    [Tooltip("蹂?Image 媛濡?諛섏쟾 (梨꾩슦湲?諛⑺뼢??嫄곌씀濡쒖씪 ??耳쒓린)")]
    public bool flipStarScaleX = false;

    [Header("Text")]
    public TMP_Text breakdownText;

    private static readonly string[] StarNames = { "Star_0", "Star_1", "Star_2" };

    private Image[] starImages;
    private Animator starFilledRootAnimator;
    private Coroutine _starFillRoutine;

    private const float StarRevealInterval = 0.45f; // 별 하나 재생 후 다음 별까지 간격
    private const float StarAnimDuration  = 0.35f; // StarFilled.anim 길이 (~0.33s)
    private const int   FlashCount        = 3;     // 3스타 달성 시 번쩍 횟수
    private const float FlashOnDuration   = 0.18f; // 번쩍 켜짐 시간
    private const float FlashOffDuration  = 0.12f; // 번쩍 꺼짐 시간

    public void Apply(StageClearResult result)
    {
        if (breakdownText != null && result.breakdownLines != null)
            breakdownText.text = string.Join("\n", result.breakdownLines);

        if (!isActiveAndEnabled)
            return;

        EnsureStarsReady();
        DisableDecorRaycasts();
        SetupResultButtons();
        FillStars(result.starCount);
    }

    private void SetupResultButtons()
    {
        SetupButton("nextStageBtn");
        SetupButton("toTitleBtn");
    }

    private void SetupButton(string name)
    {
        Transform btnRoot = FindChildByName(transform, name);
        if (btnRoot == null)
            return;

        Button btn = btnRoot.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = true;
            EnsureButtonClick(btn, name);
        }

        Image image = btnRoot.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = true;

        TMP_Text[] texts = btnRoot.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
            texts[i].raycastTarget = false;

        btnRoot.SetAsLastSibling();
    }

    private Transform FindChildByName(Transform root, string name)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == name)
                return child;

            Transform found = FindChildByName(child, name);
            if (found != null)
                return found;
        }

        return null;
    }

    private void EnsureButtonClick(Button btn, string name)
    {
        if (GameManager.Instance == null)
            return;

        btn.onClick.RemoveAllListeners();

        if (name == "nextStageBtn")
            btn.onClick.AddListener(GameManager.Instance.GoToNextStage);
        else if (name == "toTitleBtn")
            btn.onClick.AddListener(GameManager.Instance.GoToTitleFromClear);
    }

    private void EnsureStarsReady()
    {
        CacheStarImages();
        EnsureStarFillFrames();
        ApplyStarFlipX();
    }

    private void FillStars(int starCount)
    {
        if (starAnimators == null || starAnimators.Length == 0)
        {
            Debug.LogWarning("[ClearPanelUI] starAnimators 없음 → Inspector에 Star_0~2 Animator 연결 필요");
            return;
        }

        if (_starFillRoutine != null)
            StopCoroutine(_starFillRoutine);
        _starFillRoutine = StartCoroutine(RevealStarsSequential(starCount));
    }

    private IEnumerator RevealStarsSequential(int starCount)
    {
        bool hasFrames = starFillFrames != null && starFillFrames.Length >= 2;
        int fillCount = Mathf.Clamp(starCount, 0, starAnimators.Length);

        // 모든 별 초기화 (애니메이터 OFF, 스프라이트는 있을 때만)
        for (int i = 0; i < starAnimators.Length; i++)
        {
            if (starAnimators[i] != null)
                starAnimators[i].enabled = false;
            if (hasFrames && starImages != null && i < starImages.Length && starImages[i] != null)
                starImages[i].sprite = starFillFrames[0];
        }

        // 왼쪽 → 오른쪽 순차 재생
        for (int i = 0; i < fillCount; i++)
        {
            if (hasFrames && starImages != null && i < starImages.Length && starImages[i] != null)
                starImages[i].sprite = starFillFrames[starFillFrames.Length - 1];

            if (starAnimators[i] != null)
            {
                starAnimators[i].enabled = true;
                starAnimators[i].Play("StarFilled", 0, 0f);
            }

            yield return new WaitForSeconds(StarRevealInterval);
        }

        Debug.Log($"[ClearPanelUI] 별 표시 완료 — 채움: {fillCount}/{starAnimators.Length}");

        // 3스타 달성 시: 잠시 후 채워진 이미지 번쩍번쩍
        if (fillCount >= starAnimators.Length)
        {
            yield return new WaitForSeconds(0.2f);

            for (int flash = 0; flash < FlashCount; flash++)
            {
                // 꺼짐
                for (int i = 0; i < starAnimators.Length; i++)
                    if (starAnimators[i] != null) starAnimators[i].gameObject.SetActive(false);
                yield return new WaitForSeconds(FlashOffDuration);

                // 켜짐
                for (int i = 0; i < starAnimators.Length; i++)
                    if (starAnimators[i] != null) starAnimators[i].gameObject.SetActive(true);
                yield return new WaitForSeconds(FlashOnDuration);
            }
        }
    }

    private void DisableDecorRaycasts()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i].GetComponent<Button>() == null)
                graphics[i].raycastTarget = false;
        }

        if (breakdownText != null)
            breakdownText.raycastTarget = false;

        if (starImages != null)
        {
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                    starImages[i].raycastTarget = false;
            }
        }

        Transform clearImg = transform.Find("ClearTxtImage");
        if (clearImg != null)
        {
            Image img = clearImg.GetComponent<Image>();
            if (img != null)
                img.raycastTarget = false;
        }

        Transform overlay = transform.Find("Overray");
        if (overlay != null)
        {
            Image img = overlay.GetComponent<Image>();
            if (img != null)
                img.raycastTarget = false;
        }
    }

    private void CacheStarImages()
    {
        Transform starRoot = transform.Find("StarFilled");
        if (starRoot == null)
            return;

        starFilledRootAnimator = starRoot.GetComponent<Animator>();
        if (starFilledRootAnimator != null)
            starFilledRootAnimator.enabled = false;

        starImages = new Image[StarNames.Length];
        starAnimators = new Animator[StarNames.Length];

        for (int i = 0; i < StarNames.Length; i++)
        {
            Transform star = starRoot.Find(StarNames[i]);
            if (star == null)
                continue;

            starImages[i] = star.GetComponent<Image>();
            Animator anim = star.GetComponent<Animator>();
            starAnimators[i] = anim;
            if (anim != null)
                anim.enabled = false;
        }
    }

    private void EnsureStarFillFrames()
    {
        if (starFillFrames != null && starFillFrames.Length >= 2)
            return;

        if (starImages == null || starImages.Length == 0 || starImages[0] == null)
            return;

        Sprite empty = starImages[0].sprite;
        if (empty == null)
            return;

        starFillFrames = new Sprite[] { empty };
    }

    private void ApplyStarFlipX()
    {
        if (!flipStarScaleX || starImages == null)
            return;

        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null)
                continue;

            Transform star = starImages[i].transform;
            Vector3 scale = star.localScale;
            scale.x = -Mathf.Abs(scale.x);
            star.localScale = scale;
        }
    }
}
