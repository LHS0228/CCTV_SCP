using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISystem : MonoBehaviour
{
    [Header("버티기 게이지")]
    public Slider[] anomalyGageBar;

    [Header("cctv gage slider")]
    public Slider[] cctvGageBar;

    [Header("cctv gage text")]
    public TextMeshProUGUI[] cctvGageInfoText;

    [Header("버튼 열어")]
    public GameObject[] CCTV_SubButtonSet;

    [Header("FadeIn Day 텍스트s 업데이트")]
    public TextMeshProUGUI startDayText;
    public TextMeshProUGUI clearDayText;

    private void Start()
    {
        GameLocalization.SetText(startDayText, "ui.day", DaySystem.Instance.GetNowDay());
        GameLocalization.SetText(clearDayText, "ui.dayClear", DaySystem.Instance.GetNowDay());
    }

    private void Update()
    {
        //나머지는 나중에 추가 1,2는
        for (int i = 0; i < anomalyGageBar.Length; i++)
        {
            anomalyGageBar[i].value = StabilityManager.Instance.CurrentStability[i] / 100;
            cctvGageBar[i].value = anomalyGageBar[i].value;

            float cctvgagetext = anomalyGageBar[i].value * 100;
            cctvGageInfoText[i].text = Mathf.RoundToInt(cctvgagetext).ToString() + "%";
        }
    }

    public void SubButtonOpen(int index)
    {
        if (CCTV_SubButtonSet[index].activeSelf)
            CCTV_SubButtonSet[index].SetActive(false);
        else
            CCTV_SubButtonSet[index].SetActive(true);
    }
}
