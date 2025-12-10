using UnityEngine;

public class ProtocolSystem : MonoBehaviour
{
    public static ProtocolSystem instance;

    [SerializeField, Header("게임오버 카운터 다운 시간(초)")]
    private float protocol_CountTime = 20;
    public bool protocol_Activated = false; //프로토콜 작동!
    public bool protocol_FinalChased = false; //한번 찬스가 발동했나요?

    //한번 실행했는지 검사하는 거.
    private bool isWarringStartEventCheck = false;
    private bool isDeadEventCheck = false;

    [SerializeField, Header("인식 박스")]
    private BoxCollider checkCollider;

    [SerializeField, Header("키패드")]
    private KeyPad protocolKeyPad;

    bool isTriggerWd = false;

    AudioSource saveSound;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    int cachedIndex = 0;

    public void StartProtocol(int index)
    {
        cachedIndex = index; // 죽어야 할 대상(인덱스) 캐싱

        // [수정된 부분] 
        // 이미 기회를 사용했다면 (Second Chance Used) -> 즉시 사망 처리하고 함수 종료
        if (protocol_FinalChased)
        {
            Protocal_GameOver();
            return; // 중요: 여기서 return을 해야 아래의 사이렌 소리나 타이머가 실행되지 않습니다.
        }

        // --- 기회가 남아있을 때만 아래 코드가 실행됨 ---

        // 사이렌 소리 재생 (죽을 때는 사이렌 울릴 필요 없이 바로 타임라인 재생하므로 아래로 내림)
        saveSound = SoundManager.Instance.Play3DSFX(SoundManager.Instance.Data.deathCommonSirenLoopBeforeDeath, GameManager.Instance.anomalySystem.specialObjects[1].transform.position, 20, true);

        protocol_FinalChased = true; // 기회 사용 처리
        protocol_Activated = true;   // Update문의 카운트다운 시작
    }

    // Update is called once per frame
    void Update()
    {
        //여기서 게임오버 처리해야할 듯
        if (!protocol_Activated) return;

        // 위험 상태 확인 1번만 작동하는거 다 넣어
        if (!isWarringStartEventCheck)
        {
            SoundManager.Instance.PlayGlobalSFX(SoundManager.Instance.Data.deathCommonStabilityZeroGeneratorExplode);
            SoundManager.Instance.Play3DSFX(SoundManager.Instance.Data.ingameDoorOpenHydraulic, GameManager.Instance.anomalySystem.specialObjects[2].transform.position, 20, false);

            // 1, 2. 문열기, 여기에 워링 이펙트 활성화 (불빛 난다거나 하는거)
            GameManager.Instance.anomalySystem.specialObjects[2].GetComponent<Animator>().Play("Open");
            GameManager.Instance.anomalySystem.specialObjects[1].GetComponent<Animator>().Play("On");

            SoundManager.Instance.Play3DSFX(SoundManager.Instance.Data.ingameDoorOpenHydraulic, GameManager.Instance.anomalySystem.specialObjects[2].transform.position, 20, false);

            GameManager.Instance.isGameStop = true;
            GameManager.Instance.isDeadWarring = true;
            isWarringStartEventCheck = true;
        }

        //카운터 다운.
        if (GameManager.Instance.isDeadWarring) DeadWarringCount(1);
    }

    /// <summary>
    /// 프로토콜 누를 수 있는 시간제한 카운터 다운
    /// </summary>
    /// <param name="roomNum"></param>
    private void DeadWarringCount(int roomNum)
    {
        protocol_CountTime -= Time.deltaTime;
        if (protocol_CountTime > 0) return;

        if (isDeadEventCheck) return;
        Protocal_GameOver();
        //게임 오버 만들거면 여기에 만들어줘
        //혹시 쓸 수 있으니까 매개 변수 만들어두긴 함, 안쓰면 버려.
    }

    /// <summary>
    /// 플레이어가 다시 자기 방으로 돌아왔을 때 실행시키는 코드
    /// </summary>
    public void Protocol_ComeBack()
    {
        protocol_Activated = true;
        GameManager.Instance.anomalySystem.specialObjects[2].GetComponent<Animator>().Play("Close");
        SoundManager.Instance.Play3DSFX(SoundManager.Instance.Data.ingameDoorCloseHydraulic, GameManager.Instance.anomalySystem.specialObjects[2].transform.position, 20, false);
        GameManager.Instance.isGameStop = false;
    }

    /// <summary>
    /// 타임오버 혹은 2번째 기회 박탈로 인한 게임 종료
    /// </summary>
    private void Protocal_GameOver()
    {
        Debug.Log("게임 오버");

        // 중복 실행 방지
        if (isDeadEventCheck) return;
        isDeadEventCheck = true;

        // 게임 멈춤 상태 해제 (타임라인이 원활하게 돌도록, 필요시 false/true 조정)
        // 보통 타임라인이 카메라를 제어하므로 게임 로직은 멈추는게 맞음
        GameManager.Instance.isGameStop = true;
        GameManager.Instance.isDeadWarring = false; // 카운트다운 로직 정지
        protocol_Activated = false; // 프로토콜 로직 정지

        // 사이렌 소리가 있다면 끄기
        StopProtocol();

        // 캐싱된 인덱스의 몬스터 처형 타임라인 재생
        ExecutionTimeLineManager.instance.PlayExecutionTimeline(cachedIndex);
    }

    public void StopProtocol()
    {
        if (saveSound != null)
            Destroy(saveSound);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggerWd) return;

        if (!protocolKeyPad.GetSucess()) return;

        if (other.gameObject.tag != "Player") return;

        Protocol_ComeBack();
        isTriggerWd = true;
    }
}
