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
        StartSystem.instance?.TriggerVoiceTextOnFunc("메뉴얼을 확인하고 업무 정보를 확인하십시오.", 4);
        }
    }
}
