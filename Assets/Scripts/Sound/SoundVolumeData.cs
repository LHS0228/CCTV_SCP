using UnityEngine;

[CreateAssetMenu(fileName ="VolumeSettingData", menuName ="Custom/Setting/Volume")]
public class SoundVolumeData : ScriptableObject
{
    public float masterVolume = 0.5f;
    public float bgmVolume = 0.5f;
    public float sfxVolume = 0.5f;

}
