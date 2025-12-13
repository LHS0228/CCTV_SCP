using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Title_End_System : MonoBehaviour
{
    [SerializeField] private GameObject protocolDoor;
    [SerializeField] private GameObject elevatorDoor;
    [SerializeField] private GameObject playerCamera;

    private int enddingCount = 0;

    private void Start()
    {
        ExecutionTimeLineManager.instance.PlayDayTimeline(3);
    }

    public void EnddingAnimationCountingEvent()
    {
        switch(enddingCount)
        {
            case 0:
                protocolDoor.GetComponent<Animator>().Play("Open");
                SoundManager.Instance.Play3DSFX(SoundManager.Instance.Data.ingameDoorOpenHydraulic, protocolDoor.transform.position, 10, false);
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
                SoundManager.Instance.PlayGlobalSFX(SoundManager.Instance.Data.EDDelete);
                break;

            case 6:
                SoundManager.Instance.Play3DSFX(SoundManager.Instance.Data.EDElevatorcrack, elevatorDoor.transform.position, 10, true);
                StartCoroutine(CameraShake(0.05f, 200));
                break;
            case 7:
                SoundManager.Instance.StopAllGlobalSFX();
                SoundManager.Instance.PlayGlobalSFX(SoundManager.Instance.Data.EDElevatorboom);
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
        Application.Quit();
    }
}
