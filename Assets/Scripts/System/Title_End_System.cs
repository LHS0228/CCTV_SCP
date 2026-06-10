using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 타이틀과 엔딩 화면의 버튼 입력, 타임라인 이벤트, 종료 확인 UI를 관리하는 책임을 가진다.
/// </summary>
public class Title_End_System : MonoBehaviour
{
    [SerializeField] private GameObject protocolDoor;
    [SerializeField] private GameObject elevatorDoor;
    [SerializeField] private GameObject playerCamera;
    [SerializeField] private TextMeshProUGUI voiceText;
    [SerializeField] private GameObject exitConfirmPopup;

    private AudioSource saveAudio;

    private int enddingCount = 0;

    private void Start()
    {
        ExecutionTimeLineManager.instance.PlayDayTimeline(3);
        if (exitConfirmPopup != null)
        {
            exitConfirmPopup.SetActive(false);
        }
    }

    public void EnddingAnimationCountingEvent()
    {
        switch(enddingCount)
        {
            case 0:
                protocolDoor.GetComponent<Animator>().Play("Open");
                SoundManager.Instance.Play3DSFX(SoundManager.Instance.Data.ingameDoorOpenHydraulic, protocolDoor.transform.position, 10, false);
                voiceText.text = "관리자32, 당신의 업무 기간이 종료되었습니다. 축하합니다.";
                Debug.Log("실행됨");
                break;

            case 1:
                SoundManager.Instance.PlayGlobalSFX(SoundManager.Instance.Data.EDCongratulation);
                break;

            case 2:
                protocolDoor.GetComponent<Animator>().Play("Close");
                SoundManager.Instance.Play3DSFX(SoundManager.Instance.Data.ingameDoorCloseHydraulic, protocolDoor.transform.position, 10, false);
                break;

            case 3:
                elevatorDoor.GetComponent<Animator>().Play("Open");
                SoundManager.Instance.Play3DSFX(SoundManager.Instance.Data.ingameDoorOpenHydraulic, elevatorDoor.transform.position, 10, false);
                break;

            case 4:
                elevatorDoor.GetComponent<Animator>().Play("Close");
                SoundManager.Instance.Play3DSFX(SoundManager.Instance.Data.ingameDoorOpenHydraulic, elevatorDoor.transform.position, 10, false);
                break;

            case 5:
                voiceText.text = "기밀 누설 가능성 확인. 말소를 진행합니다.";
                SoundManager.Instance.PlayGlobalSFX(SoundManager.Instance.Data.EDDelete);
                break;

            case 6:
                saveAudio = SoundManager.Instance.Play3DSFX(SoundManager.Instance.Data.EDElevatorcrack, elevatorDoor.transform.position, 10, true);
                StartCoroutine(CameraShake(0.05f, 200));
                break;
            case 7:
                SoundManager.Instance.StopSFX(saveAudio);
                SoundManager.Instance.PlayGlobalSFX(SoundManager.Instance.Data.EDElevatorboom);
                break;

            case 8:
                Cursor.lockState = CursorLockMode.None; // 자유롭게 이동
                Cursor.visible = true; // 커서 보임
                break;
            default:
                Debug.Log("Error:지정되어있지 않은 문");
                break;
        }

        enddingCount++;
    }
    

    private IEnumerator CameraShake(float time, int loopValue)
    {
        for (int i = 0; i < loopValue; i++)
        {
            Vector3 orgin = new Vector3(0, -0.73f, 0);

            playerCamera.transform.position += new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f));
            yield return new WaitForSecondsRealtime(time);
            playerCamera.transform.localPosition = orgin;
        }
    }

    public void StartButton()
    {
        SceneManager.LoadScene(0); 
    }

    public void RePlay()
    {
        SceneManager.LoadScene(1);
    }

    public void EndGame()
    {
        ShowExitConfirmPopup();
    }

    public void ShowExitConfirmPopup()
    {
        if (exitConfirmPopup == null)
        {
            Debug.LogWarning("Exit confirm popup is not assigned.");
            return;
        }

        exitConfirmPopup.SetActive(true);
    }

    public void HideExitConfirmPopup()
    {
        if (exitConfirmPopup == null)
        {
            Debug.LogWarning("Exit confirm popup is not assigned.");
            return;
        }

        exitConfirmPopup.SetActive(false);
    }

    public void ConfirmExitGame()
    {
        Application.Quit();
    }
}
