using System.Collections;
using UnityEngine;

public class FeedCetner : MonoBehaviour
{
    [SerializeField, Header("텍스트 가이드")]
    private GameObject guide;
    [SerializeField]
    Anomaly_TentacleOvergrowth anomaly;
    private bool isClick = false;


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

        yield return new WaitForSecondsRealtime(1.0f);

        GameManager.Instance.anomalySystem.ClearCCTVFoodRefeel(0);
        yield return new WaitForSecondsRealtime(1.0f);

        gameObject.GetComponent<Animator>().SetBool("isAction", false);

        yield return new WaitForSecondsRealtime(1.0f);

        isClick = false;
    }

    public bool GuideOff()
    {
        return isClick;
    }
}
