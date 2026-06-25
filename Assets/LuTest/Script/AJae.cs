using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using UnityEngine.UI;

// �ؽ�Ʈ�� �ϴϱ� �� �̻��� Ű �̹��� �����ͼ� �Ϸ��� ��������ϴ�.
// Ű�� �� �̹����� ��Ī Ŭ����
/// <summary>
/// 아재 미니게임에서 입력 키와 표시 이미지를 연결하는 책임을 가진다.
/// </summary>
[System.Serializable]
public class Link_Image_And_Key
{
    public string key;
    public Sprite keyImage;
}

/// <summary>
/// 태블릿의 키 입력 순서 미니게임을 관리하는 책임을 가진다.
/// </summary>
public class AJae : MonoBehaviour
{
    // �г� �¿���
    [SerializeField]
    private GameObject aJaePanel;

    [SerializeField]
    private Link_Image_And_Key[] Link;

    // ���� ���۽� ���� ����ַ���
    [SerializeField]
    private Image[] ShowImages;

    // Ÿ�̸� �����ַ���
    [SerializeField]
    private TextMeshProUGUI timerText;
    // ���� �ð�
    private float limitTimer = 10;
    // Ÿ�̸ӿ� �����
    private float timer = 0;
    // ������������ ���� �þ�µ� ���� ����
    private int stageLen = 6;

    // �������� ���� ����Ʈ�� �Է¿� �ʿ��� ����Ʈ
    List<string> aJaeSequence = new List<string>();
    List<string> inputSequence = new List<string>();

    // �̰� �� ���ߴ� �ϴ� ������ Ÿ������ ����
    private bool isAjaePlaying = false;

    [SerializeField]
    TabletSceneManager tabletSceneManager;

    [SerializeField]
    private Button exitButton;

    private int difficultyLevel = 1;

    private void Awake()
    {
        exitButton.onClick.AddListener(() => ClickExitButton());
    }

    private void Start()
    {
        aJaePanel.SetActive(false);
    }
    private int GetDay()
    {
        return DaySystem.Instance.GetNowDay();
    }

    private void Update()
    {

        // �ð� ���� �� �ؽ�Ʈ�� ��������
        if (isAjaePlaying)
        {
            timer -= Time.deltaTime;
            timerText.text = timer.ToString("F1");

            if(timer <= 0)
            {
                SoundManager.Instance?.PlayGlobalSFX(SoundManager.Instance.Data.minigameFail);
                EndGame();
                return;
            }
        }

        // ���� ����ÿ���
        if(isAjaePlaying)
        {
            // �Է� ����
            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                InputKey("q");
            }
            if (Keyboard.current.wKey.wasPressedThisFrame)
            {
                InputKey("w");
            }
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                InputKey("e");
            }
            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                InputKey("a");
            }
            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                InputKey("s");
            }
            if (Keyboard.current.dKey.wasPressedThisFrame)
            {
                InputKey("d");
            }
        }
    }

    public void StartAJae()
    {
        if(tabletSceneManager.isPlaying)
        {
            // �г� ��, ����Ʈ ����, �Լ� ����
            aJaePanel.SetActive(true);
            aJaeSequence.Clear();
            inputSequence.Clear();
            StartCoroutine(StartStage());
        }
    }

    private IEnumerator StartStage()
    {
        // �÷��� ��, ����Ʈ ����, 1�� ��ٸ���
        aJaeSequence.Clear();
        inputSequence.Clear();
        timerText.text = limitTimer.ToString("F1");

        int day = GetDay();
        if (day == 1) { difficultyLevel = 1; }
        else if (day == 2) { difficultyLevel = 2; }
        else if (day == 3 || day == 4 || day == 5) { difficultyLevel = 3; }

        foreach (var images in ShowImages)
        {
            images.gameObject.SetActive(false);
            images.color = Color.white;
        }

        // �������� ���� ���� ���� �� �ߺ������� ���� �̾� ����Ʈ�� �ֱ�

        for(int i = 0; i < stageLen + (2 * (difficultyLevel - 1)); i++)
        {
            int index = Random.Range(0, Link.Length);
            aJaeSequence.Add(Link[index].key);
        }

        // ������ ����Ʈ ũ�� ��ŭ �����ϸ�, ���������� ���̴� �������� �������� ı �������� �Ⱥ���
        for (int i = 0; i < ShowImages.Length; i++)
        {
            if(i < aJaeSequence.Count)
            {
                string currentKey = aJaeSequence[i];
                // �迭 ã���ִ� �� ������ �̹��� ��������, �ƴ� ����
                Sprite sprite = Link.FirstOrDefault(x => x.key == currentKey)?.keyImage;

                ShowImages[i].sprite = sprite;
                
            }
            else
            {
                ShowImages[i].gameObject.SetActive(false);
            }
        }

        // n�� ��ٷȴ�, Ÿ�̸� ����,  �÷��� ��
        yield return new WaitForSeconds(1f);

        for(int i = 0; i < ShowImages.Length; i++)
        {
            if(i < aJaeSequence.Count)
            {
                ShowImages[i].gameObject.SetActive(true);
            }
        }
        timer = limitTimer;
        isAjaePlaying = true;
    }

    private void InputKey(string inputKey)
    {
        // �����÷��� �������δ� �ٽ� �۾��ҵ�
        if(!isAjaePlaying)
        {
            return;
        }
        SoundManager.Instance?.PlayGlobalSFX(SoundManager.Instance.Data.minigameRhythmKeyInput);

        // ���� �Է°� �Է� ����Ʈ�� �߰�
        inputSequence.Add(inputKey);

        int currentKey = inputSequence.Count - 1;

        // ���°Ŷ� �̻��ϰ� ġ��
        if (inputSequence[inputSequence.Count - 1] != aJaeSequence[inputSequence.Count - 1])
        {
            // ���⼭ ������ �Լ��̿�
            isAjaePlaying = false;
            SoundManager.Instance?.PlayGlobalSFX(SoundManager.Instance.Data.minigameFail);
            EndGame();
        }
        else if (inputSequence[currentKey] == aJaeSequence[currentKey])
        {
            ShowImages[currentKey].color = Color.gray;
        }

        if (inputSequence.Count == aJaeSequence.Count)
        {
            StabilityManager.Instance.StabilizationUp(10, 0);
            SoundManager.Instance?.PlayGlobalSFX(SoundManager.Instance.Data.minigameSuccess);
            // ���� �Լ����� �κ� ���ľߵ� �׽�Ʈ������ ��� ������ ����.
            Debug.Log($"{DaySystem.Instance.GetNowDay()} ����");
            EndGame();
        }
    }

    private void ClickExitButton()
    {
        if (aJaePanel.activeSelf)
        {
            EndGame();
        }
    }


    // ���� ��ü���� ��� �̿�
    public void EndGame()
    {
        StopAllCoroutines();
        isAjaePlaying = false;
        aJaePanel.SetActive(false);
        tabletSceneManager.tabletPanels[0].SetActive(true);
        tabletSceneManager.isPlaying = false;
    }
}
