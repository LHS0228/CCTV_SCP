using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

/// <summary>Runs real scene, language-switch, subtitle and saved-locale checks in Play Mode.</summary>
[InitializeOnLoad]
public static class GameLocalizationPlayModeValidation
{
    private const string Pending = "CCTV.LocalizationValidation.Pending";
    private const string Started = "CCTV.LocalizationValidation.Started";
    private const string HadPreference = "CCTV.LocalizationValidation.HadPreference";
    private const string SavedPreference = "CCTV.LocalizationValidation.SavedPreference";
    private const string ExitCode = "CCTV.LocalizationValidation.ExitCode";
    private const string SavedScenes = "CCTV.LocalizationValidation.SavedScenes";
    private const string SavedBackground = "CCTV.LocalizationValidation.SavedBackground";
    [Serializable] private sealed class OpenScenes { public SceneSetup[] scenes; }

    static GameLocalizationPlayModeValidation()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.update += Watchdog;
    }

    public static void Run()
    {
        if (!Application.isBatchMode)
        {
            if (EditorSceneManager.GetSceneManagerSetup().Any(scene => SceneManager.GetSceneByPath(scene.path).isDirty))
                throw new InvalidOperationException("Save modified scenes before running localization validation.");
            SessionState.SetString(SavedScenes, JsonUtility.ToJson(new OpenScenes { scenes = EditorSceneManager.GetSceneManagerSetup() }));
        }
        SessionState.SetBool(HadPreference, PlayerPrefs.HasKey(GameLocalization.PreferenceKey));
        SessionState.SetBool(SavedBackground, Application.runInBackground);
        SessionState.SetString(SavedPreference, PlayerPrefs.GetString(GameLocalization.PreferenceKey, "ko"));
        SessionState.SetBool(Pending, true);
        SessionState.SetInt(ExitCode, -1);
        SessionState.SetString(Started, DateTime.UtcNow.ToString("O"));
        EditorSceneManager.OpenScene(EditorBuildSettings.scenes.First(scene => scene.enabled).path);
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (!Application.isBatchMode && state == PlayModeStateChange.EnteredEditMode
            && !SessionState.GetBool(Pending, false) && SessionState.GetString(SavedScenes, "") != "")
        {
            OpenScenes saved = JsonUtility.FromJson<OpenScenes>(SessionState.GetString(SavedScenes, ""));
            SessionState.EraseString(SavedScenes);
            EditorApplication.delayCall += () => EditorSceneManager.RestoreSceneManagerSetup(saved.scenes);
        }
        if (Application.isBatchMode && state == PlayModeStateChange.EnteredEditMode
            && SessionState.GetInt(ExitCode, -1) >= 0)
        {
            int code = SessionState.GetInt(ExitCode, 1);
            SessionState.SetInt(ExitCode, -1);
            EditorApplication.delayCall += () => EditorApplication.Exit(code);
        }
        if (SessionState.GetBool(Pending, false) && state == PlayModeStateChange.EnteredPlayMode)
        {
            Application.runInBackground = true;
            GameObject runner = new GameObject("Localization Validation Runner");
            UnityEngine.Object.DontDestroyOnLoad(runner);
            runner.AddComponent<GameLocalizationValidationRunner>();
        }
    }

    private static void Watchdog()
    {
        if (!SessionState.GetBool(Pending, false))
            return;
        if (DateTime.TryParse(SessionState.GetString(Started, ""), out DateTime started)
            && (DateTime.UtcNow - started.ToUniversalTime()).TotalSeconds > 120)
        {
            Debug.LogError("LOCALIZATION_PLAYMODE_TIMEOUT");
            Finish(false);
        }
    }

    public static void Finish(bool success)
    {
        SessionState.SetBool(Pending, false);
        if (SessionState.GetBool(HadPreference, false))
            PlayerPrefs.SetString(GameLocalization.PreferenceKey, SessionState.GetString(SavedPreference, "ko"));
        else
            PlayerPrefs.DeleteKey(GameLocalization.PreferenceKey);
        PlayerPrefs.Save();
        Application.runInBackground = SessionState.GetBool(SavedBackground, false);
        if (Application.isBatchMode)
            SessionState.SetInt(ExitCode, success ? 0 : 1);
        EditorApplication.ExitPlaymode();
    }
}

public sealed class GameLocalizationValidationRunner : MonoBehaviour
{
    private bool failed;
    private int checks;

    private void OnEnable() => Application.logMessageReceived += OnLog;
    private void OnDisable() => Application.logMessageReceived -= OnLog;

    private void OnLog(string message, string stackTrace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            failed = true;
    }

