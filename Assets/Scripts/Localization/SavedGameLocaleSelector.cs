using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[Serializable]
[UnityEngine.Scripting.Preserve]
public sealed class SavedGameLocaleSelector : IStartupLocaleSelector
{
    public Locale GetStartupLocale(ILocalesProvider availableLocales)
    {
        string code = PlayerPrefs.GetString(GameLocalization.PreferenceKey, "ko");
        if (Array.IndexOf(GameLocalization.LanguageCodes, code) < 0)
            code = "ko";
        return availableLocales.GetLocale(new LocaleIdentifier(code));
    }
}
