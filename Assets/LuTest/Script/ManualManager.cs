using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ManualManager : MonoBehaviour
{
    [SerializeField]
    private MonitorManager monitorManager;

    [SerializeField]
    private GameObject ManualPanel;
    
    [SerializeField]
    private GameObject ScrollViewPanel;

    [SerializeField]
    private GameObject[] DetailPanels;

    [SerializeField]
    private Button BackButton;

    [SerializeField]
    private Button ExitButton;

    [SerializeField]
    private Transform playerCamera;

    [SerializeField]
    private PlayerMove move;

    [SerializeField]
    private Canvas crossHairCanvas;

    [SerializeField]
    private TextMeshProUGUI protocolNumber;

    private Vector3 startCameraPosition;
    private Quaternion startCameraRotation;

    private float startFov;
    private float startFar;

    private float moveDuration = 0.5f;
    private float timer = 0f;

    public bool isOnManual = false;
    public bool isMovingManualCamera = false;

    private Vector3 manualPosition = new Vector3(-1.54f, 1.745f, -0.3f);
    private Quaternion manualRotation = Quaternion.Euler(90f, 0f, 0f);
    private float manualFov = 30f;
    private float manualFar = 1000f;

    private CinemachineCamera playerCamSetting;


    private void Awake()
    {
        if(BackButton != null)
        {
            // ��ư Ŭ���̸� �޴��� �г� ,, escŰ�� �Ŵ��� �ݴ� 
            BackButton.onClick.AddListener(() => BackToManualPanel());
        }
        if (ExitButton != null)
        {
            ExitButton.onClick.AddListener(() => ExitToManual());
        }
    }

    private void ExitToManual()
    {
        ExitManualView();
    }

    private void Start()
    {
        if (playerCamera != null)
        {
            playerCamSetting = playerCamera.GetComponent<CinemachineCamera>();
            if (playerCamSetting != null)
            {
                startFov = playerCamSetting.Lens.FieldOfView;
                startFar = playerCamSetting.Lens.FarClipPlane;
            }
        }
    }

    private void Update()
    {
        if (crossHairCanvas != null)
        {
            crossHairCanvas.enabled = !isOnManual;
        }

        if (isOnManual && !isMovingManualCamera && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ExitManualView();
        }

        if (isOnManual && ScrollViewPanel.activeSelf)
        {
            BackButton.gameObject.SetActive(false);
            ExitButton.gameObject.SetActive(true);
        }
        else if (ScrollViewPanel.activeSelf == false)
        {
            ExitButton.gameObject.SetActive(false);
            BackButton.gameObject.SetActive(true);
        }

        UpdateProtocolNumber();
    }

    private void UpdateProtocolNumber()
    {
        protocolNumber.text = GameManager.Instance.protocolNum.ToString();
    }

    private IEnumerator MovingManualCamera(bool ismoving)
    {
        isMovingManualCamera = true;
        timer = 0f;

        if (ismoving)
        {
            if (move != null && move.enabled) move.enabled = false;
        }

        Vector3 startPos;
        Quaternion startRot;
        float currentStartFov;
        float currentStartFar;

        if (ismoving) // �� ��
        {
            // ���� �÷��̾� ��ġ ����
            startPos = playerCamera.transform.position;
            startRot = playerCamera.transform.rotation;
            currentStartFov = playerCamSetting.Lens.FieldOfView;
            currentStartFar = playerCamSetting.Lens.FarClipPlane;

            // ���߿� ���ƿ� ��ġ�� ����
            startCameraPosition = startPos;
            startCameraRotation = startRot;
        }
        else // ���� ��
        {
            // �Ŵ��� ��ġ���� ����
            startPos = manualPosition;
            startRot = manualRotation;
            currentStartFov = manualFov;
            currentStartFar = manualFar;
        }

        Vector3 endPos = ismoving ? manualPosition : startCameraPosition;
        Quaternion endRot = ismoving ? manualRotation : startCameraRotation;
        float endFov = ismoving ? manualFov : this.startFov;
        float endFar = ismoving ? manualFar : this.startFar;

        while (timer < moveDuration)
        {
            timer += Time.deltaTime;
            float t = timer / moveDuration;

            // Smooth�ϰ� �̵�
            t = t * t * (3f - 2f * t); // SmoothStep

            playerCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
            playerCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            playerCamSetting.Lens.FieldOfView = Mathf.Lerp(currentStartFov, endFov, t);
            playerCamSetting.Lens.FarClipPlane = Mathf.Lerp(currentStartFar, endFar, t);

            yield return null;
        }

        // ���� �� ������ �ٽ� Ȱ��ȭ
        if (!ismoving)
        {
            yield return new WaitForSeconds(0.1f);
            if (move != null) move.enabled = true;
        }

        isMovingManualCamera = false;
    }
    public bool IsMoving()
    {
        return isMovingManualCamera;
    }

    public void MovingManualView()
    {
        if (isMovingManualCamera || isOnManual) return;

        isOnManual = true;
        StartCoroutine(MovingManualCamera(true));

        // UI �ѱ� (����Ʈ �г�)
        if (ManualPanel != null) ManualPanel.SetActive(true);
        if (ScrollViewPanel != null) ScrollViewPanel.SetActive(true);

        CloseAllDetailPanels();
    }
    public void MoveToDetailPanel(int buttonindex)
    {
        if (ScrollViewPanel != null) ScrollViewPanel.SetActive(false);

        // �ٸ� �� ������ �� ���
        CloseAllDetailPanels();
        // ������ �������� �ѱ�
        if (buttonindex >= 0 && buttonindex < DetailPanels.Length)
        {
            DetailPanels[buttonindex].SetActive(true);
        }
    }

    private void ExitManualView()
    {
        if (isMovingManualCamera || !isOnManual) return;

        if (!GameManager.Instance.isGameStart && DaySystem.Instance.GetNowDay() == 1)
        {
            GameManager.Instance.isGameStop = false;
            GameManager.Instance.isGameStart = true;

            GameManager.Instance.anomalySystem.specialObjects[3].GetComponent<Animator>().Play("Close");
            GameManager.Instance.anomalySystem.specialObjects[2].GetComponent<Animator>().Play("Close");

            SoundManager.Instance.Play3DSFX(SoundManager.Instance.Data.ingameDoorCloseHydraulic, GameManager.Instance.anomalySystem.specialObjects[3].transform.position, 20, false);
            SoundManager.Instance.Play3DSFX(SoundManager.Instance.Data.ingameDoorCloseHydraulic, GameManager.Instance.anomalySystem.specialObjects[2].transform.position, 20, false);
        }

        isOnManual = false;
        StartCoroutine(MovingManualCamera(false));

        // ��� �г� ���
        if (ManualPanel != null) ManualPanel.SetActive(true);
        if (ScrollViewPanel != null) ScrollViewPanel.SetActive(true);
        CloseAllDetailPanels();

        BackButton.gameObject.SetActive(false);
        ExitButton.gameObject.SetActive(true);

        if(DaySystem.Instance.GetNowDay() == 1)
        {
            if (monitorManager != null)
            {
                monitorManager.CompleteManualInteract();
            }
        }
        
    }

    private void BackToManualPanel()
    {
        CloseAllDetailPanels();

        // ����Ʈ(��ũ�Ѻ�) �ٽ� �ѱ�
        if (ScrollViewPanel != null) ScrollViewPanel.SetActive(true);
    }

    private void CloseAllDetailPanels()
    {
        for(int i = 0; i < DetailPanels.Length; i++)
        {
            DetailPanels[i].SetActive(false);
        }
    }
}
