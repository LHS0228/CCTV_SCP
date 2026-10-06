using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Dropdown))]
public sealed class LanguageSelectorUI : MonoBehaviour
{
    private TMP_Dropdown dropdown;
    private MenuDialog translationNotice;
    private Coroutine pendingNotice;

    private void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string>(GameLocalization.LanguageNames));
        dropdown.onValueChanged.AddListener(OnUserSelectedLanguage);
    }

    private void OnEnable()
    {
        if (dropdown == null)
            dropdown = GetComponent<TMP_Dropdown>();
        GameLocalization.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        GameLocalization.LanguageChanged -= Refresh;
        if (pendingNotice != null)
        {
            StopCoroutine(pendingNotice);
            pendingNotice = null;
        }
        if (translationNotice != null)
            translationNotice.Cancel();
    }
    private void OnDestroy()
    {
        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(OnUserSelectedLanguage);
        if (translationNotice != null)
            Destroy(translationNotice.gameObject);
    }

    private void OnUserSelectedLanguage(int index)
    {
        if (index < 0 || index >= GameLocalization.LanguageCodes.Length || index == GameLocalization.CurrentIndex)
            return;
        if (pendingNotice != null)
        {
            StopCoroutine(pendingNotice);
            pendingNotice = null;
        }
        if (translationNotice != null)
            translationNotice.Cancel();
        GameLocalization.SetLanguage(index);
        if (GameLocalization.CurrentCode != "ko")
            pendingNotice = StartCoroutine(ShowNoticeAfterDropdownCloses(index));
    }

    private IEnumerator ShowNoticeAfterDropdownCloses(int index)
    {
        // TMP_Dropdown.Hide restores selection after invoking onValueChanged.
        yield return null;
        pendingNotice = null;
        if (GameLocalization.CurrentIndex != index || GameLocalization.CurrentCode == "ko")
            yield break;
        if (translationNotice == null)
            translationNotice = MenuDialog.Create();
        translationNotice?.ShowTranslationNotice();
    }

    private void Refresh() => dropdown.SetValueWithoutNotify(GameLocalization.CurrentIndex);
}
