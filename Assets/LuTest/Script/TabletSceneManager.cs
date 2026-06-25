using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 태블릿 메뉴 패널 전환과 미니게임 시작/복귀 흐름을 관리하는 책임을 가진다.
/// </summary>
public class TabletSceneManager : MonoBehaviour
{
    [SerializeField]
    private Button[] startBts;

    [SerializeField]
    public GameObject[] tabletPanels;

    [SerializeField]
    private Button9 button9;

    [SerializeField]
    private AJae aJae;

    [SerializeField]
    private Fishing fishing;

    public bool isPlaying = false;

    private int currentPanelIndex = 1;
    private int currentGameIndex = -1;

    private void Awake()
    {
        for (int i = 0; i < startBts.Length; i++)
        {
            int panelBtIndex = i;
            if (startBts[panelBtIndex] != null)
            {
                startBts[panelBtIndex].onClick.AddListener(() => OnStartButton(panelBtIndex));
            }
        }

        tabletPanels[0].transform.Find("RightButton").GetComponent<Button>().onClick.AddListener(() => SwitchPanel(tabletPanels[1]));
        tabletPanels[1].transform.Find("LeftButton").GetComponent<Button>().onClick.AddListener(() => SwitchPanel(tabletPanels[0]));
        tabletPanels[1].transform.Find("RightButton").GetComponent<Button>().onClick.AddListener(() => SwitchPanel(tabletPanels[2]));
        tabletPanels[2].transform.Find("LeftButton").GetComponent<Button>().onClick.AddListener(() => SwitchPanel(tabletPanels[1]));
    }

    private void Start()
    {
        ShowPanel(1);
    }

    private void SwitchPanel(GameObject switchPanel)
    {
        for (int i = 0; i < tabletPanels.Length; i++)
        {
            if (tabletPanels[i] == switchPanel)
            {
                ShowPanel(i);
                return;
            }
        }
    }

    private void OnStartButton(int panelBtIndex)
    {
        GameStart(panelBtIndex);
    }

    private void GameStart(int panelBtIndex)
    {
        isPlaying = true;
        currentGameIndex = panelBtIndex;

        if (panelBtIndex == 0)
        {
            aJae.StartAJae();
            tabletPanels[0].SetActive(false);
        }
        else if (panelBtIndex == 1)
        {
            button9.StartButton9();
            tabletPanels[1].SetActive(false);
        }
        else if (panelBtIndex == 2)
        {
            fishing.StartFishing();
            tabletPanels[2].SetActive(false);
        }

        Debug.Log($"isPlayer : {isPlaying} & PanelNumber : {panelBtIndex + 1}");
    }

    public bool TryHandleBackInput()
    {
        if (!isPlaying)
        {
            return false;
        }

        EndCurrentGame();
        return true;
    }

    public void ReturnToPanel(int panelIndex)
    {
        isPlaying = false;
        currentGameIndex = -1;
        ShowPanel(panelIndex);
    }

    private void ShowPanel(int panelIndex)
    {
        if (tabletPanels == null || panelIndex < 0 || panelIndex >= tabletPanels.Length)
        {
            return;
        }

        for (int i = 0; i < tabletPanels.Length; i++)
        {
            tabletPanels[i].SetActive(i == panelIndex);
        }

        currentPanelIndex = panelIndex;
    }

    private void EndCurrentGame()
    {
        switch (currentGameIndex)
        {
            case 0:
                aJae.EndGame();
                break;
            case 1:
                button9.EndGame();
                break;
            case 2:
                fishing.EndGame();
                break;
            default:
                ReturnToPanel(currentPanelIndex);
                break;
        }
    }
}
