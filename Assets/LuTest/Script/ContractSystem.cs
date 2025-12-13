using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ContractSystem : MonoBehaviour
{
    [Header("Sound Setting")]
    public SoundManager soundManager;
    [Header("Main Panel Setting")]
    public GameObject backGroundPanel; // RealBackGroundPanel
    public RectTransform contractPanel; // BackGroundPanel
    public Image fadeOutImage;
    public GameObject startButton;
    public GameObject gameLogo;

    [Header("Interact Objects")]
    public Image checkMarkLeft;
    public Image checkMarkRight;

    [Header("Base Setting")]
    public float slideDuration = 0.8f;
    public Vector2 targetPostion = Vector2.zero;

    [Header("UI Buttons")]
    public Button gameStartButton;
    public Button yesButton;
    public Button noButton;

    [Header("Option")]
    public GameObject optionMenu;
    public Button optionButton;

    [Header("Exit")]
    public Button exitButton;

    private Vector2 InitPosition;

    void Start()
    {
        if(fadeOutImage != null)
        {
            fadeOutImage.color = new Color(0,0,0,0); // cho gi hwa  hok si mol la seo
            fadeOutImage.raycastTarget = false;
        }

        if (contractPanel != null)
        {
            InitPosition = contractPanel.anchoredPosition;
        }

        if (checkMarkLeft != null)
        {
            checkMarkLeft.gameObject.SetActive(true);
            checkMarkLeft.fillAmount = 0f;
            checkMarkLeft.raycastTarget = false;
        }

        if (checkMarkRight != null)
        {
            checkMarkRight.gameObject.SetActive(true);
            checkMarkRight.fillAmount = 0f;
            checkMarkRight.raycastTarget = false;
        }

        if (gameStartButton != null)
        {
            gameStartButton.onClick.AddListener(() => OnClickTitleStartButton());
        }

        if(yesButton != null)
        {
            yesButton.onClick.AddListener(() => OnClickYesButton());
        }

        if (noButton != null)
        {
            noButton.onClick.AddListener(() => OnClickNoButton());
        }
        if (optionButton != null)
        {
            optionButton.onClick.AddListener(() => OnClickOptionButton());
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(() => OnClickExitButton());
        }

        if (soundManager == null)
            soundManager = SoundManager.Instance;
        if(soundManager != null)
        {
            soundManager.PlayBGM(soundManager.Data.systemUiBgmTitle);
        }
    }

    private void Update()
    {
        if (optionMenu.activeSelf)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                optionMenu.SetActive(false);
            }
        }
    }

    private void OnClickOptionButton()
    {
        if (optionMenu.activeSelf == false)
        {
            optionMenu.SetActive(true);
        }
        else if (optionMenu.activeSelf)
        {
            optionMenu.SetActive(false);
        }
    }
    public void OnClickTitleStartButton()
    {
        if(startButton != null)
        {
            startButton.SetActive(false);
            gameLogo.SetActive(false);
        }
        optionButton.gameObject.SetActive(false);
        exitButton.gameObject.SetActive(false);

        backGroundPanel.SetActive(true);
        //contractPanel.DOAnchorPos(targetPostion, duration).SetEase(Ease.OutBack);

        contractPanel.rotation = Quaternion.Euler(0, 0, -40f);

        // [��� 2] �ӵ��� 0.5�ʷ� �ٿ��� �� ���ǵ��ϰ� (���� 0.8�� -> 0.5��)
        float duration = 1.2f;

        // 1. ��ġ �̵� (������ �Ʒ���)
        contractPanel.DOAnchorPos(targetPostion, duration).SetEase(Ease.OutBack);

        // 2. ȸ�� ���� (�ߵ��� ���� -> 0��)
        // �������鼭 ������ �� �������� �谨�� ��ϴ�.
        contractPanel.DORotate(Vector3.zero, duration).SetEase(Ease.OutBack);

    }

    public void OnClickYesButton()
    {
        if (checkMarkLeft != null)
        {
            checkMarkLeft.DOFillAmount(1f, 0.5f).SetEase(Ease.Linear);
        }

        DOVirtual.DelayedCall(1.0f, () =>
        {
            FadeOut();
        });
    }

    public void OnClickNoButton()
    {
        if (checkMarkRight != null)
        {
            checkMarkRight.DOFillAmount(1f, 0.5f).SetEase(Ease.Linear);
        }

        DOVirtual.DelayedCall(1.0f, () =>
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });

    }
    private void OnClickExitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    private void FadeOut()
    {
        if(fadeOutImage != null)
        {
            fadeOutImage.raycastTarget = true;
            fadeOutImage.DOFade(1f, 2f).OnComplete(() =>
            {
                SceneManager.LoadScene(1);
            });
        }
        else
        {
            SceneManager.LoadScene(1);
        }
    }
}
