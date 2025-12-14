using UnityEngine;
using UnityEngine.UI;

public class OptionSoundUI : MonoBehaviour
{
    [SerializeField]    SoundVolumeData volumeData;
    [SerializeField]    Slider masterVolumeSlider;
    [SerializeField]    Slider bgmVolumeSlider;
    [SerializeField]    Slider sfxVolumeSlider;

    private void OnEnable()
    {
        masterVolumeSlider.value = volumeData.masterVolume;
        bgmVolumeSlider.value = volumeData.bgmVolume;
        sfxVolumeSlider.value = volumeData.sfxVolume;
    }
}
