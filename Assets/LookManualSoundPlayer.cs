using UnityEngine;

public class LookManualSoundPlayer : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (DaySystem.Instance?.GetNowDay() == 1)
        {
            
        SoundManager.Instance?.PlayGlobalSFX(SoundManager.Instance.Data.systemUiManualAlarm);
        StartSystem.instance?.TriggerVoiceTextOnFunc("메뉴얼을 확인하고 업무 정보를 확인하십시오.", 4);
        }
    }
}
