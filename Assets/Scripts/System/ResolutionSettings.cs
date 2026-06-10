using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 화면을 16:9 해상도 프리셋 안에서만 변경하고 선택값을 저장/복원하는 책임을 가진다.
/// </summary>
public sealed class ResolutionSettings : MonoBehaviour
{
    private const string ResolutionWidthKey = "ResolutionSettings.Width";
    private const string ResolutionHeightKey = "ResolutionSettings.Height";
    private const string FullScreenKey = "ResolutionSettings.FullScreen";

    [SerializeField] private List<ResolutionPreset> presets = new List<ResolutionPreset>
    {
        new ResolutionPreset(1280, 720),
        new ResolutionPreset(1600, 900),
        new ResolutionPreset(1920, 1080),
        new ResolutionPreset(2560, 1440)
    };

    [SerializeField] private bool applyOnAwake = true;
    [SerializeField] private bool defaultFullScreen = true;
    [SerializeField] private FullScreenMode fullScreenMode = FullScreenMode.ExclusiveFullScreen;
    [SerializeField] private FullScreenMode windowedMode = FullScreenMode.Windowed;

    private List<ResolutionPreset> availablePresets;

    public static ResolutionSettings Instance { get; private set; }

    public IReadOnlyList<ResolutionPreset> AvailablePresets
    {
        get
        {
            EnsureAvailablePresets();
            return availablePresets;
        }
    }

    public bool IsFullScreen => PlayerPrefs.GetInt(FullScreenKey, defaultFullScreen ? 1 : 0) == 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAvailablePresets();

        if (applyOnAwake)
        {
            ApplySavedSettings();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateDefaultInstance()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject resolutionSettingsObject = new GameObject(nameof(ResolutionSettings));
        resolutionSettingsObject.AddComponent<ResolutionSettings>();
    }

    public void ApplyPresetIndex(int presetIndex)
    {
        EnsureAvailablePresets();

        if (availablePresets.Count == 0)
        {
            Debug.LogWarning("ResolutionSettings: 사용할 수 있는 16:9 해상도 프리셋이 없습니다.");
            return;
        }

        int safeIndex = Mathf.Clamp(presetIndex, 0, availablePresets.Count - 1);
        ResolutionPreset preset = availablePresets[safeIndex];

        SaveResolution(preset.Width, preset.Height);
        ApplyResolution(preset.Width, preset.Height, IsFullScreen);
    }

    public void SetFullScreen(bool isFullScreen)
    {
        PlayerPrefs.SetInt(FullScreenKey, isFullScreen ? 1 : 0);
        PlayerPrefs.Save();

        ResolutionPreset preset = GetSavedPreset();
        ApplyResolution(preset.Width, preset.Height, isFullScreen);
    }

    public void ApplySavedSettings()
    {
        ResolutionPreset preset = GetSavedPreset();
        ApplyResolution(preset.Width, preset.Height, IsFullScreen);
    }

    public int GetSavedPresetIndex()
    {
        EnsureAvailablePresets();

        ResolutionPreset savedPreset = GetSavedPreset();
        for (int i = 0; i < availablePresets.Count; i++)
        {
            if (availablePresets[i].Matches(savedPreset.Width, savedPreset.Height))
            {
                return i;
            }
        }

        return GetFallbackPresetIndex();
    }

    public string GetPresetLabel(int presetIndex)
    {
        EnsureAvailablePresets();

        if (presetIndex < 0 || presetIndex >= availablePresets.Count)
        {
            return string.Empty;
        }

        return availablePresets[presetIndex].Label;
    }

    private void EnsureAvailablePresets()
    {
        if (availablePresets != null)
        {
            return;
        }

        availablePresets = new List<ResolutionPreset>();

        foreach (ResolutionPreset preset in presets)
        {
            if (preset == null || !preset.IsValid16By9)
            {
                continue;
            }

            if (!HasDisplayResolutionList() || IsSupportedByDisplay(preset))
            {
                AddPresetIfMissing(preset);
            }
        }

        if (availablePresets.Count == 0)
        {
            AddPresetIfMissing(new ResolutionPreset(1920, 1080));
        }
    }

    private ResolutionPreset GetSavedPreset()
    {
        EnsureAvailablePresets();

        int width = PlayerPrefs.GetInt(ResolutionWidthKey, 0);
        int height = PlayerPrefs.GetInt(ResolutionHeightKey, 0);

        for (int i = 0; i < availablePresets.Count; i++)
        {
            if (availablePresets[i].Matches(width, height))
            {
                return availablePresets[i];
            }
        }

        int fallbackIndex = GetFallbackPresetIndex();
        ResolutionPreset fallbackPreset = availablePresets[fallbackIndex];
        SaveResolution(fallbackPreset.Width, fallbackPreset.Height);
        return fallbackPreset;
    }

    private int GetFallbackPresetIndex()
    {
        EnsureAvailablePresets();

        int screenWidth = Screen.width;
        int screenHeight = Screen.height;

        for (int i = 0; i < availablePresets.Count; i++)
        {
            if (availablePresets[i].Matches(screenWidth, screenHeight))
            {
                return i;
            }
        }

        for (int i = availablePresets.Count - 1; i >= 0; i--)
        {
            if (availablePresets[i].Width <= Screen.currentResolution.width &&
                availablePresets[i].Height <= Screen.currentResolution.height)
            {
                return i;
            }
        }

        return 0;
    }

    private void ApplyResolution(int width, int height, bool isFullScreen)
    {
        FullScreenMode mode = isFullScreen ? fullScreenMode : windowedMode;
        Screen.SetResolution(width, height, mode);
    }

    private void SaveResolution(int width, int height)
    {
        PlayerPrefs.SetInt(ResolutionWidthKey, width);
        PlayerPrefs.SetInt(ResolutionHeightKey, height);
        PlayerPrefs.Save();
    }

    private void AddPresetIfMissing(ResolutionPreset preset)
    {
        for (int i = 0; i < availablePresets.Count; i++)
        {
            if (availablePresets[i].Matches(preset.Width, preset.Height))
            {
                return;
            }
        }

        availablePresets.Add(new ResolutionPreset(preset.Width, preset.Height));
    }

    private static bool HasDisplayResolutionList()
    {
        return Screen.resolutions != null && Screen.resolutions.Length > 0;
    }

    private static bool IsSupportedByDisplay(ResolutionPreset preset)
    {
        Resolution[] supportedResolutions = Screen.resolutions;

        for (int i = 0; i < supportedResolutions.Length; i++)
        {
            if (supportedResolutions[i].width == preset.Width &&
                supportedResolutions[i].height == preset.Height)
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// UI에 노출할 수 있는 단일 16:9 해상도 값을 표현하는 책임을 가진다.
/// </summary>
[Serializable]
public sealed class ResolutionPreset
{
    [SerializeField] private int width;
    [SerializeField] private int height;

    public ResolutionPreset(int width, int height)
    {
        this.width = width;
        this.height = height;
    }

    public int Width => width;
    public int Height => height;
    public string Label => $"{width} x {height}";
    public bool IsValid16By9 => width > 0 && height > 0 && width * 9 == height * 16;

    public bool Matches(int targetWidth, int targetHeight)
    {
        return width == targetWidth && height == targetHeight;
    }
}
