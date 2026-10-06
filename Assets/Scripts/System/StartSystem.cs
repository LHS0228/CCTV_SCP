using System.Collections;
using UnityEngine;

public class StartSystem : MonoBehaviour
{
    public static StartSystem instance;
    private Coroutine subtitleRoutine;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        GameManager.Instance.isGameStop = true;

        StartCoroutine(GameStartTimeline());
    }

    private IEnumerator GameStartTimeline()
    {
        SoundManager.Instance.PlayGlobalSFX(SoundManager.Instance.Data.ingameElevatorArriveDing);

        yield return new WaitForSecondsRealtime(5.5f);

        SoundManager.Instance.Play3DSFX(SoundManager.Instance.Data.ingameDoorOpenHydraulic, GameManager.Instance.anomalySystem.specialObjects[3].transform.position, 20, false);
        GameManager.Instance.anomalySystem.specialObjects[3].GetComponent<Animator>().Play("Open");

        yield return new WaitForSecondsRealtime(1.0f);

        float nextTime = 0;

        switch (DaySystem.Instance.GetNowDay())
        {
            case 1:
                nextTime = 8;
                SoundManager.Instance.PlayGlobalSFX(SoundManager.Instance.Data.RobotDay1);
                TriggerLocalizedVoiceText("voice.day1", nextTime);
                break;
            case 2:
                nextTime = 10;
                SoundManager.Instance.PlayGlobalSFX(SoundManager.Instance.Data.RobotDay2);
                TriggerLocalizedVoiceText("voice.day2", nextTime);
                break;
            case 3:
                nextTime = 8;
                SoundManager.Instance.PlayGlobalSFX(SoundManager.Instance.Data.RobotDay3);
                TriggerLocalizedVoiceText("voice.day3", nextTime);
                break;
            case 4:
                nextTime = 8;
                SoundManager.Instance.PlayGlobalSFX(SoundManager.Instance.Data.RobotDay4);
                TriggerLocalizedVoiceText("voice.day4", nextTime);
                break;
            case 5:
                nextTime = 8;
                SoundManager.Instance.PlayGlobalSFX(SoundManager.Instance.Data.RobotDay5);
                TriggerLocalizedVoiceText("voice.day5", nextTime);
                break;
            default:
                Debug.LogError("버그 남 날짜관련 버그 일단 StartSystem에서 난거니까 확인");
                break;
        }
        
        yield return new WaitForSecondsRealtime(nextTime);

        SoundManager.Instance.Play3DSFX(SoundManager.Instance.Data.ingameDoorOpenHydraulic, GameManager.Instance.anomalySystem.specialObjects[2].transform.position, 20, false);
        GameManager.Instance.anomalySystem.specialObjects[2].GetComponent<Animator>().Play("Open");
    }
    public void TriggerVoiceTextOnFunc(string text, float time)
    {
        ShowSubtitle(text, time, false);
    }
    public void TriggerLocalizedVoiceText(string key, float time)
    {
        ShowSubtitle(key, time, true);
    }

    private void ShowSubtitle(string text, float time, bool localized)
    {
        if (subtitleRoutine != null)
            StopCoroutine(subtitleRoutine);
        subtitleRoutine = StartCoroutine(VoiceTextOn(text, time, localized));
    }

    private IEnumerator VoiceTextOn(string text, float time, bool localized = false)
    {
        GameManager.Instance.voiceTextBox.SetActive(true);
        if (localized)
        {
            GameLocalization.SetText(GameManager.Instance.voiceText, text);
        }
        else
        {
            // Legacy callers can still display a literal subtitle without a stale locale binding.
            LocalizedTMPText binding = GameManager.Instance.voiceText.GetComponent<LocalizedTMPText>();
            if (binding != null)
                binding.SetReference(null);
            GameManager.Instance.voiceText.text = text;
        }

        yield return new WaitForSecondsRealtime(time);

        GameManager.Instance.voiceTextBox.SetActive(false);
        subtitleRoutine = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (DaySystem.Instance.GetNowDay() == 1) return;
        if (GameManager.Instance.isGameStart) return;

        GameManager.Instance.anomalySystem.specialObjects[3].GetComponent<Animator>().Play("Close");
        GameManager.Instance.anomalySystem.specialObjects[2].GetComponent<Animator>().Play("Close");

        SoundManager.Instance.Play3DSFX(SoundManager.Instance.Data.ingameDoorCloseHydraulic, GameManager.Instance.anomalySystem.specialObjects[3].transform.position, 20, false);
        SoundManager.Instance.Play3DSFX(SoundManager.Instance.Data.ingameDoorCloseHydraulic, GameManager.Instance.anomalySystem.specialObjects[2].transform.position, 20, false);
        GameManager.Instance.isGameStop = false;
        GameManager.Instance.isGameStart = true;
    }
}
