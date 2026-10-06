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
        ApplyTranslatedLayout(korean);
        target.text = value;
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
