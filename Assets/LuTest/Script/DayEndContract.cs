using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DayEndContract : MonoBehaviour
{
    private static DayEndContract _instance;
    public static DayEndContract Instance
    {
        get
        {
            // ���� �ν��Ͻ��� ����ִٸ�
            if (_instance == null)
            {
                // �� �ȿ� �ִ� DayEndContract�� ã�ƺ���
                _instance = FindFirstObjectByType<DayEndContract>();
            }
            return _instance;
        }
    }

    [Header("Main Panel Setting")]
    public GameObject backGroundPanel; // RealBackGroundPanel
    public RectTransform contractPanel; // BackGroundPanel
    public GameObject RealBackGround;
    public TextMeshProUGUI dayText;

    [Header("Base Setting")]
    public float slideDuration = 0.8f;
    public Vector2 targetPosition = Vector2.zero;

    [Header("Interact Objects")]
    public Image checkMarkLeft;

    [Header("UI Buttons")]
    public Button yesButton;

    public bool isContractOn = false;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
        if (yesButton != null)
        {
            yesButton.onClick.AddListener(() => OnClickNextButton());
        }
        if (checkMarkLeft != null)
        {
            checkMarkLeft.gameObject.SetActive(true);
            checkMarkLeft.fillAmount = 0f;
            checkMarkLeft.raycastTarget = false;
        }
        if (backGroundPanel != null)
        { 
            backGroundPanel.SetActive(false); 
        } 
        if(RealBackGround != null)
        {
            RealBackGround.SetActive(false);
        }
    }

    private void Update()
    {
        //if (Keyboard.current.spaceKey.wasPressedThisFrame)
        //{
        //    ShowContract();
        //}
    }

    public void ShowContract()
    {

        isContractOn = true;

        Debug.Log("����");
        if (backGroundPanel != null)
        {
            backGroundPanel.SetActive(true);
        }
        if(RealBackGround != null)
        {
            RealBackGround.SetActive(true);
        }
        if (contractPanel != null)
        {
            contractPanel.gameObject.SetActive(true);
            contractPanel.anchoredPosition = new Vector2(0, -1500f);

            // ���� -1500 ��ġ���� TargetPosition(0,0)���� �̵�
            contractPanel.DOAnchorPos(targetPosition, slideDuration).SetEase(Ease.OutBack);
        }
        dayText.text = "Day " + DaySystem.Instance.GetNowDay().ToString() + " Report";
    }

    public void OnClickNextButton()
    {
        isContractOn= false;

        if (checkMarkLeft != null)
        {
            checkMarkLeft.DOFillAmount(1f, 0.5f).SetEase(Ease.Linear);
        }

        DOVirtual.DelayedCall(2.0f, () =>
        {
            DaySystem.Instance.NextDayButton();
        });
    }

}
