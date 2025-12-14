using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Fishing : MonoBehaviour
{
    [SerializeField]
    private GameObject fishingPanel; // �̴� ���� �г�

    [SerializeField]
    private RectTransform gaugeBarTransform; // ������ �� ��ġ
    
    [SerializeField]
    private RectTransform successZoneTransform; // ���� ���� ��ġ

    [SerializeField]
    private RectTransform playerMakerTransform; // �÷��̾� ��Ŀ ��ġ

    [SerializeField]
    private TextMeshProUGUI Timer; // Ÿ�̸� �ؽ�Ʈ

    [SerializeField]
    private TextMeshProUGUI Progress; // ���� ������ �ִ� �ð� �ؽ�Ʈ

    private Rect gaugeBarRect; // width�� ��� <- [������Ʈ ���� ������]
    private Rect successZoneRect; // width�� ���

    private float makerspeed = 0.5f; // ��Ŀ ���ǵ�
    private float gravity = 0.8f; // �����̽� �� �ȴ����� �� �������� �ӵ�

    private float totalTime = 10f; // �� �ð� 
    private float successTime = 3f; // ������ �ʿ��� �ð�
    private float progressTime = 0f; // ���� ������ �󸶳� �־����� Ȯ��
    private float curTime = 0f; // ���� �ð� �׳� �̰� Ÿ�̸Ӷ� ������
    private float playerVelocity = 0f; // �÷��̾� �ӵ� <- ��Ŀ�� ������

    private bool isReeling = false; // Ű ������ ������
    private bool isFishingPlaying = false; // �̴ϰ��� ���ο� ����

    [SerializeField]
    TabletSceneManager tabletSceneManager;

    [SerializeField]
    private Button exitButton;

    private void Awake()
    {
        exitButton.onClick.AddListener(() => ClickExitButton());
    }

    public void StartFishing()
    {
        if(tabletSceneManager.isPlaying)
        {
            // �̴ϰ��� ���۽� �г� Ȱ��ȭ �� ���� ����
            fishingPanel.SetActive(true);
            isFishingPlaying = true;

            // �̰� �˰��� �� ���� �ʱ�ȭ ����
            curTime = totalTime;
            progressTime = 0f;
            playerVelocity = 0f;

            GetDay();
            SetDifficultyLevel();

            // ���� ���� ��ġ �Լ�
            SuccessZonePlacement();
        }
    }

    private int GetDay()
    {
        return DaySystem.Instance.GetNowDay();
    }
    private void SetDifficultyLevel()
    {
        int day = GetDay();

        float zoneRange = 30f;
        float makerVelocity = 1.5f;
        float gravityVelocity = 7f;

        if (day == 1) { zoneRange = 15f; }
        else if (day == 2) { zoneRange = 15f; }//makerVelocity = 5f; //gravityVelocity = 5f; }
        else if (day == 3) { zoneRange = 12f; }//makerVelocity = 8f; //gravityVelocity = 7f; }
        else if (day == 4) { zoneRange = 12f; }//makerVelocity = 8f; //gravityVelocity = 7f; }
        else if (day >= 5) { zoneRange = 10f; }//makerVelocity = 9f; //gravityVelocity = 10f; }

            makerspeed = makerVelocity * 0.1f;
            gravity = gravityVelocity * 0.1f;

            float totalWidth = gaugeBarRect.width > 0 ? gaugeBarRect.width : 500f;

            float newWidth = totalWidth * (zoneRange / 100f);

            successZoneTransform.sizeDelta = new Vector2(newWidth, successZoneTransform.sizeDelta.y);

            successZoneRect = successZoneTransform.rect;

    }
    private void Start()
    {
        // ���� ���� �� �г� ��Ȱ��ȭ �� �̴ϰ��ӿ� ���� ������Ʈ�� �� ��������
        fishingPanel.SetActive(false);
        // �ش� �������� �������ִ� ũ�Ⱚ �ֱ�
        gaugeBarRect = gaugeBarTransform.rect;
        successZoneRect = successZoneTransform.rect;
    }

    private void Update()
    {
        if(fishingPanel.activeSelf)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                EndGame();
            }
        }

        if (!isFishingPlaying) return;

        // �Ź� �θ��� �Լ� - �ð� Ȯ��, Ű ����, ���� ���� �ð� Ȯ��
        CheckTimer();
        PressSpaceBar();
        CheckSuccessTime();
    }

    // ������ ���� ���� ���� ��ġ �Լ�
    private void SuccessZonePlacement()
    {
        float zoneHalfWidth = successZoneRect.width / 2;
        // �Ʊ� �޾ƿ� ������ �� ũ���� x�� �ּҰ� �ִ밪 ���� �״��� �������� �� �θ���
        float minX = gaugeBarRect.xMin + zoneHalfWidth;
        float maxX = gaugeBarRect.xMax - zoneHalfWidth;
        float randomX = Random.Range(minX, maxX);

        if(minX > maxX)
        {
            minX = 0f;
            maxX = 0f;
        }

        // ���� ������ Pos X�� ���� ����x �� ��ġ
        successZoneTransform.anchoredPosition = new Vector2(randomX, successZoneTransform.anchoredPosition.y);
        // ��� �߾� ����
        playerMakerTransform.anchoredPosition = new Vector2(0f, playerMakerTransform.anchoredPosition.y);
    }

    private void CheckTimer()
    {
        // �ð� ������Ʈ �Լ��� ���� �ð� ������ ��
        curTime -= Time.deltaTime;
        Timer.text = curTime.ToString("F2") + "s";

        if (curTime <= 0)
        {
            SoundManager.Instance?.PlayGlobalSFX(SoundManager.Instance.Data.minigameFail);
            EndGame();
        }
    }

    private void PressSpaceBar()
    {
        // ���� Ű �� �۵��ϳ� �׸��� �����̽� �� ���ȳ�
        isReeling = Mouse.current != null && Mouse.current.leftButton.isPressed;

        // Ʈ��� �÷��̾� ��Ŀ ������ �̵� �ƴϸ� ���� �̵�
        if (isReeling)
        {
            playerVelocity = makerspeed;
        }
        else
        {
            playerVelocity -= gravity * Time.deltaTime;
        }

        // Ʈ���� �� ��Ŀ�� pos x ���� �÷��̾� �ӵ�(��Ŀ �ӵ�) �׷��� ������ �̵���
        float makerX = playerMakerTransform.anchoredPosition.x + playerVelocity * Time.deltaTime;

        // ��Ŀ�� ������ �� �� �Ѿ�� �Ϸ��� �ּ� �ִ� ���ص�
        float bar = gaugeBarRect.width / 2f;
        makerX = Mathf.Clamp(makerX, (-bar + 0.1f), (bar - 0.08f));

        // �׷��� ��Ŀ��ġ�� �� ���� makerX���� ���� ������ ����
        playerMakerTransform.anchoredPosition = new Vector2(makerX, playerMakerTransform.anchoredPosition.y);
    }

    AudioSource audioInSuccessZone = null;
    private void CheckSuccessTime()
    {
        // ���� ���� Ȯ�� ������ 
        if (IsMakerInSuccessZone())
        {
            if(audioInSuccessZone == null)
            {
                audioInSuccessZone = SoundManager.Instance?.PlayStoppable2DSFX(SoundManager.Instance.Data.minigameFishingSuccessZoneLoop);
                audioInSuccessZone.loop = true;
                if(audioInSuccessZone.isPlaying == false)
                    audioInSuccessZone.Play();
            }
            else
            {
                if(audioInSuccessZone.isPlaying == false)
                    audioInSuccessZone.Play();
            }
            // ���� ���� ���ο� �ִ� �ð� ǥ�� �Ǹ� ���� ������ 0���� �ʱ�ȭ
            progressTime += Time.deltaTime;
            Progress.text = ((progressTime / 0.3) * 10).ToString("F1") + "%";

            if (progressTime >= successTime)
            {
                StabilityManager.Instance.StabilizationUp(10, 2);
                if (audioInSuccessZone != null)
                {
                    if (audioInSuccessZone.isPlaying)
                        audioInSuccessZone.Stop();
                }
                SoundManager.Instance?.PlayGlobalSFX(SoundManager.Instance.Data.minigameSuccess);
                
                EndGame();
            }
        }
        else
        {
            if (audioInSuccessZone != null)
            {
                if (audioInSuccessZone.isPlaying)
                    audioInSuccessZone.Stop();
            }
            progressTime = 0f;
            Progress.text = progressTime.ToString("F1") + "%";
        }
    }

    private bool IsMakerInSuccessZone()
    {
        // ���� ���� Ȯ�� �Լ�
        if (playerMakerTransform == null && successZoneTransform == null) return false;

        // ��Ŀ ��ġ Ȯ�ο� �׸��� �� ���� ���� �¿��ؼ� �ּ� �ִ� �ȿ� ������ ���� �ƴ� ����
        float makerX = playerMakerTransform.anchoredPosition.x;
        float successMin = successZoneTransform.anchoredPosition.x - (successZoneRect.width / 2f);
        float successMax = successZoneTransform.anchoredPosition.x + (successZoneRect.width / 2f);

        return makerX >= successMin && makerX <= successMax;
    }

    private void ClickExitButton()
    {
        if(fishingPanel.activeSelf)
        {
            EndGame();
        }
    }

    public void EndGame()
    {
        // �̰� �� �̴� ���� ������ �� �ϴ� ��
        isFishingPlaying = false;
        fishingPanel.SetActive(false);
        tabletSceneManager.tabletPanels[2].SetActive(true);
        tabletSceneManager.isPlaying = false;
    }
}
