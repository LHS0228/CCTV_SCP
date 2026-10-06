using UnityEngine;
using UnityEngine.UI;

/// <summary>Uses translated text for non-English labels baked into the original button art.</summary>
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
        bool translated = GameLocalization.CurrentCode != "ko" && GameLocalization.CurrentCode != "en";
        originalImage.enabled = !translated;
        if (translatedContent != null)
            translatedContent.SetActive(translated);
    }
}
