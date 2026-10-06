using TMPro;
using UnityEngine;

/// <summary>Flows translated rules into the paper instead of shrinking fixed sections independently.</summary>
[DisallowMultipleComponent]
public sealed class ManualRulesLocalizationLayout : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text[] sections;
    [SerializeField] private TMP_Text protocolCaption;
    [SerializeField] private TMP_Text protocolNumber;

    private struct OriginalLayout
    {
        public Vector2 size, position;
        public Vector3 scale;
        public TextAlignmentOptions alignment;
        public OriginalLayout(TMP_Text text)
        {
            size = text.rectTransform.sizeDelta;
            position = text.rectTransform.anchoredPosition;
            scale = text.rectTransform.localScale;
            alignment = text.alignment;
        }
        public void Restore(TMP_Text text)
        {
            text.rectTransform.sizeDelta = size;
            text.rectTransform.anchoredPosition = position;
            text.rectTransform.localScale = scale;
            text.alignment = alignment;
        }
    }

    private OriginalLayout titleLayout, captionLayout, numberLayout;
    private OriginalLayout[] sectionLayouts;
    private bool pending = true;

    public bool Configure(TMP_Text heading, TMP_Text objective, TMP_Text management,
        TMP_Text emergency, TMP_Text caption, TMP_Text number)
    {
        if (title == heading && protocolCaption == caption && protocolNumber == number
            && sections != null && sections.Length == 3 && sections[0] == objective
            && sections[1] == management && sections[2] == emergency)
            return false;
        title = heading;
        sections = new[] { objective, management, emergency };
        protocolCaption = caption;
        protocolNumber = number;
        return true;
    }

    private void Awake()
    {
        if (title == null || protocolCaption == null || protocolNumber == null
            || sections == null || sections.Length != 3)
            return;
        titleLayout = new OriginalLayout(title);
        captionLayout = new OriginalLayout(protocolCaption);
        numberLayout = new OriginalLayout(protocolNumber);
        sectionLayouts = new OriginalLayout[sections.Length];
        for (int i = 0; i < sections.Length; i++)
            sectionLayouts[i] = new OriginalLayout(sections[i]);
    }
    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;
        GameLocalization.LanguageChanged += RequestLayout;
        pending = true;
    }
    private void OnDisable() => GameLocalization.LanguageChanged -= RequestLayout;
    private void RequestLayout() => pending = true;
    private void LateUpdate()
    {
        // LocalizedTMPText has refreshed every sibling before we measure the new language.
        if (pending)
            Reflow();
    }

    public void Reflow()
    {
        pending = false;
        if (sectionLayouts == null)
            return;
        titleLayout.Restore(title);
        captionLayout.Restore(protocolCaption);
        numberLayout.Restore(protocolNumber);
        for (int i = 0; i < sections.Length; i++)
            sectionLayouts[i].Restore(sections[i]);
        string language = GameLocalization.CurrentCode;
        if (language == "ko")
            return;

        // Keep the back arrow outside the heading; give the code its own reserved area.
        Place(title, 0.06f, titleLayout.position.y, 0.87f, titleLayout.size.y);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        Place(protocolCaption, -0.15f, captionLayout.position.y, 0.68f, 0.12f);
        protocolCaption.alignment = TextAlignmentOptions.Right;
        protocolCaption.textWrappingMode = TextWrappingModes.NoWrap;
        protocolCaption.fontSizeMax = language == "en" || language == "ja" ? 0.042f : 0.06f;
        protocolCaption.fontSizeMin = 0.03f;
        protocolCaption.fontSize = protocolCaption.fontSizeMax;
        Place(protocolNumber, 0.34f, numberLayout.position.y, 0.28f, numberLayout.size.y);
        protocolNumber.alignment = TextAlignmentOptions.Left;

        const float top = 0.47f, bottom = -0.51f, gap = 0.018f, padding = 0.016f;
        const float desiredSize = 0.035f;
        float available = top - bottom - gap * (sections.Length - 1);
        foreach (TMP_Text section in sections)
        {
            section.enableAutoSizing = false;
            section.alignment = TextAlignmentOptions.TopLeft;
            section.textWrappingMode = TextWrappingModes.Normal;
        }

        // Use one shared size, choosing the largest that fits the complete document.
        float low = desiredSize * 0.75f, high = desiredSize, size = high;
        if (RequiredHeight(high, padding) > available)
        {
            for (int iteration = 0; iteration < 12; iteration++)
            {
                float candidate = (low + high) * 0.5f;
                if (RequiredHeight(candidate, padding) <= available)
                    low = candidate;
                else
                    high = candidate;
            }
            size = low;
        }
        float y = top;
        for (int i = 0; i < sections.Length; i++)
        {
            TMP_Text section = sections[i];
            section.fontSize = size;
            float height = section.GetPreferredValues(section.text, 1f, float.PositiveInfinity).y + padding;
            float scaledHeight = height * Mathf.Abs(section.rectTransform.localScale.y);
            Place(section, 0f, y - scaledHeight * (1f - section.rectTransform.pivot.y), 1f, height);
            y -= scaledHeight + gap;
        }
    }
    private float RequiredHeight(float size, float padding)
    {
        float height = 0f;
        foreach (TMP_Text section in sections)
        {
            section.fontSize = size;
            height += (section.GetPreferredValues(section.text, 1f, float.PositiveInfinity).y + padding)
                * Mathf.Abs(section.rectTransform.localScale.y);
        }
        return height;
    }
    private static void Place(TMP_Text text, float x, float y, float width, float height)
    {
        text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        text.rectTransform.anchoredPosition = new Vector2(x, y);
    }
}
