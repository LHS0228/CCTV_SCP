using UnityEngine;
using UnityEngine.UI;

/// <summary>Uses native text for Japanese/Chinese labels baked into the original English button art.</summary>
[RequireComponent(typeof(Image))]
[DisallowMultipleComponent]
public sealed class LocalizedSpriteButton : MonoBehaviour
{
    [SerializeField] private GameObject translatedContent;
    private Image originalImage;

    public void Configure(GameObject content) => translatedContent = content;

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;
        originalImage = GetComponent<Image>();
        GameLocalization.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable() => GameLocalization.LanguageChanged -= Refresh;

    private void Refresh()
    {
        bool translated = GameLocalization.CurrentCode == "ja" || GameLocalization.CurrentCode == "zh-CN"
            || GameLocalization.CurrentCode == "zh-TW";
        originalImage.enabled = !translated;
        if (translatedContent != null)
            translatedContent.SetActive(translated);
    }
}
