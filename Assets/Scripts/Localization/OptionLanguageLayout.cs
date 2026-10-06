using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Fits the added language row and existing settings inside the same side panel.</summary>
[RequireComponent(typeof(RectTransform))]
public sealed class OptionLanguageLayout : MonoBehaviour
{
    private bool arranging;
    private void OnEnable() => Reflow();
    private void OnRectTransformDimensionsChange() => Reflow();

    public void Reflow()
    {
        if (arranging)
            return;
        arranging = true;
        try
        {
            RectTransform panel = GetComponent<RectTransform>();
            LocalizedTMPText[] labels = GetComponentsInChildren<LocalizedTMPText>(true);
            TMP_Dropdown language = GetComponentsInChildren<LanguageSelectorUI>(true)
                .Select(selector => selector.GetComponent<TMP_Dropdown>()).FirstOrDefault();
            if (language == null)
                return;
            TMP_Dropdown resolution = GetComponentsInChildren<TMP_Dropdown>(true)
                .FirstOrDefault(dropdown => dropdown != language && dropdown.name == "ResolutionDropdown");
            Toggle fullScreen = GetComponentsInChildren<Toggle>(true)
                .FirstOrDefault(toggle => toggle.name == "FullScreenToggle");
            var sliders = new List<KeyValuePair<string, Slider>>();
            string[] sliderNames = { "MasterSoundSlider", "BGMSoundSlider", "SFXSoundSlider", "ScreenTurnSpeedSlider" };
            string[] keys = { "options.master", "options.music", "options.sfx", "options.sensitivity" };
            for (int i = 0; i < sliderNames.Length; i++)
            {
                Slider slider = GetComponentsInChildren<Slider>(true).FirstOrDefault(item => item.name == sliderNames[i]);
                if (slider != null)
                    sliders.Add(new KeyValuePair<string, Slider>(keys[i], slider));
            }
            float height = panel.rect.height > 1 ? panel.rect.height : 1080f;
            int rowCount = 1 + sliders.Count + (resolution == null ? 0 : 1) + (fullScreen == null ? 0 : 1);
            float spacing = Mathf.Max(74f, (height - 155f) / rowCount);
            float y = -125f;
            Place(labels.FirstOrDefault(label => label.Key == "menu.options")?.transform as RectTransform,
                -55f, 380f, 85f);
            Place(labels.FirstOrDefault(label => label.Key == "options.language")?.transform as RectTransform,
                y, 370f, 34f);
            Place(language.transform as RectTransform, y - 43f, 370f, 50f);
            LanguageMenuButton languageButton = GetComponentInChildren<LanguageMenuButton>(true);
            if (languageButton != null)
                Place(languageButton.transform as RectTransform, y - 43f, 370f, 60f);
            y -= spacing;
            if (resolution != null)
            {
                Place(labels.FirstOrDefault(label => label.Key == "options.resolution")?.transform as RectTransform,
                    y, 370f, 34f);
                Place(resolution.transform as RectTransform, y - 43f, 370f, 50f);
                y -= spacing;
            }
            if (fullScreen != null)
            {
                Place(fullScreen.transform as RectTransform, y - 28f, 370f, 40f);
                y -= spacing;
            }
            foreach (KeyValuePair<string, Slider> pair in sliders)
            {
                Place(labels.FirstOrDefault(label => label.Key == pair.Key)?.transform as RectTransform,
                    y, 370f, 34f);
                Place(pair.Value.transform as RectTransform, y - 43f, 370f, 40f);
                y -= spacing;
            }
        }
        finally { arranging = false; }
    }

    private static void Place(RectTransform rect, float y, float width, float height)
    {
        if (rect == null)
            return;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(width, height);
    }
}
