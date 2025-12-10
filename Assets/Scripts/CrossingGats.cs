using System.Collections;
using UnityEngine;

public class CrossingGats : MonoBehaviour
{
    [SerializeField, Header("텍스트 가이드")]
    private GameObject guide;
    private bool isClick = false;

    [Header("담당 불빛")]
    public Light mainLight;
    [SerializeField] LightOutAnomaly lightOutAnomaly;

    private void Awake()
    {
        lightOutAnomaly.SetCrossingGats(gameObject);
    }

   public void TryActionEvent()
    {
        if (GameManager.Instance.isGameStop == true)
            return;
        if (isClick == true) return;
        StartCoroutine(ActionEvent());
    }


    private IEnumerator ActionEvent()
    {
        isClick = true;

        gameObject.GetComponent<Animator>().SetBool("isAction", true);

        if (gameObject.GetComponent<Animator>().GetBool("isShotDown"))
        {
            yield return new WaitForSecondsRealtime(1.2f);
            GameManager.Instance.anomalySystem.ClearMission(2);

            yield return new WaitForSecondsRealtime(1.0f);

            gameObject.GetComponent<Animator>().SetBool("isAction", false);
            gameObject.GetComponent<Animator>().SetBool("isShotDown", false);

            yield return new WaitForSecondsRealtime(1.0f);
        }

        else
        {
            yield return new WaitForSecondsRealtime(1.0f);
            gameObject.GetComponent<Animator>().SetBool("isAction", false);
        }
        
        isClick = false;
    }

    public bool GuideOff()
    {
        return isClick;
    }
}
