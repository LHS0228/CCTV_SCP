using TMPro;
using UnityEngine;

/// <summary>Keeps a TMP label and its font current when the player changes language.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class LocalizedTMPText : MonoBehaviour
{
    [SerializeField] private string key;
    [SerializeField] private TMP_Text translatedBottomBoundary;
    [SerializeField, Range(0.55f, 1f)] private float minimumTranslatedSizeRatio = 0.55f;
    public string Key => key;
    private object[] arguments;
    private TMP_Text target;
    private TMP_FontAsset originalFont;
    private Material originalMaterial;
    private float originalSize;
    private float originalMin;
    private float originalMax;
    private bool originalAutoSize;
    private FontStyles originalStyle;
    private TextWrappingModes originalWrapping;
    private Vector2 originalSizeDelta;
    private Vector2 originalAnchoredPosition;

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;
        GameLocalization.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable() => GameLocalization.LanguageChanged -= Refresh;

    public void SetReference(string entryKey, params object[] values)
    {
        key = entryKey;
        arguments = values;
        if (Application.isPlaying)
            Refresh();
    }

    public void ConfigureTranslatedLayout(TMP_Text bottomBoundary, float minimumSizeRatio)
    {
        translatedBottomBoundary = bottomBoundary;
        minimumTranslatedSizeRatio = Mathf.Clamp(minimumSizeRatio, 0.55f, 1f);
    }

    public void Refresh()
    {
        if (string.IsNullOrEmpty(key))
            return;
        if (target == null)
        {
            target = GetComponent<TMP_Text>();
            originalFont = target.font;
            originalMaterial = target.fontSharedMaterial;
            originalSize = target.fontSize;
            originalMin = target.fontSizeMin;
            originalMax = target.fontSizeMax;
            originalAutoSize = target.enableAutoSizing;
            originalStyle = target.fontStyle;
            originalWrapping = target.textWrappingMode;
            originalSizeDelta = target.rectTransform.sizeDelta;
            originalAnchoredPosition = target.rectTransform.anchoredPosition;
        }

        string value = GameLocalization.Get(key, arguments);
        // Ending captions overlap the settings panel but have no interaction of their own.
        if (key.StartsWith("ending.", System.StringComparison.Ordinal))
            target.raycastTarget = false;
        string code = GameLocalization.CurrentCode;
        bool korean = code == "ko";
        bool document = key.StartsWith("manual.", System.StringComparison.Ordinal)
            || key.StartsWith("contract.", System.StringComparison.Ordinal)
            || key.StartsWith("entity.", System.StringComparison.Ordinal)
            || key.StartsWith("report.", System.StringComparison.Ordinal);
        TMP_FontAsset font = originalFont;
        if (!korean && (document || code != "en" || originalFont == null || !originalFont.HasCharacters(value)))
            font = GameLocalization.GetFont(code);
        if (font != null)
        {
            target.font = font;
            target.fontSharedMaterial = font == originalFont ? originalMaterial : font.material;
        }
        target.fontStyle = !korean && document ? originalStyle & ~FontStyles.Bold : originalStyle;
        target.enableAutoSizing = korean ? originalAutoSize : true;
        target.fontSizeMin = korean ? originalMin : Mathf.Max(originalSize * minimumTranslatedSizeRatio, 0.001f);
        target.fontSizeMax = korean ? originalMax : (originalAutoSize ? originalMax : originalSize);
        target.fontSize = originalSize;
        target.textWrappingMode = korean ? originalWrapping : TextWrappingModes.Normal;
        bool worldDocument = originalSize < 1f && (key.StartsWith("entity.", System.StringComparison.Ordinal)
            || key.StartsWith("manual.mimic.", System.StringComparison.Ordinal)
            || key.StartsWith("manual.circuit.", System.StringComparison.Ordinal)
            || key.StartsWith("manual.tentacles.", System.StringComparison.Ordinal)
            || key.StartsWith("manual.symptoms.", System.StringComparison.Ordinal));
        if (worldDocument)
        {
            target.rectTransform.sizeDelta = originalSizeDelta;
            target.rectTransform.anchoredPosition = originalAnchoredPosition;
            if (!korean && key.EndsWith(".title", System.StringComparison.Ordinal))
            {
                // Reserve the same back-arrow space as the basic rules heading.
                target.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                    target.rectTransform.rect.width - 0.13f);
                target.rectTransform.anchoredPosition += new Vector2(0.06f, 0f);
            }
        }
        ApplyTranslatedLayout(korean);
        target.text = value;
        if (!korean && worldDocument)
            FitWorldDocument();
    }

    private void FitWorldDocument()
    {
        // TMP AutoSize rounds in 0.05-point steps. These world-space documents use
        // 0.02-0.07 points, so a single step otherwise collapses them to the minimum.
        target.enableAutoSizing = false;
        target.fontSizeMin = Mathf.Max(target.fontSizeMin,
            originalSize * (key.EndsWith(".description", System.StringComparison.Ordinal) ? 0.7f : 0.75f));
        float low = target.fontSizeMin, high = target.fontSizeMax;
        if (WorldDocumentFits(high))
            return;
        for (int iteration = 0; iteration < 12; iteration++)
        {
            float candidate = (low + high) * 0.5f;
            if (WorldDocumentFits(candidate))
                low = candidate;
            else
                high = candidate;
        }
        target.fontSize = low;
    }

    private bool WorldDocumentFits(float size)
    {
        target.fontSize = size;
        // Check the rendered layout, including rich-text sizes and line breaking.
        target.ForceMeshUpdate(true);
        Rect area = target.rectTransform.rect;
        Bounds glyphs = target.textBounds;
        const float tolerance = 0.0001f;
        return !target.isTextOverflowing
            && glyphs.min.x >= area.xMin + target.margin.x - tolerance
            && glyphs.max.x <= area.xMax - target.margin.z + tolerance;
    }

    private void ApplyTranslatedLayout(bool korean)
    {
        if (translatedBottomBoundary == null)
            return;

        RectTransform rect = target.rectTransform;
        rect.sizeDelta = originalSizeDelta;
        rect.anchoredPosition = originalAnchoredPosition;
        if (korean)
            return;

        // Leave a small gap below the title, and use the space above the agreement prompt.
        Vector3[] corners = new Vector3[4];
        translatedBottomBoundary.rectTransform.GetWorldCorners(corners);
        float boundaryTop = float.NegativeInfinity;
        foreach (Vector3 corner in corners)
            boundaryTop = Mathf.Max(boundaryTop, rect.InverseTransformPoint(corner).y);
        float scale = Mathf.Abs(rect.localScale.y);
        if (scale < 0.001f)
            return;
        float oldHeight = rect.rect.height;
        const float topInset = 28f;
        const float bottomGap = 16f;
        float height = Mathf.Max(oldHeight, rect.rect.yMax - boundaryTop - (topInset + bottomGap) / scale);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        rect.anchoredPosition -= new Vector2(0f, (height - oldHeight) * (1f - rect.pivot.y) * rect.localScale.y + topInset);
    }
}
