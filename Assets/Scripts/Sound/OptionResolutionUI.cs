using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 옵션 화면의 해상도 드롭다운과 전체화면 토글을 ResolutionSettings에 연결하는 책임을 가진다.
/// </summary>
public sealed class OptionResolutionUI : MonoBehaviour
{
    [SerializeField] private ResolutionSettings resolutionSettings;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullScreenToggle;

    private bool isInitializing;

    private void Awake()
    {
        if (resolutionSettings == null)
        {
            resolutionSettings = ResolutionSettings.Instance != null
                ? ResolutionSettings.Instance
                : FindFirstObjectByType<ResolutionSettings>();
        }
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (resolutionSettings == null)
        {
            Debug.LogWarning("OptionResolutionUI: ResolutionSettings 참조가 없습니다.");
            return;
        }

        isInitializing = true;
        RefreshResolutionDropdown();
        RefreshFullScreenToggle();
        isInitializing = false;
    }

    public void OnResolutionChanged(int presetIndex)
    {
        if (isInitializing || resolutionSettings == null)
        {
            return;
        }

        resolutionSettings.ApplyPresetIndex(presetIndex);
    }

    public void OnFullScreenChanged(bool isFullScreen)
    {
        if (isInitializing || resolutionSettings == null)
        {
            return;
        }

        resolutionSettings.SetFullScreen(isFullScreen);
    }

    private void RefreshResolutionDropdown()
    {
        if (resolutionDropdown == null)
        {
            return;
        }

        IReadOnlyList<ResolutionPreset> presets = resolutionSettings.AvailablePresets;
        List<string> labels = new List<string>(presets.Count);

        for (int i = 0; i < presets.Count; i++)
        {
            labels.Add(presets[i].Label);
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(labels);
        resolutionDropdown.value = resolutionSettings.GetSavedPresetIndex();
        resolutionDropdown.RefreshShownValue();
    }

    private void RefreshFullScreenToggle()
    {
        if (fullScreenToggle == null)
        {
            return;
        }

        fullScreenToggle.isOn = resolutionSettings.IsFullScreen;
    }
}
