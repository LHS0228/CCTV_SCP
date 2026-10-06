using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

/// <summary>Central language selection, bundled translations and dynamic text formatting.</summary>
public static class GameLocalization
{
    public const string TableName = "GameText";
    public const string PreferenceKey = "CCTV_SCP.Language";
    public static readonly string[] LanguageCodes = { "ko", "en", "ja", "zh-CN", "zh-TW", "fr", "de", "ru" };
    public static readonly string[] LanguageNames = { "한국어", "English", "日本語", "简体中文", "繁體中文", "Français", "Deutsch", "Русский" };
    public static event Action LanguageChanged;

    private static readonly Dictionary<string, StringTable> tables = new Dictionary<string, StringTable>();
    private static readonly Dictionary<string, TMP_FontAsset> fonts = new Dictionary<string, TMP_FontAsset>();
    private static readonly HashSet<string> missingKeys = new HashSet<string>();
    private static bool initialized;
    private static string currentCode = "ko";
    public static string CurrentCode { get { Initialize(); return currentCode; } }
    public static int CurrentIndex => Array.IndexOf(LanguageCodes, CurrentCode);
    public static string TablePath(string code) => "Localization/Tables/GameText_" + code;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        if (initialized && LocalizationSettings.HasSettings)
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        initialized = false;
        currentCode = "ko";
        tables.Clear();
        fonts.Clear();
        missingKeys.Clear();
        LanguageChanged = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        if (initialized)
            return;
        initialized = true;
        currentCode = PlayerPrefs.GetString(PreferenceKey, "ko");
        if (Array.IndexOf(LanguageCodes, currentCode) < 0)
            currentCode = "ko";

        if (LocalizationSettings.HasSettings)
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(currentCode));
            if (locale != null)
                LocalizationSettings.SelectedLocale = locale;
        }
    }

    public static void SetLanguage(int index)
    {
        Initialize();
        if (index < 0 || index >= LanguageCodes.Length)
            return;
        ApplyLanguage(LanguageCodes[index]);
        Locale locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(currentCode));
        if (locale != null)
            LocalizationSettings.SelectedLocale = locale;
    }

    private static void OnLocaleChanged(Locale locale)
    {
        if (locale != null && Array.IndexOf(LanguageCodes, locale.Identifier.Code) >= 0)
            ApplyLanguage(locale.Identifier.Code);
    }

    private static void ApplyLanguage(string code)
    {
        if (currentCode == code)
            return;
        currentCode = code;
        PlayerPrefs.SetString(PreferenceKey, code);
        PlayerPrefs.Save();
        LanguageChanged?.Invoke();
    }

    public static StringTable LoadTable(string code)
    {
        if (!tables.TryGetValue(code, out StringTable table) || table == null)
        {
            table = Resources.Load<StringTable>(TablePath(code));
            tables[code] = table;
        }
        return table;
    }

    public static string Get(string key, params object[] arguments)
    {
        string value = LoadTable(CurrentCode)?.GetEntry(key)?.LocalizedValue;
        if (string.IsNullOrEmpty(value))
            value = LoadTable("ko")?.GetEntry(key)?.LocalizedValue;
        if (string.IsNullOrEmpty(value))
        {
            if (missingKeys.Add(key))
                Debug.LogWarning($"Missing localization entry: {key}");
            return key;
        }
        if (arguments == null || arguments.Length == 0)
            return value;
        try
        {
            return string.Format(CultureInfo.InvariantCulture, value, arguments);
        }
        catch (FormatException error)
        {
            Debug.LogError($"Invalid localization format for {key}: {error.Message}");
            return value;
        }
    }

    public static TMP_FontAsset GetFont(string code)
    {
        string name = code == "ja" ? "Japanese" : code == "zh-CN" ? "Chinese"
            : code == "zh-TW" ? "TraditionalChinese"
            : code == "fr" || code == "de" || code == "ru" ? "Western" : "English";
        if (!fonts.TryGetValue(name, out TMP_FontAsset font) || font == null)
        {
            font = Resources.Load<TMP_FontAsset>("Localization/Fonts/" + name);
            fonts[name] = font;
        }
        return font;
    }

    public static void SetText(TMP_Text target, string key, params object[] arguments)
    {
        if (target == null)
            return;
        LocalizedTMPText component = target.GetComponent<LocalizedTMPText>();
        if (component == null)
            component = target.gameObject.AddComponent<LocalizedTMPText>();
        component.SetReference(key, arguments);
    }
}
