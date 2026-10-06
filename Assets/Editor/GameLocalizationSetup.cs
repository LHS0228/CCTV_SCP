using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

/// <summary>Builds and validates the translation assets with the project's installed Unity editor.</summary>
public static class GameLocalizationSetup
{
    private const string CatalogPath = "Assets/Localization/translations.json";
    private const string ResourceRoot = "Assets/Resources/Localization";
    [Serializable] private sealed class Catalog { public Entry[] entries; public string[] literals; }
    [Serializable] private sealed class Entry
    {
        public string key, ko, en, ja, zhCN, zhTW, fr, de, ru;
        public string[] sources;
        public string Value(string code) => code == "en" ? en : code == "ja" ? ja
            : code == "zh-CN" ? zhCN : code == "zh-TW" ? zhTW
            : code == "fr" ? fr : code == "de" ? de : code == "ru" ? ru : ko;
    }

    [MenuItem("Tools/Localization/Apply Translation Assets Only")]
    public static void ApplyAssetsOnly()
    {
        Catalog catalog = ReadCatalog();
        CreateSettingsAndTables(catalog);
        CreateFonts(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log($"LOCALIZATION_ASSETS_APPLIED: {catalog.entries.Length} keys, {GameLocalization.LanguageCodes.Length} languages.");
    }

    [MenuItem("Tools/Localization/Apply Translation Catalog")]
    public static void Apply()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        SceneSetup[] sceneSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Catalog catalog = ReadCatalog();
            CreateSettingsAndTables(catalog);
            CreateFonts(catalog);
            var sourceKeys = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Entry entry in catalog.entries)
                foreach (string source in entry.sources)
                    sourceKeys[source] = entry.key;

            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            string[] prefabs = AssetDatabase.GetDependencies(scenes, true)
                .Where(path => path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.Ordinal).ToArray();
            int bound = 0;
            foreach (string path in prefabs)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    bool changed = Configure(root, sourceKeys, ref bound);
                    if (changed)
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }

            foreach (string path in scenes)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool changed = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                    changed |= Configure(root, sourceKeys, ref bound);
                if (changed)
                    EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"LOCALIZATION_APPLIED: {catalog.entries.Length} keys, {GameLocalization.LanguageCodes.Length} languages, {bound} text bindings.");
            ValidateCatalogAndScenes();
        }
        finally
        {
            if (!Application.isBatchMode)
                EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
        }
    }

    private static Catalog ReadCatalog()
    {
        Catalog catalog = JsonUtility.FromJson<Catalog>(File.ReadAllText(CatalogPath));
        if (catalog?.entries == null || catalog.entries.Length == 0)
            throw new InvalidOperationException("The translation catalog is empty.");
        if (catalog.entries.GroupBy(entry => entry.key).Any(group => group.Count() > 1))
            throw new InvalidOperationException("The translation catalog contains duplicate keys.");
        foreach (Entry entry in catalog.entries)
            foreach (string code in GameLocalization.LanguageCodes)
                if (string.IsNullOrWhiteSpace(entry.Value(code)))
                    throw new InvalidOperationException($"Empty translation: {entry.key}/{code}");
        return catalog;
    }

    private static void CreateSettingsAndTables(Catalog catalog)
    {
        EnsureFolder(ResourceRoot + "/Tables");
        EnsureFolder(ResourceRoot + "/Locales");
        EnsureFolder("Assets/Localization/Tables");
        string settingsPath = ResourceRoot + "/Settings.asset";
        LocalizationSettings settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(settingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<LocalizationSettings>();
            AssetDatabase.CreateAsset(settings, settingsPath);
        }
        LocalizationEditorSettings.ActiveLocalizationSettings = settings;
        var provider = new ResourceLocalesProvider();
        var locales = new List<Locale>();
        for (int i = 0; i < GameLocalization.LanguageCodes.Length; i++)
        {
            string code = GameLocalization.LanguageCodes[i];
            string path = ResourceRoot + "/Locales/" + code + ".asset";
            Locale locale = AssetDatabase.LoadAssetAtPath<Locale>(path);
            if (locale == null)
            {
                locale = Locale.CreateLocale(code);
                AssetDatabase.CreateAsset(locale, path);
            }
            locale.LocaleName = GameLocalization.LanguageNames[i];
            EditorUtility.SetDirty(locale);
            locales.Add(locale);
            provider.AddLocale(locale);
        }
        settings.SetAvailableLocales(provider);
        settings.GetStartupLocaleSelectors().Clear();
        settings.GetStartupLocaleSelectors().Add(new SavedGameLocaleSelector());
        settings.GetStringDatabase().TableProvider = new ResourceStringTableProvider();
        EditorUtility.SetDirty(settings);

        StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(GameLocalization.TableName);
        if (collection == null)
            collection = LocalizationEditorSettings.CreateStringTableCollection(GameLocalization.TableName,
                "Assets/Localization/Tables", locales);
        foreach (Locale locale in locales)
        {
            if (!collection.StringTables.Any(table => table.LocaleIdentifier == locale.Identifier))
                collection.AddNewTable(locale.Identifier);
        }
        foreach (StringTable table in collection.StringTables)
        {
            string code = table.LocaleIdentifier.Code;
            foreach (Entry entry in catalog.entries)
                table.AddEntry(entry.key, entry.Value(code));
            string destination = ResourceRoot + "/Tables/GameText_" + code + ".asset";
            string current = AssetDatabase.GetAssetPath(table);
            if (current != destination)
            {
                string error = AssetDatabase.MoveAsset(current, destination);
                if (!string.IsNullOrEmpty(error))
                    throw new InvalidOperationException(error);
            }
            EditorUtility.SetDirty(table);
        }
        EditorUtility.SetDirty(collection.SharedData);
        EditorUtility.SetDirty(collection);
        LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null, collection);
        AssetDatabase.SaveAssets();
    }

    private static void CreateFonts(Catalog catalog)
    {
        EnsureFolder(ResourceRoot + "/Fonts");
        CreateFont("English", "Assets/Font/NanumMyeongjo-Regular.ttf", catalog, "en");
        CreateFont("Japanese", "Assets/Font/Localization/NotoSansCJKjp-Regular.otf", catalog, "ja");
        CreateFont("Chinese", "Assets/Font/Localization/NotoSansCJKsc-Regular.otf", catalog, "zh-CN");
        CreateFont("TraditionalChinese", "Assets/Font/Localization/NotoSansCJKtc-Regular.otf", catalog, "zh-TW");
        CreateFont("Western", "Assets/TextMesh Pro/Fonts/LiberationSans.ttf", catalog, "fr");
    }

    private static void CreateFont(string name, string source, Catalog catalog, string code)
    {
        string path = ResourceRoot + "/Fonts/" + name + ".asset";
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(source);
        if (sourceFont == null)
            throw new InvalidOperationException("Missing source font: " + source);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        // Rebuild from the source so an editor restart or changed catalog cannot leave stale glyph data.
        TMP_FontAsset generated = TMP_FontAsset.CreateFontAsset(sourceFont, 64, 6, GlyphRenderMode.SDFAA, 2048, 2048,
            AtlasPopulationMode.Dynamic, true);
        if (generated == null)
            throw new InvalidOperationException("Could not create font: " + name);
        string characters = string.Concat(catalog.entries.Select(entry => PlainText(entry.Value(code)) + PlainText(entry.ko)))
            + string.Concat(GameLocalization.LanguageNames) + "0123456789";
        if (code == "en")
            characters = string.Concat(catalog.entries.Select(entry => PlainText(entry.en) + PlainText(entry.ko))) + "한국어0123456789";
        if (name == "Western")
            characters = string.Concat(catalog.entries.Select(entry => PlainText(entry.fr) + PlainText(entry.de) + PlainText(entry.ru)))
                + "FrançaisDeutschРусский0123456789";
        uint[] unicodes = characters.Where(c => !char.IsControl(c)).Select(c => (uint)c).Distinct().ToArray();
        generated.TryAddCharacters(unicodes, out uint[] missing);
        if (missing != null && missing.Length > 0)
            throw new InvalidOperationException($"{name}: Missing font glyphs: {string.Join(",", missing.Select(c => $"U+{c:X4}"))}");
        generated.atlasPopulationMode = AtlasPopulationMode.Static;
        if (font == null)
        {
            font = generated;
            font.name = name;
            AssetDatabase.CreateAsset(font, path);
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (Texture2D atlas in font.atlasTextures)
                AssetDatabase.AddObjectToAsset(atlas, font);
        }
        else
        {
            Material material = font.material;
            Texture2D[] oldAtlases = font.atlasTextures;
            Texture2D[] newAtlases = generated.atlasTextures;
            var stableAtlases = new Texture2D[newAtlases.Length];
            for (int i = 0; i < newAtlases.Length; i++)
            {
                if (i < oldAtlases.Length && oldAtlases[i] != null)
                {
                    EditorUtility.CopySerialized(newAtlases[i], oldAtlases[i]);
                    stableAtlases[i] = oldAtlases[i];
                }
                else
                {
                    stableAtlases[i] = newAtlases[i];
                    AssetDatabase.AddObjectToAsset(stableAtlases[i], font);
                }
            }
            EditorUtility.CopySerialized(generated.material, material);
            EditorUtility.CopySerialized(generated, font);
            font.name = name;
            font.material = material;
            font.atlasTextures = stableAtlases;
            font.material.SetTexture(ShaderUtilities.ID_MainTex, stableAtlases[0]);
            for (int i = newAtlases.Length; i < oldAtlases.Length; i++)
                UnityEngine.Object.DestroyImmediate(oldAtlases[i], true);
            UnityEngine.Object.DestroyImmediate(generated);
        }
        var serializedFont = new SerializedObject(font);
        serializedFont.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false;
        serializedFont.ApplyModifiedPropertiesWithoutUndo();
        foreach (Texture2D atlas in font.atlasTextures)
        {
            if (!AssetDatabase.Contains(atlas))
                AssetDatabase.AddObjectToAsset(atlas, font);
            EditorUtility.SetDirty(atlas);
        }
        EditorUtility.SetDirty(font);
        EditorUtility.SetDirty(font.material);
        AssetDatabase.SaveAssets();
    }

    public static bool ConfigureManualRules(GameObject root)
    {
        bool changed = false;
        foreach (Transform page in root.GetComponentsInChildren<Transform>(true)
                     .Where(item => item.name == "DetailsPage[Base Rule]"))
        {
            // The prefab contains prototype text; the build scene overrides it with the actual
            // rules and translation keys. Bind the stable hierarchy so both use the same layout.
            TMP_Text heading = page.Find("Title")?.GetComponent<TMP_Text>();
            TMP_Text objective = page.Find("Text1")?.GetComponent<TMP_Text>();
            TMP_Text management = page.Find("Text1 (1)")?.GetComponent<TMP_Text>();
            TMP_Text emergency = page.Find("Text1 (2)")?.GetComponent<TMP_Text>();
            TMP_Text caption = page.Find("hangle")?.GetComponent<TMP_Text>();
            TMP_Text number = page.Find("Number")?.GetComponent<TMP_Text>();
            if (heading == null || objective == null || management == null || emergency == null || caption == null || number == null)
                throw new InvalidOperationException("Incomplete Basic Management Rules page: " + page.name);
            ManualRulesLocalizationLayout layout = page.GetComponent<ManualRulesLocalizationLayout>();
            if (layout == null)
                layout = page.gameObject.AddComponent<ManualRulesLocalizationLayout>();
            if (layout.Configure(heading, objective, management, emergency, caption, number))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(layout);
                EditorUtility.SetDirty(layout);
                changed = true;
            }
        }
        return changed;
    }

    [MenuItem("Tools/Localization/Apply Manual Rules Layout")]
    public static void ApplyManualRules()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Apply the manual layout in Edit Mode.");
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        if (setup.Any(scene => SceneManager.GetSceneByPath(scene.path).isDirty))
            throw new InvalidOperationException("Save modified scenes before applying the manual layout.");
        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        try
        {
            foreach (string path in AssetDatabase.GetDependencies(scenes, true)
                         .Where(path => path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)).OrderBy(path => path))
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (ConfigureManualRules(root))
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            foreach (string path in scenes)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool changed = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                    changed |= ConfigureManualRules(root);
                if (changed)
                    EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("MANUAL_RULES_LAYOUT_APPLIED");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    private static bool Configure(GameObject root, Dictionary<string, string> sourceKeys, ref int bound)
    {
        bool changed = false;
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.GetComponentInParent<LanguageSelectorUI>(true) != null)
                continue;
            if (!sourceKeys.TryGetValue(text.text ?? "", out string key))
                continue;
            LocalizedTMPText localized = text.GetComponent<LocalizedTMPText>();
            if (localized != null && localized.Key == key)
                continue;
            if (localized == null)
                localized = text.gameObject.AddComponent<LocalizedTMPText>();
            localized.SetReference(key);
            PrefabUtility.RecordPrefabInstancePropertyModifications(localized);
            EditorUtility.SetDirty(localized);
            changed = true;
            bound++;
        }

        changed |= ConfigureManualRules(root);

        foreach (LocalizedTMPText body in root.GetComponentsInChildren<LocalizedTMPText>(true)
                     .Where(label => label.Key == "contract.terms"))
        {
            LocalizedTMPText agreement = body.transform.parent.GetComponentsInChildren<LocalizedTMPText>(true)
                .FirstOrDefault(label => label.Key == "contract.acceptQuestion");
            if (agreement == null)
                continue;
            var serializedBody = new SerializedObject(body);
            if (serializedBody.FindProperty("translatedBottomBoundary").objectReferenceValue == agreement.GetComponent<TMP_Text>()
                && Mathf.Approximately(serializedBody.FindProperty("minimumTranslatedSizeRatio").floatValue, 0.75f))
                continue;
            body.ConfigureTranslatedLayout(agreement.GetComponent<TMP_Text>(), 0.75f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(body);
            EditorUtility.SetDirty(body);
            changed = true;
        }

        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            if ((transform.name == "OptionMenu" || transform.name == "Ingame_OptionMenu")
                && transform.GetComponent<RectTransform>() != null)
            {
                RectTransform panel = transform.GetComponentsInChildren<RectTransform>(true)
                    .FirstOrDefault(rect => rect.name == "Menu") ?? (RectTransform)transform;
                LanguageSelectorUI selector = transform.GetComponentInChildren<LanguageSelectorUI>(true);
                if (selector == null)
                {
                    CreateLanguageSelector(panel);
                    selector = panel.GetComponentInChildren<LanguageSelectorUI>(true);
                    changed = true;
                }
                RectTransform dropdownRect = (RectTransform)selector.transform;
                RectTransform labelRect = transform.GetComponentsInChildren<RectTransform>(true)
                    .FirstOrDefault(rect => rect.name == "LanguageLabel");
                if (dropdownRect.parent != panel)
                {
                    dropdownRect.SetParent(panel, false);
                    if (labelRect != null)
                        labelRect.SetParent(panel, false);
                    changed = true;
                }
                if (panel.GetComponent<OptionLanguageLayout>() == null)
                {
                    TMP_Dropdown resolution = panel.GetComponentsInChildren<TMP_Dropdown>(true)
                        .FirstOrDefault(dropdown => dropdown.name == "ResolutionDropdown");
                    if (resolution != null && panel.Find("ResolutionLabel") == null)
                        CreateSettingLabel(panel, "ResolutionLabel", "options.resolution", "해상도");
                    foreach (LocalizedTMPText label in panel.GetComponentsInChildren<LocalizedTMPText>(true))
                    {
                        if (!label.Key.StartsWith("options.", StringComparison.Ordinal))
                            continue;
                        TMP_Text text = label.GetComponent<TMP_Text>();
                        text.fontSize = 30;
                        text.fontSizeMin = 16;
                        text.fontSizeMax = 30;
                        text.enableAutoSizing = true;
                    }
                    panel.gameObject.AddComponent<OptionLanguageLayout>().Reflow();
                    changed = true;
                }
                TMP_Dropdown language = selector.GetComponent<TMP_Dropdown>();
                Image arrow = language.transform.Find("Arrow")?.GetComponent<Image>();
                if (arrow != null && arrow.sprite == null)
                {
                    arrow.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
                    changed = true;
                }
                if (panel.GetComponentInChildren<LanguageMenuButton>(true) == null)
                {
                    CreateLanguageButton(panel, language);
                    changed = true;
                }
                if (labelRect != null && labelRect.gameObject.activeSelf)
                {
                    labelRect.gameObject.SetActive(false);
                    changed = true;
                }
                panel.GetComponent<OptionLanguageLayout>().Reflow();
            }
        }
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            LocalizedSpriteButton existingButton = image.GetComponent<LocalizedSpriteButton>();
            // Tablet game buttons share their original artwork in every locale.
            bool tablet = image.GetComponentInParent<TabletSceneManager>(true) != null;
            for (Transform parent = image.transform; parent != null && !tablet; parent = parent.parent)
                tablet = parent.name == "TabletCanvas";
            if (tablet)
            {
                if (existingButton != null)
                {
                    UnityEngine.Object.DestroyImmediate(existingButton);
                    changed = true;
                }
                Transform replacement = image.transform.Find("TranslatedButtonContent");
                if (replacement != null)
                {
                    UnityEngine.Object.DestroyImmediate(replacement.gameObject);
                    changed = true;
                }
                if (!image.enabled)
                {
                    image.enabled = true;
                    changed = true;
                }
                continue;
            }
            if (existingButton != null)
            {
                AngledButtonFrame frame = image.GetComponentInChildren<AngledButtonFrame>(true);
                if (frame != null && frame.GetComponent<CanvasRenderer>() == null)
                {
                    frame.gameObject.AddComponent<CanvasRenderer>();
                    changed = true;
                }
                if (frame != null && !frame.raycastTarget)
                {
                    frame.raycastTarget = true;
                    changed = true;
                }
                continue;
            }
            if (image.sprite == null)
                continue;
            string sprite = image.sprite.texture.name;
            string key = sprite == "Start_BaseMap" ? "menu.start.68"
                : sprite == "Options_BaseMap" ? "menu.options"
                : sprite == "Exit_BaseMap" ? "menu.exit" : null;
            if (key == null)
                continue;
            GameObject content = new GameObject("TranslatedButtonContent", typeof(RectTransform), typeof(CanvasRenderer), typeof(AngledButtonFrame));
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.SetParent(image.transform, false);
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = contentRect.offsetMax = Vector2.zero;
            content.GetComponent<AngledButtonFrame>().raycastTarget = true;
            GameObject captionObject = new GameObject("TranslatedCaption", typeof(RectTransform));
            RectTransform captionRect = captionObject.GetComponent<RectTransform>();
            captionRect.SetParent(contentRect, false);
            captionRect.anchorMin = Vector2.zero;
            captionRect.anchorMax = Vector2.one;
            captionRect.offsetMin = new Vector2(16f, 4f);
            captionRect.offsetMax = new Vector2(-16f, -4f);
            TextMeshProUGUI caption = captionObject.AddComponent<TextMeshProUGUI>();
            caption.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ResourceRoot + "/Fonts/English.asset");
            caption.fontSize = 42f;
            caption.alignment = TextAlignmentOptions.Center;
            caption.raycastTarget = false;
            caption.text = GameLocalization.LoadTable("ko").GetEntry(key).LocalizedValue;
            captionObject.AddComponent<LocalizedTMPText>().SetReference(key);
            image.gameObject.AddComponent<LocalizedSpriteButton>().Configure(content);
            content.SetActive(false);
            changed = true;
        }
        // The report's clipboard texture contains English contract clauses. Cover only its
        // paper area with a native UI panel; the existing translated report labels stay above it.
        if (root.GetComponentInChildren<DayEndContract>(true) != null)
        {
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == null || image.sprite.texture.name != "contract3"
                    || image.transform.Find("LocalizedReportPaper") != null)
                    continue;
                GameObject paper = new GameObject("LocalizedReportPaper", typeof(RectTransform), typeof(Image));
                RectTransform paperRect = paper.GetComponent<RectTransform>();
                paperRect.SetParent(image.transform, false);
                paperRect.SetAsFirstSibling();
                paperRect.anchorMin = new Vector2(0.06f, 0.05f);
                paperRect.anchorMax = new Vector2(0.945f, 0.88f);
                paperRect.offsetMin = paperRect.offsetMax = Vector2.zero;
                Image panel = paper.GetComponent<Image>();
                panel.color = new Color(0.9f, 0.9f, 0.89f);
                panel.raycastTarget = false;
                changed = true;
            }
        }
        return changed;
    }

    private static void CreateLanguageSelector(RectTransform parent)
    {
        GameObject dropdownObject = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
        });
        dropdownObject.name = "LanguageDropdown";
        RectTransform rect = dropdownObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(420f, 220f);
        rect.sizeDelta = new Vector2(300f, 60f);
        TMP_Dropdown dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string>(GameLocalization.LanguageNames));
        TMP_FontAsset menuFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ResourceRoot + "/Fonts/Japanese.asset");
        foreach (TMP_Text text in dropdown.GetComponentsInChildren<TMP_Text>(true))
        {
            text.font = menuFont;
            text.fontSize = 26f;
            text.color = Color.black;
        }
        dropdownObject.AddComponent<LanguageSelectorUI>();

        GameObject labelObject = new GameObject("LanguageLabel", typeof(RectTransform));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(parent, false);
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(420f, 280f);
        labelRect.sizeDelta = new Vector2(300f, 50f);
        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.font = menuFont;
        label.text = "언어";
        label.fontSize = 32f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        labelObject.AddComponent<LocalizedTMPText>().SetReference("options.language");
    }

    private static void CreateSettingLabel(RectTransform parent, string name, string key, string value)
    {
        GameObject labelObject = new GameObject(name, typeof(RectTransform));
        labelObject.transform.SetParent(parent, false);
        TMP_Text text = labelObject.AddComponent<TextMeshProUGUI>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ResourceRoot + "/Fonts/Japanese.asset");
        text.fontSize = 30;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.text = value;
        labelObject.AddComponent<LocalizedTMPText>().SetReference(key);
    }

    private static void CreateLanguageButton(RectTransform panel, TMP_Dropdown dropdown)
    {
        GameObject buttonObject = TMP_DefaultControls.CreateButton(new TMP_DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd")
        });
        buttonObject.name = "LanguageSelectButton";
        buttonObject.transform.SetParent(panel, false);
        buttonObject.GetComponent<Image>().color = new Color(0.22f, 0.22f, 0.22f, 1f);
        TMP_Text caption = buttonObject.GetComponentInChildren<TMP_Text>();
        caption.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ResourceRoot + "/Fonts/Japanese.asset");
        caption.fontSize = caption.fontSizeMax = 30f;
        caption.fontSizeMin = 16f;
        caption.enableAutoSizing = true;
        caption.color = Color.white;
        caption.raycastTarget = false;
        caption.text = "언어 선택: 한국어";
        caption.gameObject.AddComponent<LocalizedTMPText>().SetReference("options.languageSelect", "한국어");
        buttonObject.AddComponent<LanguageMenuButton>().Configure(dropdown, caption);
        // Retain TMP's selectable native-language list, opened by the new button.
        dropdown.GetComponent<Image>().enabled = false;
        dropdown.GetComponent<Image>().raycastTarget = false;
        dropdown.captionText.enabled = false;
        Image arrow = dropdown.transform.Find("Arrow")?.GetComponent<Image>();
        if (arrow != null)
            arrow.enabled = false;
        dropdown.navigation = new Navigation { mode = Navigation.Mode.None };
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string>(GameLocalization.LanguageNames));
        dropdown.template.sizeDelta = new Vector2(dropdown.template.sizeDelta.x, 250f);
        RectTransform content = dropdown.template.GetComponent<ScrollRect>().content;
        content.sizeDelta = new Vector2(content.sizeDelta.x, 42f);
        RectTransform item = dropdown.itemText.GetComponentInParent<Toggle>(true).transform as RectTransform;
        item.sizeDelta = new Vector2(item.sizeDelta.x, 42f);
    }

    [MenuItem("Tools/Localization/Validate Translations")]
    public static void Validate()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        SceneSetup[] sceneSetup = EditorSceneManager.GetSceneManagerSetup();
        try { ValidateCatalogAndScenes(); }
        finally
        {
            if (!Application.isBatchMode)
                EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
        }
    }

    private static void ValidateCatalogAndScenes()
    {
        Catalog catalog = ReadCatalog();
        ValidateAssets(catalog);
        ValidateSceneBindings(catalog);
    }

    [MenuItem("Tools/Localization/Validate Translation Assets Only")]
    public static void ValidateAssetsOnly() => ValidateAssets(ReadCatalog());

    private static void ValidateAssets(Catalog catalog)
    {
        foreach (string code in GameLocalization.LanguageCodes)
        {
            StringTable table = GameLocalization.LoadTable(code);
            if (table == null)
                throw new InvalidOperationException("Missing table: " + code);
            foreach (Entry entry in catalog.entries)
            {
                string value = table.GetEntry(entry.key)?.LocalizedValue;
                if (value != entry.Value(code))
                    throw new InvalidOperationException($"Translation mismatch: {entry.key}/{code}");
                if (entry.key.StartsWith("ui.day", StringComparison.Ordinal)
                    || entry.key == "options.languageSelect")
                    string.Format(value, 5);
                if (code != "ko")
                {
                    TMP_FontAsset font = GameLocalization.GetFont(code);
                    string characters = PlainText(value);
                    if (font == null || !font.HasCharacters(characters, out uint[] missing, false, false))
                        throw new InvalidOperationException($"Missing font coverage: {entry.key}/{code}");
                }
            }
        }
        TMP_FontAsset menuFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ResourceRoot + "/Fonts/Japanese.asset");
        if (!menuFont.HasCharacters(string.Concat(GameLocalization.LanguageNames)))
            throw new InvalidOperationException("The language menu is missing native-name glyphs.");
        Debug.Log($"LOCALIZATION_ASSETS_VALIDATED: {catalog.entries.Length * GameLocalization.LanguageCodes.Length} translations, 5 fonts.");
    }

    private static void ValidateSceneBindings(Catalog catalog)
    {
        var sources = new HashSet<string>(catalog.entries.SelectMany(entry => entry.sources));
        var literals = new HashSet<string>(catalog.literals);
        int checkedLabels = 0;
        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes.Where(scene => scene.enabled))
        {
            Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            int menus = 0;
            int languageButtons = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                menus += root.GetComponentsInChildren<LanguageSelectorUI>(true).Length;
                languageButtons += root.GetComponentsInChildren<LanguageMenuButton>(true).Length;
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.GetComponentInParent<LanguageSelectorUI>(true) != null || string.IsNullOrWhiteSpace(text.text))
                        continue;
                    LocalizedTMPText localized = text.GetComponent<LocalizedTMPText>();
                    if (sources.Contains(text.text) && localized == null)
                        throw new InvalidOperationException($"Unbound text in {buildScene.path}: {text.name}");
                    if (localized != null)
                    {
                        if (catalog.entries.All(entry => entry.key != localized.Key))
                            throw new InvalidOperationException($"Unknown key in {buildScene.path}: {localized.Key}");
                        checkedLabels++;
                    }
                    else if (!literals.Contains(text.text) && Regex.IsMatch(text.text, "[가-힣]"))
                        throw new InvalidOperationException($"Untranslated Korean text in {buildScene.path}: {text.name}");
                }
            }
            if (menus == 0)
                throw new InvalidOperationException("Missing language selector: " + buildScene.path);
            if (languageButtons != menus)
                throw new InvalidOperationException("Missing language button: " + buildScene.path);
        }
        Debug.Log($"LOCALIZATION_VALIDATED: {catalog.entries.Length * GameLocalization.LanguageCodes.Length} translations, 5 fonts, {checkedLabels} labels, 3 scenes.");
    }

    private static string PlainText(string text) => Regex.Replace(text, "<[^>]+>", "")
        .Replace("\\u000b", "\n");

    public static void ExportUnbound()
    {
        Catalog catalog = ReadCatalog();
        var known = new HashSet<string>(catalog.entries.SelectMany(entry => entry.sources));
        known.UnionWith(catalog.literals);
        var missing = new List<Entry>();
        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes.Where(scene => scene.enabled))
        {
            Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (string.IsNullOrWhiteSpace(text.text) || known.Contains(text.text)
                        || text.GetComponent<LocalizedTMPText>() != null
                        || text.GetComponentInParent<LanguageSelectorUI>(true) != null)
                        continue;
                    if (missing.Any(entry => entry.ko == text.text))
                        continue;
                    missing.Add(new Entry { key = text.name, ko = text.text, sources = new[] { buildScene.path } });
                }
            }
        }
        File.WriteAllText(".utmp/localization/unbound.json", JsonUtility.ToJson(new Catalog { entries = missing.ToArray() }, true));
        Debug.Log($"LOCALIZATION_UNBOUND: {missing.Count}");
    }

    public static void BuildValidationPlayer()
    {
        Validate();
        Directory.CreateDirectory(".utmp/localization/player");
        UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            target = BuildTarget.StandaloneWindows64,
            locationPathName = ".utmp/localization/player/CCTV_SCP.exe",
            options = BuildOptions.Development
        });
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new InvalidOperationException($"Player build failed: {report.summary.totalErrors} errors.");
        Debug.Log($"LOCALIZATION_BUILD_PASSED: {report.summary.totalSize} bytes, {report.summary.totalWarnings} warnings.");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