    private IEnumerator Start()
    {
        yield return null;
        yield return null;
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes.Where(scene => scene.enabled))
        {
            if (SceneManager.GetActiveScene().path != scene.path)
            {
                yield return SceneManager.LoadSceneAsync(scene.path);
                yield return null;
                yield return null;
            }
            LanguageSelectorUI[] selectors = FindObjectsByType<LanguageSelectorUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Check(selectors.Length > 0, "language selector in " + scene.path);
            foreach (LanguageSelectorUI selector in selectors)
            {
                Transform parent = selector.transform.parent;
                while (parent != null)
                {
                    if (parent.name == "OptionMenu" || parent.name == "Ingame_OptionMenu")
                    {
                        parent.gameObject.SetActive(true);
                        break;
                    }
                    parent = parent.parent;
                }
            }

            // Allow TMP Start and the newly enabled menu's graphic depth/layout to initialize.
            yield return null;

            GameObject dynamicObject = new GameObject("Dynamic Subtitle Probe", typeof(RectTransform));
            TextMeshProUGUI dynamicLabel = dynamicObject.AddComponent<TextMeshProUGUI>();
            dynamicLabel.font = GameLocalization.GetFont("en");
            dynamicLabel.fontSize = 32;
            GameLocalization.SetText(dynamicLabel, "ui.dayReport", 5);

            for (int index = 0; index < GameLocalization.LanguageCodes.Length; index++)
            {
                TMP_Dropdown dropdown = selectors[0].GetComponent<TMP_Dropdown>();
                LanguageMenuButton languageButton = dropdown.transform.parent.GetComponentInChildren<LanguageMenuButton>();
                Check(languageButton != null, "language button in " + scene.path);
                Canvas.ForceUpdateCanvases();
                GameObject pointerTarget = PointerHandler(languageButton.transform as RectTransform);
                Check(pointerTarget == languageButton.gameObject,
                    "pointer reaches language button " + scene.path + "; actual=" + (pointerTarget == null ? "none" : pointerTarget.name));
                ExecuteEvents.Execute(languageButton.gameObject, new PointerEventData(EventSystem.current),
                    ExecuteEvents.pointerClickHandler);
                yield return null;
                yield return null;
                Check(dropdown.IsExpanded, "language button opens choices");
                UnityEngine.UI.Toggle[] choices = dropdown.GetComponentsInChildren<UnityEngine.UI.Toggle>()
                    .Where(toggle => toggle.isActiveAndEnabled).ToArray();
                Check(choices.Length == GameLocalization.LanguageCodes.Length, "five visible language choices");
                Check(dropdown.options.Count == GameLocalization.LanguageCodes.Length, "five configured language choices");
                ExecuteEvents.Execute(choices[index].gameObject, new PointerEventData(EventSystem.current),
                    ExecuteEvents.pointerClickHandler);
                yield return new WaitForSecondsRealtime(0.25f);
                Check(!dropdown.IsExpanded, "language choices close after selection");
                yield return null;
                string code = GameLocalization.LanguageCodes[index];
                Check(GameLocalization.CurrentCode == code, "selected language " + code);
                Check(PlayerPrefs.GetString(GameLocalization.PreferenceKey, "ko") == code,
                    "saved language " + code);
                Check(LocalizationSettings.SelectedLocale.Identifier.Code == code, "Unity locale " + code);
                Check(dropdown.captionText.text == GameLocalization.LanguageNames[index], "native menu caption " + code);
                Check(languageButton.Caption.text == GameLocalization.Get("options.languageSelect", GameLocalization.LanguageNames[index]),
                    "language button caption " + code);
                foreach (LocalizedSpriteButton button in FindObjectsByType<LocalizedSpriteButton>(FindObjectsSortMode.None))
                {
                    bool translated = code == "ja" || code == "zh-CN" || code == "zh-TW";
                    UnityEngine.UI.Graphic[] graphics = button.GetComponentsInChildren<UnityEngine.UI.Graphic>();
                    Check(graphics.Any(graphic => graphic.isActiveAndEnabled && graphic.raycastTarget),
                        "translated button remains clickable " + button.name + "/" + code);
                    Check(button.GetComponent<UnityEngine.UI.Image>().enabled != translated,
                        "button artwork switches " + button.name + "/" + code);
                }
                Check(dynamicLabel.text == GameLocalization.Get("ui.dayReport", 5), "dynamic arguments survive locale change " + code);

                var localizedString = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(GameLocalization.TableName, "menu.exit");
                yield return localizedString;
                Check(localizedString.Result == GameLocalization.Get("menu.exit"), "standard Unity string API " + code);

                foreach (LocalizedTMPText label in FindObjectsByType<LocalizedTMPText>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    label.Refresh();
                    TMP_Text text = label.GetComponent<TMP_Text>();
                    Check(!string.IsNullOrEmpty(text.text), "nonempty text " + label.Key);
                    if (code != "ko" && text.isActiveAndEnabled)
                    {
                        text.ForceMeshUpdate();
                        Check(text.textInfo.characterInfo.Take(text.textInfo.characterCount)
                            .All(character => character.character != '\u25a1'), "no missing glyph boxes " + label.Key);
                    }
                }
                ValidateReportedUI(code);
                if (SceneManager.GetActiveScene().buildIndex == 1)
                    yield return ValidateManualRules(code);
                if (SceneManager.GetActiveScene().buildIndex == 0)
                {
                    yield return ValidateContract(selectors[0], code);
                    yield return ValidateButtonRaycasts(selectors[0], code);
                    yield return Capture(".utmp/localization/options-" + code + ".png");
                }
            }
            if (SceneManager.GetActiveScene().buildIndex == 0)
            {
                GameLocalization.SetLanguage(0);
                yield return ValidateContract(selectors[0], "ko");
            }
            Destroy(dynamicObject);
            if (SceneManager.GetActiveScene().buildIndex == 1)
            {
                yield return ValidateGameplayChanges();
                yield return ValidateRuntimeToolAndDayEnd();
            }
            if (SceneManager.GetActiveScene().buildIndex == 2)
            {
                Title_End_System ending = FindFirstObjectByType<Title_End_System>();
                typeof(Title_End_System).GetField("enddingCount", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ending, 8);
                ending.EnddingAnimationCountingEvent();
                TMP_Text subtitle = new SerializedObject(ending).FindProperty("voiceText").objectReferenceValue as TMP_Text;
                Check(subtitle.GetComponent<LocalizedTMPText>().Key == "voice.ending.last", "last recorded announcement is connected");
                Check(subtitle.gameObject.activeInHierarchy, "last announcement caption is visible");
            }
            yield return null;
        }
        Debug.Log($"LOCALIZATION_PLAYMODE_{(failed ? "FAILED" : "PASSED")}: {checks} checks, 3 scenes, {GameLocalization.LanguageCodes.Length} languages.");
        File.WriteAllText(".utmp/localization/playmode-result.txt", $"{(failed ? "FAILED" : "PASSED")}: {checks} checks, 3 scenes, {GameLocalization.LanguageCodes.Length} languages.\n");
        GameLocalizationPlayModeValidation.Finish(!failed);
    }

    private void Check(bool condition, string message)
    {
        checks++;
        if (condition)
            return;
        failed = true;
        Debug.LogError("LOCALIZATION_CHECK_FAILED: " + message);
    }

    private void ValidateReportedUI(string code)
    {
        string[] manualKeys = { "manual.rules.plain", "manual.circuit.menu", "manual.mimic.menu", "manual.tentacles.menu", "manual.symptoms.menu" };
        Manual[] buttons = FindObjectsByType<Manual>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Manual button in buttons)
        {
            LocalizedTMPText label = button.GetComponentInChildren<LocalizedTMPText>(true);
            Check(label != null && label.Key == manualKeys[button.manualIndex], "distinct manual binding " + button.manualIndex);
            Check(label.GetComponent<TMP_Text>().text == GameLocalization.Get(manualKeys[button.manualIndex]), "manual caption " + code + "/" + button.manualIndex);
        }
        if (buttons.Length > 0)
            Check(manualKeys.Select(key => GameLocalization.Get(key)).Distinct().Count() == 5, "five distinct manual titles " + code);

        foreach (TabletSceneManager tablet in FindObjectsByType<TabletSceneManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var data = new SerializedObject(tablet);
            SerializedProperty starts = data.FindProperty("startBts");
            for (int i = 0; i < starts.arraySize; i++)
            {
                var button = starts.GetArrayElementAtIndex(i).objectReferenceValue as UnityEngine.UI.Button;
                var image = button.GetComponent<UnityEngine.UI.Image>();
                Check(image.enabled && image.sprite != null && image.sprite.texture.name == "Start_BaseMap", "original tablet artwork " + code + "/" + i);
                Check(button.GetComponent<LocalizedSpriteButton>() == null && button.transform.Find("TranslatedButtonContent") == null, "no tablet replacement " + code + "/" + i);
            }
        }

        string[] voiceKeys = { "voice.day1", "voice.manual", "voice.day2", "voice.day3", "voice.day4", "voice.day5", "voice.emergency", "voice.ending.congratulations", "voice.ending.erasure", "voice.ending.last" };
        foreach (string key in voiceKeys)
            Check(!string.IsNullOrEmpty(GameLocalization.Get(key)) && GameLocalization.Get(key) != key, "recorded announcement entry " + code + "/" + key);
        if (code == "en")
        {
            string[] recorded = {
                "Previous records expunged. New designation assigned. Welcome, Administrator 32.",
                "Check the manual for essential task information.",
                "Welcome, Administrator 32. Reminder: The preservation of company assets takes strict priority over personnel safety.",
                "Welcome, Administrator 32.and please remain alert as the Entity is currently unstable for reasons unknown.",
                "Welcome, Administrator 32. Please note that asset loss leads to immediate termination.",
                "Welcome, Administrator 32. Complete your service with perfection until the last moment.",
                "Emergency. Emergency. Execute protocol immediately......",
                "Administrator 32. Your term of service has ended. Congratulations.",
                "Possibility of confidential leak confirmed. Proceeding with Termination.",
                "Previous records expunged. New designation assigned. Welcome, Administrator 33."
            };
            for (int i = 0; i < voiceKeys.Length; i++)
                Check(GameLocalization.Get(voiceKeys[i]) == recorded[i], "English caption matches recording " + voiceKeys[i]);
        }
        foreach (LocalizedTMPText label in FindObjectsByType<LocalizedTMPText>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            bool document = label.Key.StartsWith("manual.") || label.Key.StartsWith("contract.") || label.Key.StartsWith("entity.") || label.Key.StartsWith("report.");
            if (code == "ko" || !document)
                continue;
            TMP_Text text = label.GetComponent<TMP_Text>();
            Check(!text.text.Contains("<b>") && (text.fontStyle & FontStyles.Bold) == 0, "uniform document weight " + code + "/" + label.Key);
            Check(text.font == GameLocalization.GetFont(code), "uniform document font " + code + "/" + label.Key);
        }
    }

    private IEnumerator ValidateManualRules(string code)
    {
        GameManager.Instance.isTimeStop = true;
        StartSystem.instance.StopAllCoroutines();
        ExecutionTimeLineManager.instance.GetComponent<UnityEngine.Playables.PlayableDirector>().Stop();
        ManualManager manual = FindFirstObjectByType<ManualManager>();
        GameObject manualPanel = new SerializedObject(manual).FindProperty("ManualPanel").objectReferenceValue as GameObject;
        manualPanel.SetActive(true);
        if (!manual.isOnManual)
        {
            manual.MovingManualView();
            yield return new WaitForSecondsRealtime(0.7f);
        }
        manual.MoveToDetailPanel(0);
        yield return null;
        ManualRulesLocalizationLayout layout = FindFirstObjectByType<ManualRulesLocalizationLayout>();
        Check(layout != null, "rules page has translated layout");
        TMP_Text[] sections = new[] { "Text1", "Text1 (1)", "Text1 (2)" }
            .Select(name => layout.transform.Find(name).GetComponent<TMP_Text>()).ToArray();
        foreach (TMP_Text text in sections)
            text.ForceMeshUpdate();
        if (code == "ko")
        {
            float[] positions = { 0.329f, 0.037f, -0.327f };
            for (int i = 0; i < sections.Length; i++)
            {
                Check(Mathf.Approximately(sections[i].fontSize, .035f), "original Korean rules size");
                Check(Mathf.Abs(sections[i].rectTransform.anchoredPosition.y - positions[i]) < .001f,
                    "original Korean section position " + i);
                Check(Mathf.Abs(sections[i].rectTransform.rect.height - .275f) < .001f,
                    "original Korean section height " + i);
            }
        }
        else
        {
            foreach (TMP_Text text in sections)
            {
                Check(text.fontSize >= .02625f && Mathf.Abs(text.fontSize - sections[0].fontSize) < .00001f,
                    "readable uniform rules size " + code);
                Check(!text.isTextOverflowing, "rules text fits its area " + text.name + "/" + code);
                Check(!text.enableAutoSizing, "individual rules sections do not collapse " + code);
            }
            for (int i = 1; i < sections.Length; i++)
            {
                RectTransform above = sections[i - 1].rectTransform, below = sections[i].rectTransform;
                float aboveBottom = above.anchoredPosition.y + above.rect.yMin * above.localScale.y;
                float belowTop = below.anchoredPosition.y + below.rect.yMax * below.localScale.y;
                Check(aboveBottom - belowTop >= .017f, "rules sections do not overlap " + code);
            }
            RectTransform last = sections[2].rectTransform;
            Check(last.anchoredPosition.y + last.rect.yMin * last.localScale.y >= -.511f,
                "rules body ends before footer " + code);
            TMP_Text caption = layout.transform.Find("hangle").GetComponent<TMP_Text>();
            TMP_Text number = layout.transform.Find("Number").GetComponent<TMP_Text>();
            caption.ForceMeshUpdate();
            number.ForceMeshUpdate();
            Check(!caption.isTextOverflowing && caption.textInfo.lineCount == 1, "protocol caption fits on one line " + code);
            Check(caption.rectTransform.anchoredPosition.x + caption.rectTransform.rect.xMax
                < number.rectTransform.anchoredPosition.x + number.rectTransform.rect.xMin,
                "protocol caption has separate digit area " + code);
            if (code == "en" || code == "ja")
                Check(caption.fontSize <= .0421f, "English/Japanese protocol caption is smaller " + code);
            Debug.Log($"MANUAL_RULES_SIZE: {code}, body={sections[0].fontSize:F5}, caption={caption.fontSize:F5}");
        }
        GameObject option = FindObjectsByType<LanguageSelectorUI>(FindObjectsSortMode.None)
            .Select(selector => selector.GetComponentInParent<OptionLanguageLayout>().gameObject)
            .First();
        bool oldOption = option.activeSelf;
        option.SetActive(false);
        UnityEngine.UI.Image fade = FindObjectsByType<UnityEngine.UI.Image>(FindObjectsSortMode.None)
            .First(image => image.name == "FadePanel");
        Color oldFade = fade.color;
        fade.color = Color.clear;
        yield return Capture(".utmp/localization/manual-rules-" + code + ".png");
        fade.color = oldFade;
        option.SetActive(oldOption);
        manual.TryHandleBackInput();
        manualPanel.SetActive(false);
        // Capture temporarily changes overlay canvas modes. Let their screen matrices settle
        // after restoration before the next native menu raycast.
        yield return null;
        yield return null;
    }

    private static void ClickEditorButton(UnityEngine.UIElements.Button button)
    {
        // Drive the Button's registered Clickable, without requiring keyboard focus in a
        // foreground Editor window. Unity's navigation event only fires on a focused control.
        typeof(UnityEngine.UIElements.Clickable).GetMethod("SimulateSingleClick", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(button.clickable, new object[] { null, 0 });
    }

    private IEnumerator ValidateRuntimeToolAndDayEnd()
    {
        ManualManager manual = FindFirstObjectByType<ManualManager>();
        manual.TryHandleBackInput();
        yield return new WaitForSecondsRealtime(.7f);
        foreach (LanguageSelectorUI selector in FindObjectsByType<LanguageSelectorUI>(FindObjectsSortMode.None))
        {
            Transform parent = selector.transform;
            while (parent != null && parent.name != "Ingame_OptionMenu") parent = parent.parent;
            if (parent != null) parent.gameObject.SetActive(false);
        }
        RuntimeTestWindow.Open();
        RuntimeTestWindow window = Resources.FindObjectsOfTypeAll<RuntimeTestWindow>().First();
        yield return new WaitForSecondsRealtime(.3f);
        var ui = window.rootVisualElement;
        UnityEngine.UIElements.Toggle pause = ui.Q<UnityEngine.UIElements.Toggle>("pause-clock");
        pause.value = true;
        ui.Q<UnityEngine.UIElements.Toggle>("suspend-spawns").value = true;
        Check(GameManager.Instance.isTimeStop, "editor time pause controls actual clock");
        Check(GameManager.Instance.anomalySystem.EditorSuspendNaturalSpawns, "editor natural-spawn toggle works");
        ui.Q<UnityEngine.UIElements.IntegerField>("hour").value = 1;
        ui.Q<UnityEngine.UIElements.IntegerField>("minute").value = 23;
        ClickEditorButton(ui.Q<UnityEngine.UIElements.Button>("set-clock"));
        yield return new WaitForSecondsRealtime(.15f);
        Check(DaySystem.Instance.GetClock() == 83, "editor time fields/button set 01:23");

        AnomalySystem system = GameManager.Instance.anomalySystem;
        BasicEventAnomaly[] pool = system.standardEventPool.Concat(system.specialEventPool).Distinct().ToArray();
        foreach (BasicEventAnomaly anomaly in pool)
        {
            ui.Q<UnityEngine.UIElements.DropdownField>("anomaly").index = System.Array.IndexOf(pool, anomaly);
            ClickEditorButton(ui.Q<UnityEngine.UIElements.Button>("trigger"));
            yield return new WaitForSecondsRealtime(.15f);
            bool registered = system.activeStandardAnomalies.Any(active => active.eventScript == anomaly)
                || system.EditorActiveSpecialAnomaly?.eventScript == anomaly;
            Check(registered, "editor picker executes/registers " + anomaly.name);
            if (registered)
                Check(!system.EditorTriggerAnomaly(anomaly, out _), "editor rejects duplicate anomaly " + anomaly.name);
            ClickEditorButton(ui.Q<UnityEngine.UIElements.Button>("clear"));
            yield return new WaitForSecondsRealtime(.4f);
            Check(system.activeStandardAnomalies.Count == 0 && system.EditorActiveSpecialAnomaly == null,
                "editor clear button cleans registration " + anomaly.name);
        }
        Check(typeof(RuntimeTestWindow).Assembly.GetName().Name == "Assembly-CSharp-Editor",
            "test window is compiled in the Editor assembly");

        for (int day = 1; day <= 2; day++)
        {
            Check(DaySystem.Instance.GetNowDay() == day, "day-end regression begins on expected day " + day);
            StartSystem.instance.StopAllCoroutines();
            GameManager.Instance.isGameStop = false;
            GameManager.Instance.isTimeStop = true;
            ClickEditorButton(ui.Q<UnityEngine.UIElements.Button>("finish-day"));
            yield return new WaitForSecondsRealtime(4.6f);
            DayEndContract report = DayEndContract.Instance;
            Check(report != null && report.isContractOn && report.contractPanel.gameObject.activeInHierarchy,
                "06:00 shows report " + day);
            Canvas canvas = report.contractPanel.GetComponentInParent<Canvas>();
            Check(canvas.sortingOrder > OverlayLetterboxSafeArea.FadePanelSortingOrder,
                "day report renders above black fade " + day);
            Check(PointerHandler(report.yesButton.transform as RectTransform) == report.yesButton.gameObject,
                "pointer reaches next-day button " + day);
            if (day == 1)
            {
                int sorting = canvas.sortingOrder;
                canvas.sortingOrder = 0;
                yield return Capture(".utmp/localization/day1-report-baseline.png");
                canvas.sortingOrder = sorting;
                yield return Capture(".utmp/localization/day1-report-fixed.png");
            }
            ExecuteEvents.Execute(report.yesButton.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            report.OnClickNextButton(); // A second request must not schedule a second scene load.
            Check(!report.yesButton.interactable, "next-day button disables repeated confirmation " + day);
            yield return new WaitForSecondsRealtime(2.2f);
            Check(canvas.sortingOrder < OverlayLetterboxSafeArea.FadePanelSortingOrder,
                "next-day Timeline regains fade priority " + day);
            float deadline = Time.realtimeSinceStartup + 12f;
            while (Time.realtimeSinceStartup < deadline && (DaySystem.Instance == null || DaySystem.Instance.GetNowDay() == day))
                yield return null;
            yield return new WaitForSecondsRealtime(.3f);
            Check(DaySystem.Instance != null && DaySystem.Instance.GetNowDay() == day + 1,
                "next-day Timeline loads exactly the next day " + day);
            int configuredStart = new SerializedObject(DaySystem.Instance).FindProperty("TIME_START").intValue;
            Check(SceneManager.GetActiveScene().buildIndex == 1 && DaySystem.Instance.GetClock() == configuredStart,
                "next day opens gameplay scene with reset clock " + day);
            Check(!DaySystem.Instance.EditorIsDayClear, "next day can finish again " + day);
        }
        window.Close();
        Debug.Log("MANUAL_DAY_END_RUNTIME_TOOL_VALIDATED");
    }

    private IEnumerator ValidateGameplayChanges()
    {
        Time.timeScale = 1f;
        PlayerMove player = FindFirstObjectByType<PlayerMove>();
        player.enabled = false;
        Rigidbody body = player.GetComponent<Rigidbody>();
        body.isKinematic = true;
        Transform camera = player.headObject.GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>().transform;
        Vector3 cameraPosition = camera.position;
        Quaternion oldRotation = camera.localRotation;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var look = typeof(PlayerMove).GetField("lookDelta", flags);
        var move = typeof(PlayerMove).GetField("moveInput", flags);
        var focus = typeof(PlayerMove).GetMethod("OnApplicationFocus", flags);
        var update = typeof(PlayerMove).GetMethod("Update", flags);
        var applyLook = typeof(PlayerMove).GetMethod("ApplyLook", flags);
        Cursor.lockState = CursorLockMode.Locked;
        float savedSensitivity = player.sensitivity;
        player.sensitivity = 20f;
        player.SetMoveEnable(true);
        focus.Invoke(player, new object[] { true });
        // Batch Mode never locks its native cursor. Exercise the same rotation routine directly.
        applyLook.Invoke(player, new object[] { new Vector2(0f, 240f) });
        Debug.Log($"CAMERA_PITCH_TEST: angle={Quaternion.Angle(camera.localRotation, oldRotation)}, displacement={Vector3.Distance(camera.position, cameraPosition)}");
        Check(Vector3.Distance(camera.position, cameraPosition) < 0.0001f, "camera pitch preserves position");
        Check(Quaternion.Angle(camera.localRotation, oldRotation) > 20f, "camera pitch still changes view");
        applyLook.Invoke(player, new object[] { new Vector2(0f, -480f) });
        Check(Vector3.Distance(camera.position, cameraPosition) < .0001f, "opposite pitch preserves position");
        look.SetValue(player, new Vector2(500f, 500f));
        move.SetValue(player, Vector2.one);
        focus.Invoke(player, new object[] { false });
        Check((Vector2)look.GetValue(player) == Vector2.zero && (Vector2)move.GetValue(player) == Vector2.zero, "focus loss clears input");
        Quaternion beforeFocus = camera.localRotation;
        update.Invoke(player, null);
        Check(Quaternion.Angle(beforeFocus, camera.localRotation) < .001f, "unfocused input cannot move view");
        focus.Invoke(player, new object[] { true });
        update.Invoke(player, null);
        Check(Quaternion.Angle(beforeFocus, camera.localRotation) < .001f, "focus return has no stale delta");
        Cursor.lockState = CursorLockMode.None;
        look.SetValue(player, Vector2.one * 500f);
        update.Invoke(player, null);
        Check(Quaternion.Angle(beforeFocus, camera.localRotation) < .001f, "menu cursor cannot turn camera");
        camera.localRotation = oldRotation;
        player.sensitivity = savedSensitivity;
        player.SetMoveEnable(false);

        AnomalySystem system = GameManager.Instance.anomalySystem;
        Anomaly_CryingMask anomaly = AssetDatabase.LoadAssetAtPath<Anomaly_CryingMask>("Assets/Anomalys/SO_Event/Anomaly_Crying Mask.asset");
        UnityEngine.Random.State randomState = UnityEngine.Random.state;
        foreach (int seed in new[] { 7, 31, 97, 123, 205 })
        {
            UnityEngine.Random.InitState(seed);
            anomaly.Execute();
            yield return null;
            GetMask[] masks = FindObjectsByType<GetMask>(FindObjectsSortMode.None);
            Check(masks.Length == (int)StabilityManager.Instance.dayGetMaskValue[DaySystem.Instance.GetNowDay() - 1], "floor spawn count seed " + seed);
            foreach (GetMask mask in masks)
            {
                Check(Mathf.Abs(mask.transform.position.y) < .02f, "mask on actual room floor");
                Check(Vector3.Dot(mask.transform.Find("MaskDeath").forward, Vector3.up) > .99f, "mask faces up");
                AudioSource noise = mask.GetComponentInChildren<AudioSource>();
                Check(noise != null && noise.clip.name == "Mask_noise" && noise.loop && noise.spatialBlend == 1f, "spatial mask noise starts");
                Check(noise != null && noise.rolloffMode == AudioRolloffMode.Linear && noise.minDistance < noise.maxDistance, "mask noise grows nearer");
                Check(mask.guideUI == null || !mask.guideUI.activeSelf, "no F prompt");
            }
            anomaly.Fail();
            yield return null;
            Check(FindObjectsByType<GetMask>(FindObjectsSortMode.None).Length == 0, "mask failure cleans objects");
            Check(!FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Any(source => source.clip != null && source.clip.name == "Mask_noise"), "mask failure cleans loops");
        }
        UnityEngine.Random.state = randomState;

        anomaly.Execute();
        system.activeStandardAnomalies.Add(new ActiveAnomaly(anomaly, anomaly.eventPlace, EventType.Mission));
        yield return null;
        GetMask[] collection = FindObjectsByType<GetMask>(FindObjectsSortMode.None);
        BoxCollider feet = player.GetComponent<BoxCollider>();
        Vector3 centerOffset = feet.bounds.center - player.transform.position;
        float bottomOffset = feet.bounds.min.y - player.transform.position.y;
        int collected = 0;
        foreach (GetMask mask in collection)
        {
            Vector3 target = mask.transform.position;
            body.position = new Vector3(target.x - centerOffset.x + 1.5f, target.y - bottomOffset + .025f, target.z - centerOffset.z);
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Check(mask != null, "approaching without stepping preserves mask");
            body.position = new Vector3(target.x - centerOffset.x, target.y - bottomOffset + .025f, target.z - centerOffset.z);
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;
            collected++;
            Check(mask == null, "step destroys mask without F");
            Check(StabilityManager.Instance.currentGetMask == (collected == collection.Length ? 0 : collected), "one collection per mask");
        }
        Check(!system.activeStandardAnomalies.Any(active => active.eventScript == anomaly), "all masks complete mission");
        Check(!FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Any(source => source.clip != null && source.clip.name == "Mask_noise"), "collection stops noise");
        AudioSource global = (AudioSource)typeof(SoundManager).GetField("_globalSfxSource", flags).GetValue(SoundManager.Instance);
        Check(global.spatialBlend == 0f && SoundManager.Instance.Data.abnormalMaskCreationWeepingMan.name == "Mask_Spawned", "spawn announcement has no distance attenuation");
        ProtocolSystem protocol = FindFirstObjectByType<ProtocolSystem>();
        protocol.StartProtocol(1);
        Check(GameManager.Instance.voiceText.GetComponent<LocalizedTMPText>().Key == "voice.emergency", "emergency announcement subtitle is connected");
        Check(GameManager.Instance.voiceTextBox.activeInHierarchy, "emergency subtitle visible");
        Check(SoundManager.Instance.Data.deathCommonStabilityZeroProtocolAnnounce.name == "Emergency1", "emergency plays recorded clip");
        protocol.StopProtocol();
        protocol.protocol_Activated = false;
        Debug.Log("SIX_FIXES_GAMEPLAY_VALIDATED");
    }

    private IEnumerator ValidateContract(LanguageSelectorUI selector, string code)
    {
        ContractSystem contract = FindFirstObjectByType<ContractSystem>();
        LocalizedTMPText body = FindObjectsByType<LocalizedTMPText>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(label => label.Key == "contract.terms");
        TMP_Text text = body.GetComponent<TMP_Text>();
        TMP_Text agreement = body.transform.parent.GetComponentsInChildren<LocalizedTMPText>(true)
            .Single(label => label.Key == "contract.acceptQuestion").GetComponent<TMP_Text>();
        bool oldActive = contract.backGroundPanel.activeSelf;
        bool oldMenu = contract.optionMenu.activeSelf;
        bool oldStart = contract.startButton.activeSelf;
        bool oldLogo = contract.gameLogo.activeSelf;
        bool oldOptionsButton = contract.optionButton.gameObject.activeSelf;
        bool oldExitButton = contract.exitButton.gameObject.activeSelf;
        Vector2 oldPosition = contract.contractPanel.anchoredPosition;
        Quaternion oldRotation = contract.contractPanel.localRotation;
        try
        {
            contract.optionMenu.SetActive(false);
            contract.startButton.SetActive(false);
            contract.gameLogo.SetActive(false);
            contract.optionButton.gameObject.SetActive(false);
            contract.exitButton.gameObject.SetActive(false);
            contract.backGroundPanel.SetActive(true);
            contract.contractPanel.anchoredPosition = contract.targetPostion;
            contract.contractPanel.localRotation = Quaternion.identity;
            yield return null;
            body.Refresh();
            Canvas.ForceUpdateCanvases();
            text.ForceMeshUpdate();
            Check(text.textInfo.characterCount > 100, "contract body generates text " + code);
            Check(text.fontSize >= 28.49f, "contract body remains readable " + code);
            if (code == "ko")
            {
                Check(!text.enableAutoSizing && Mathf.Approximately(text.fontSize, 38f), "Korean contract font restored");
                Check(Mathf.Approximately(text.rectTransform.rect.height, 390f)
                    && Mathf.Approximately(text.rectTransform.anchoredPosition.y, 145f), "Korean contract layout restored");
            }
            else
            {
                Check(!text.isTextOverflowing, "translated contract fits " + code);
                Check(text.rectTransform.rect.height > 780f, "contract uses available body space " + code);
                TMP_Text title = body.transform.parent.GetComponentsInChildren<LocalizedTMPText>(true)
                    .Single(label => label.Key == "contract.title").GetComponent<TMP_Text>();
                title.ForceMeshUpdate();
                float titleBottom = text.rectTransform.InverseTransformPoint(title.rectTransform.TransformPoint(new Vector3(0, title.textBounds.min.y))).y;
                float bodyTop = text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(character => character.isVisible)
                    .Max(character => character.topRight.y);
                Check(bodyTop < titleBottom, "contract body stays below title " + code);
            }
            float bottom = text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(character => character.isVisible)
                .Min(character => character.bottomLeft.y);
            Vector3[] corners = new Vector3[4];
            agreement.rectTransform.GetWorldCorners(corners);
            float agreementTop = corners.Max(corner => text.rectTransform.InverseTransformPoint(corner).y);
            Check(bottom > agreementTop, "contract body stays above agreement prompt " + code);
            Debug.Log($"CONTRACT_LAYOUT: {code}, size={text.fontSize:F2}, height={text.rectTransform.rect.height:F2}, lines={text.textInfo.lineCount}, overflow={text.isTextOverflowing}");
            yield return Capture(".utmp/localization/contract-" + code + ".png");
        }
        finally
        {
            contract.contractPanel.anchoredPosition = oldPosition;
            contract.contractPanel.localRotation = oldRotation;
            contract.backGroundPanel.SetActive(oldActive);
            contract.startButton.SetActive(oldStart);
            contract.gameLogo.SetActive(oldLogo);
            contract.optionButton.gameObject.SetActive(oldOptionsButton);
            contract.exitButton.gameObject.SetActive(oldExitButton);
            contract.optionMenu.SetActive(oldMenu);
        }
        yield return null;
    }

    private IEnumerator ValidateButtonRaycasts(LanguageSelectorUI selector, string code)
    {
        Transform menu = selector.transform;
        while (menu.parent != null && menu.name != "OptionMenu")
            menu = menu.parent;
        menu.gameObject.SetActive(false);
        yield return null;
        Canvas.ForceUpdateCanvases();
        foreach (LocalizedSpriteButton button in FindObjectsByType<LocalizedSpriteButton>(FindObjectsSortMode.None))
        {
            RectTransform rect = button.transform as RectTransform;
            Canvas canvas = button.GetComponentInParent<Canvas>();
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 center = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = center };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            GameObject clickHandler = hits.Count == 0 ? null
                : ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            var clickable = clickHandler == null ? null : clickHandler.GetComponent<UnityEngine.UI.Button>();
            Check(clickable != null && clickable.interactable
                && (button.transform.IsChildOf(clickable.transform) || clickable.transform.IsChildOf(button.transform)),
                "pointer reaches title button " + button.name + "/" + code);
        }
        menu.gameObject.SetActive(true);
        yield return null;
    }

    private static GameObject PointerHandler(RectTransform rect)
    {
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 center = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
        var pointer = new PointerEventData(EventSystem.current) { position = center };
        var hits = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        return hits.Count == 0 ? null : ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
    }

    private static IEnumerator Capture(string path)
    {
        Camera camera = Camera.main;
        GameObject temporaryCamera = null;
        if (camera == null)
        {
            // Day-end Timeline disables the player's camera. Overlay UI still renders; use
            // a temporary camera to capture it without reactivating the animated player.
            Camera source = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(item => item.CompareTag("MainCamera"));
            temporaryCamera = new GameObject("Regression Capture Camera", typeof(Camera));
            camera = temporaryCamera.GetComponent<Camera>();
            if (source != null)
            {
                camera.CopyFrom(source);
                camera.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            }
            camera.enabled = false;
        }
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(canvas => canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        Camera[] oldCameras = canvases.Select(canvas => canvas.worldCamera).ToArray();
        float[] oldDistances = canvases.Select(canvas => canvas.planeDistance).ToArray();
        int width = Mathf.Max(Screen.width, 1);
        int height = Mathf.Max(Screen.height, 1);
        RenderTexture render = RenderTexture.GetTemporary(width, height, 24);
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        try
        {
            camera.targetTexture = render;
            foreach (Canvas canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = camera.nearClipPlane + 1;
            }
            yield return null;
            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text label in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
                label.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = render;
            Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();
            File.WriteAllBytes(path, screenshot.EncodeToPNG());
            Destroy(screenshot);
        }
        finally
        {
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceOverlay;
                canvases[i].worldCamera = oldCameras[i];
                canvases[i].planeDistance = oldDistances[i];
            }
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(render);
            if (temporaryCamera != null)
                Destroy(temporaryCamera);
        }
    }
}
