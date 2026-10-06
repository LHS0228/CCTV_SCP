using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Opens the language choices from one settings button and displays the current language.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class LanguageMenuButton : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private TMP_Text caption;
    private Button button;

    public TMP_Dropdown Dropdown => dropdown;
    public TMP_Text Caption => caption;

    public void Configure(TMP_Dropdown choices, TMP_Text label)
    {
        dropdown = choices;
        caption = label;
    }

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OpenChoices);
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;
        GameLocalization.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        GameLocalization.LanguageChanged -= Refresh;
        if (dropdown != null && dropdown.IsExpanded)
            dropdown.Hide();
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OpenChoices);
    }

    private void OpenChoices()
    {
        StartCoroutine(OpenChoicesAfterLayout());
    }

    private IEnumerator OpenChoicesAfterLayout()
    {
        // TMP initializes its dropdown tween in Start; a freshly opened Esc menu
        // can receive a click before that first frame has completed.
        yield return null;
        if (isActiveAndEnabled && dropdown != null && dropdown.isActiveAndEnabled)
            dropdown.Show();
    }

    private void Refresh()
    {
        if (caption != null)
            GameLocalization.SetText(caption, "options.languageSelect",
                GameLocalization.LanguageNames[GameLocalization.CurrentIndex]);
    }
}
