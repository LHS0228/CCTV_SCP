using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 태블릿의 9버튼 순서 기억 미니게임을 관리하는 책임을 가진다.
/// </summary>
public class Button9 : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI DisplayNum;

    // ��ư 9�� 
    [SerializeField]
    private Button[] buttons;

    // �г� ����
    [SerializeField]
    private GameObject button9Panel;

    // ������ ��ư ����Ʈ, ���� ��ư ����Ʈ
    private List<int> buttonSequence = new List<int>();
    private List<int> inputSequence = new List<int>();

    // ��ư�� ���� �ο��Ϸ��� ���� ��
    private Color defaultColor = Color.white;
    private Color lightColor = Color.cyan;

    // ��ư �˷��ִ� �ð�
    private float lightDuration = 0.5f;

    // ��ư�� �˷��ְ� �ִ°�? �� �̰� ������� ��ư ���� �� �Է� �� �ް� �ϴ�? �׷��������� ��
    private bool isStagePlaying = false;

    [SerializeField]
    TabletSceneManager tabletSceneManager;

    [SerializeField]
    private Button exitButton;

    private int difficultyLevel = 0;

    private void Awake()
    {
       // ������ ��ư �迭�� OnClick ����� �־���
        for(int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            if (buttons[index] != null)
            {
                buttons[index].onClick.AddListener(() => OnClickButton(index));
            }
        }

        exitButton.onClick.AddListener(() => ClickExitButton());
    }
    // ���۽� �г� ��
    private void Start()
    {
        button9Panel.SetActive(false);
    }


    private int GetDay()
    {
        return DaySystem.Instance.GetNowDay();
    }
    // �º������ ��ŸƮ ��ư�� ������ �� ����Ǵ� �Լ��̸�, �г� �ѱ�,
    // �������� �ʱ�ȭ, ��ư ���� �ʱ�ȭ, �������� ������ ����.
    public void StartButton9()
    {
        if (tabletSceneManager.isPlaying)
        {
            button9Panel.SetActive(true);
            buttonSequence.Clear();
            StartCoroutine(StageStart());
            GetDay();
        }
    }
    
    // �������� ���� �Լ�
    private IEnumerator StageStart()
    {
        // �������� ���� ���� Ȱ��ȭ [��ư ���� �� �Է� ��������]
        isStagePlaying = true;
        // �Է� �� ���� �ʱ�ȭ
        buttonSequence.Clear();
        inputSequence.Clear();

        if (GetDay() == 1)
        {
            difficultyLevel = 1;
            lightDuration = 0.4f;
        }
        else if (GetDay() == 2)
        {
            difficultyLevel = 2;
            lightDuration = 0.4f;
        }
        else if (GetDay() == 3)
        {
            difficultyLevel = 2;
            lightDuration = 0.3f;
        }
        else if (GetDay() == 4 || GetDay() == 5)
        {
            difficultyLevel = 3;
            lightDuration = 0.3f;
        }

        // ��ư �ߺ� ������ �����ϱ����� ����� ����Ʈ �߰� �� ����Ʈ�� ��ư ����ŭ �迭�� �ֱ�
        List<int> preparatoryList = new List<int>();
        for(int i = 0; i < buttons.Length; i++)
        {
            preparatoryList.Add(i);
        }

        // �������� �� Ȱ��ȭ ��ư �� �� ���� ���ϱ�
        for(int i = 0; i < 4 + difficultyLevel; i++)
        {
            // ����� ����Ʈ�� 2�� �̻� ���� �� �̰� �־��� ������ Random.Range�� �����ǵ�
            // Random.Range�� (�ּ�, �ִ�)�̰� (0, 1) �̶������� �ִ��� -1 ���� �̴°Ŷ� 0�ۿ� �Ȼ��� Ȯ��
            if(preparatoryList.Count > 1)
            {
                // �������� ���� ����
                int random = Random.Range(0, preparatoryList.Count);
                // ���� ���� �ֱ����� ���� 1�� ��
                int randomNumber = preparatoryList[random];
                // ��ư ���� ����Ʈ�� �߰�
                buttonSequence.Add(randomNumber);
                // ���� ���� ���� ����Ʈ���� ����
                preparatoryList.RemoveAt(random);
            }
        }
        DisplayInputNumber();
        yield return new WaitForSeconds(1f);

        // �������� ���� ����Ʈ �ٺ�������
        foreach(int index in buttonSequence)
        {
            // ��ư�� ���� �ٲٱ����� image ������Ʈ �ҷ�����
            Image btColor = buttons[index].GetComponent<Image>();

            yield return new WaitForSeconds(lightDuration);
            SoundManager.Instance?.PlayGlobalSFX(SoundManager.Instance.Data.minigameKeypadLeadSignal);
            btColor.color = lightColor;
            yield return new WaitForSeconds(lightDuration);
            btColor.color = defaultColor;
        }
        isStagePlaying = false;
        Debug.Log($"�÷��� �� : {isStagePlaying}");

    }

    private void OnClickButton(int buttonindex)
    {
        // �������� ���� ���� �� [�� ��¦�� ��] ����
        if (isStagePlaying)
        {
            return;
        }

        // ���� �� ��ư�� OnClick ��� �ο��ߴµ� ������ �� ���� ��ư�� ��ȣ�� ����Ʈ�� �߰� 
        inputSequence.Add(buttonindex);

        DisplayInputNumber();
        // ���� ���� ��ư�� ���� ��ư�� ����Ʈ�� ���Ͽ� Ʋ�� ��� �ٷ� ı
        // ���� ��� ��ư�� ����Ʈ�� { 0 , 1 , 2 }�� ���� �� 
        // ���� inputSequence�� ����Ʈ���� {} ������� ���� 1��° ĭ�� ������ 0�� ��
        // �׷��� inputSequence�� ����Ʈ�� { 0 }�� �ǰ� �� if�� ������ ����
        // count�� 1�� �Ǵϱ�
        // inputSequence[0]�� ���� 0 buttonSequence[0]�� ���� 0 �̱⿡ ������ Ʋ���� ������
        if (inputSequence[inputSequence.Count - 1] != buttonSequence[inputSequence.Count - 1])
        {
            Debug.Log("����");
            SoundManager.Instance?.PlayGlobalSFX(SoundManager.Instance.Data.minigameFail);
            EndGame();
            return;
        }
        // �׷��� �� �����ϰ� �� �迭�� ������ ������ �Ȱ��� �ƴٴ� �ű⿡ ����
        if (inputSequence.Count == buttonSequence.Count)
        {
            if (inputSequence.Count == 9)
            {
                Debug.Log("5����");
                SoundManager.Instance?.PlayGlobalSFX(SoundManager.Instance.Data.minigameFail);
                EndGame();
            }
            else
            {
                Debug.Log($"{DaySystem.Instance.GetNowDay()} ���� Ŭ����");
                SoundManager.Instance?.PlayGlobalSFX(SoundManager.Instance.Data.minigameSuccess);
                StabilityManager.Instance.StabilizationUp(10, 1);
                EndGame();
            }
        }
        Debug.Log($"��ư {buttonindex + 1} ����");
    }

    private void DisplayInputNumber()
    {
        if(DisplayNum == null)
        {
            return;
        }

        string resultText = "";

        foreach (int i in inputSequence)
        {
            resultText += (i + 1).ToString() + " ";
        }

        int remainText = buttonSequence.Count - inputSequence.Count;
        for (int i = 0; i < remainText; i++)
        {
            resultText += "_ ";
        }

        DisplayNum.text = resultText;
    }

    private void ClickExitButton()
    {
        if(button9Panel.activeSelf)
        {
            EndGame();
        }
    }

    // ���� ���� ��
    public void EndGame()
    {
        StopAllCoroutines();
        isStagePlaying = false;
        buttonSequence.Clear();
        inputSequence.Clear();
        button9Panel.SetActive(false);
        tabletSceneManager.tabletPanels[1].SetActive(true);
        tabletSceneManager.isPlaying = false;

        if(DisplayNum != null)
        {
            DisplayNum.text = " ";
        }
    }
}
