using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    bool isOptionMode = false;
    public bool IsOptionMode => isOptionMode;

    //게임 시작했나요?
    [HideInInspector]
    public bool isGameStart = false;


    [Header("세팅 해야되는 시스템들")]
    public AnomalySystem anomalySystem;
    public GameObject player;
    public GameObject voiceTextBox;
    public TextMeshProUGUI voiceText;

    public bool isGameStop;
    [HideInInspector] public bool isTimeStop;
    [HideInInspector] public bool isDeadWarring;

    [Header("미상개체 방 빛 모음집")]
    public Light[] lights;

    [SerializeField, Header("메뉴창")]
    private GameObject optionMenu;

    [Header("프로토콜 비밀번호")]
    public int protocolNum;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (optionMenu.activeSelf)
            {
                player.GetComponent<PlayerMove>().isStop = false;
                OptionOff();
                optionMenu.SetActive(false);
            }
            else
            {
                player.GetComponent<PlayerMove>().isStop = true;
                OptionOn();
                optionMenu.SetActive(true);
            }
        }
    }

    private void Start()
    {
        protocolNum = Random.Range(1000, 9999);
        ExecutionTimeLineManager.instance.PlayDayTimeline(0);
        if (SceneManager.GetActiveScene().buildIndex == 1)//GameSceneIndex
        {
            SoundManager.Instance?.PlayBGM(SoundManager.Instance.Data.systemUiBGMIngame);
        }
    }

    /// <summary>
    /// 게임이 멈췄거나 컷씬 애니메이션 재생과 같은 시간 일시정지 상태인가요?
    /// </summary>
    /// <returns></returns>
    public bool AllStopCheck()
    {
        if (isGameStop || isTimeStop) { return true; }
        else return false;
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(0);
    }

    public void OptionOn()
    { isOptionMode = true; }
    public void OptionOff()
    { isOptionMode = false; }
}
