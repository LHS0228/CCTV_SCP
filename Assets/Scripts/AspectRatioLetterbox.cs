using UnityEngine;

/// <summary>
/// Camera viewport를 목표 화면비로 제한해 빌드 해상도 차이에서 생기는 화면 늘어남을 막는 책임을 가진다.
/// </summary>
[RequireComponent(typeof(Camera))]
public sealed class AspectRatioLetterbox : MonoBehaviour
{
    public float targetAspect = 16f / 9f;

    private Camera targetCamera;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;
    private float lastTargetAspect = -1f;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        ApplyLetterbox(true);
    }

    private void Update()
    {
        ApplyLetterbox(false);
    }

    private void OnDisable()
    {
        if (targetCamera != null)
        {
            targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
        }
    }

    private void ApplyLetterbox(bool force)
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
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

        if (!IsValidDimension(screenWidth) ||
            !IsValidDimension(screenHeight) ||
            !IsValidAspect(targetAspect))
        {
            targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
            return;
        }

        float windowAspect = (float)screenWidth / screenHeight;

        if (!IsValidAspect(windowAspect))
        {
            targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
            return;
        }

        if (windowAspect > targetAspect)
        {
            float width = targetAspect / windowAspect;
            float x = (1f - width) * 0.5f;
            targetCamera.rect = new Rect(x, 0f, width, 1f);
            return;
        }

        float height = windowAspect / targetAspect;
        float y = (1f - height) * 0.5f;
        targetCamera.rect = new Rect(0f, y, 1f, height);
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
