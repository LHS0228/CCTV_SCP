using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overlay Canvas의 UI를 카메라 레터박스와 같은 목표 화면비 영역 안에 배치하는 책임을 가진다.
/// </summary>
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasScaler))]
public sealed class OverlayLetterboxSafeArea : MonoBehaviour
{
    public float targetAspect = 16f / 9f;
    public Vector2 referenceResolution = new Vector2(1920f, 1080f);

    private const string SafeAreaName = "LetterboxSafeArea";

    private RectTransform safeAreaRect;
    private CanvasScaler canvasScaler;
    private bool isRebuildingHierarchy;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;
    private float lastTargetAspect = -1f;

    private void Awake()
    {
        canvasScaler = GetComponent<CanvasScaler>();
        EnsureSafeArea();
        MoveRootChildrenIntoSafeArea();
        ApplySafeArea(true);
    }

    private void OnEnable()
    {
        ApplySafeArea(true);
    }

    private void Update()
    {
        ApplySafeArea(false);
    }

    private void OnTransformChildrenChanged()
    {
        if (isRebuildingHierarchy || !isActiveAndEnabled)
        {
            return;
        }

        EnsureSafeArea();
        MoveRootChildrenIntoSafeArea();
        ApplySafeArea(true);
    }

    private void EnsureSafeArea()
    {
        Transform existing = transform.Find(SafeAreaName);
        if (existing != null)
        {
            safeAreaRect = existing as RectTransform;
            return;
        }

        isRebuildingHierarchy = true;

        GameObject safeArea = new GameObject(SafeAreaName, typeof(RectTransform), typeof(RectMask2D));
        safeArea.transform.SetParent(transform, false);
        safeAreaRect = safeArea.GetComponent<RectTransform>();
        SetSafeAreaAnchors(Vector2.zero, Vector2.one);

        isRebuildingHierarchy = false;
    }

    private void MoveRootChildrenIntoSafeArea()
    {
        if (safeAreaRect == null)
        {
            return;
        }

        isRebuildingHierarchy = true;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child == safeAreaRect)
            {
                continue;
            }

            child.SetParent(safeAreaRect, false);
            child.SetAsFirstSibling();
        }

        safeAreaRect.SetAsFirstSibling();
        isRebuildingHierarchy = false;
    }

    private void ApplySafeArea(bool force)
    {
        if (safeAreaRect == null || canvasScaler == null)
        {
            return;
        }

        int screenWidth = Screen.width;
        int screenHeight = Screen.height;

        if (!force &&
            screenWidth == lastScreenWidth &&
            screenHeight == lastScreenHeight &&
            Mathf.Approximately(targetAspect, lastTargetAspect))
        {
            return;
        }

        lastScreenWidth = screenWidth;
        lastScreenHeight = screenHeight;
        lastTargetAspect = targetAspect;

        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = referenceResolution;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        if (!IsValidDimension(screenWidth) ||
            !IsValidDimension(screenHeight) ||
            !IsValidAspect(targetAspect))
        {
            canvasScaler.matchWidthOrHeight = 0f;
            SetSafeAreaAnchors(Vector2.zero, Vector2.one);
            return;
        }

        float windowAspect = (float)screenWidth / screenHeight;

        if (!IsValidAspect(windowAspect))
        {
            canvasScaler.matchWidthOrHeight = 0f;
            SetSafeAreaAnchors(Vector2.zero, Vector2.one);
            return;
        }

        if (windowAspect > targetAspect)
        {
            canvasScaler.matchWidthOrHeight = 1f;
            float width = targetAspect / windowAspect;
            float x = (1f - width) * 0.5f;
            SetSafeAreaAnchors(new Vector2(x, 0f), new Vector2(1f - x, 1f));
            return;
        }

        canvasScaler.matchWidthOrHeight = 0f;
        float height = windowAspect / targetAspect;
        float y = (1f - height) * 0.5f;
        SetSafeAreaAnchors(new Vector2(0f, y), new Vector2(1f, 1f - y));
    }

    private void SetSafeAreaAnchors(Vector2 anchorMin, Vector2 anchorMax)
    {
        safeAreaRect.anchorMin = anchorMin;
        safeAreaRect.anchorMax = anchorMax;
        safeAreaRect.offsetMin = Vector2.zero;
        safeAreaRect.offsetMax = Vector2.zero;
        safeAreaRect.localScale = Vector3.one;
    }

    private static bool IsValidDimension(int value)
    {
        return value > 0;
    }

    private static bool IsValidAspect(float value)
    {
        return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
