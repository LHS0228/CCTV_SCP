using System.Collections;
using UnityEngine;

public class StartSystem : MonoBehaviour
{
    public static StartSystem instance;
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
                StartCoroutine(VoiceTextOn("이전 관리 기록 말소, 신입 관리자 번호 배정. 출근을 환영합니다, 관리자 32님.", nextTime));
                break;
            case 2:
                nextTime = 10;
                SoundManager.Instance.PlayGlobalSFX(SoundManager.Instance.Data.RobotDay2);
                StartCoroutine(VoiceTextOn("출근을 환영합니다, 관리자 32님. 회사의 자산은 언제나 직원의 안전보다 우선되는 것을 명심하십시오.", nextTime));
                break;
            case 3:
                nextTime = 8;
                SoundManager.Instance.PlayGlobalSFX(SoundManager.Instance.Data.RobotDay3);
                StartCoroutine(VoiceTextOn("출근을 환영합니다, 관리자 32님. 알수없는 원인으로 개체가 불안정해졌으니 주의하십시오.", nextTime));
                break;
            case 4:
                nextTime = 8;
                SoundManager.Instance.PlayGlobalSFX(SoundManager.Instance.Data.RobotDay4);
                StartCoroutine(VoiceTextOn("출근을 환영합니다, 관리자 32님. 자산에 손실이 일어나면 즉시 해고되니 주의하십시오.", nextTime));
                break;
            case 5:
                nextTime = 8;
                SoundManager.Instance.PlayGlobalSFX(SoundManager.Instance.Data.RobotDay5);
                StartCoroutine(VoiceTextOn("출근을 환영합니다, 관리자 32님. 마지막까지 완벽하게 근무하십시오.", nextTime));
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
        StartCoroutine(VoiceTextOn(text, time));
    }
    private IEnumerator VoiceTextOn(string text, float time)
    {
        GameManager.Instance.voiceTextBox.SetActive(true);
        GameManager.Instance.voiceText.text = text;

        yield return new WaitForSecondsRealtime(time);

        GameManager.Instance.voiceTextBox.SetActive(false);
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
