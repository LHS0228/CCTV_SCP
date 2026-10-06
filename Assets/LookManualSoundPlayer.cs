using UnityEngine;

public class LookManualSoundPlayer : MonoBehaviour
{
    bool isFirst = true;
    private void OnTriggerEnter(Collider other)
    {
        if (DaySystem.Instance?.GetNowDay() == 1 && isFirst)
        {
            isFirst = false;
        SoundManager.Instance?.PlayGlobalSFX(SoundManager.Instance.Data.systemUiManualAlarm);
        StartSystem.instance?.TriggerLocalizedVoiceText("voice.manual", 4);
        }
    }
}
