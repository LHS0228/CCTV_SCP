using System.Collections;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static System.TimeZoneInfo;
// �ش� �׽�Ʈ ��ũ��Ʈ�Դϴ�. 
public class TabletManager : MonoBehaviour
{
    // �÷��̾� ī�޶�� �º�� ī�޶� ��ġ �� ȸ�� ��
    [SerializeField]
    private Transform playerCamera;

    // �º���� ���� �� �̵� ����
    [SerializeField]
    private PlayerMove move;

    // // �º�� �� ������ ���� �̵� �� �ð� ���� ���� [�ε巯�� ���� ��ȯ?]
    private Vector3 startCameraPosition;
    private Quaternion startCameraRotation;

    // �÷��̾� ���� FOV, Far ��
    private float startFov;
    private float startFar;

    private float moveDuration = 0.5f;
    private float timer = 0f;

    // �º�� �� ���� ����
    public bool isOnTablet = false;
    public bool isMovingTabletCamera = false;

    // ������ Test�� ��ġ, ȸ��, FOV, Far��
    private Vector3 tabletPosition = new Vector3(-3.12f, 2.5f, -0.3506f);
    //-2.045   -0.464
    private Quaternion tabletRotation = Quaternion.Euler(90f, 0f, 0f);
    private float tabletFov = 13f;
    private float tabletFar = 1000f;

    // ī�޶� ���� ����
    //private Camera playerCamSetting;
    private CinemachineCamera playerCamSetting;

    [SerializeField]
    private Button[] interactionTabletButtons;

    [SerializeField]
    private Canvas crossHairCanvas;

    void Start()
    {
        // �׳� ���� �̰�
        if (move != null)
        {
            move.enabled = true;
        }

        //playerCamSetting = playerCamera.GetComponent<Camera>();
        playerCamSetting = playerCamera.GetComponent<CinemachineCamera>();
        if (playerCamSetting != null)
        {
            //startFov = playerCamSetting.fieldOfView;
            //startFar = playerCamSetting.farClipPlane;
            startFov = playerCamSetting.Lens.FieldOfView;
            startFar = playerCamSetting.Lens.FarClipPlane;
        }
        else
        {
            Debug.Log("�÷��̾� ī�޶� ���� null");
        }

        timer = moveDuration + 5f;
    }

    private void Update()
    {
        if (crossHairCanvas == null) return;
        if (!isOnTablet)
        {
            crossHairCanvas.enabled = true;
            for (int i = 0; i < interactionTabletButtons.Length; i++)
            {
                interactionTabletButtons[i].interactable = false;
            }
        }
        else
        {
            crossHairCanvas.enabled = false;
            for (int i = 0; i < interactionTabletButtons.Length; i++)
            {
                interactionTabletButtons[i].interactable = true;
            }
        }

        if (isOnTablet && !isMovingTabletCamera && Input.GetKeyDown(KeyCode.F))
        {
            isOnTablet = false;
            StartCoroutine(MovingTabletCamera(false));
        }
    }

    public bool IsMoving()
    {
        return isMovingTabletCamera;
    }

    public void MovingTabletView()
    {
        if (isMovingTabletCamera || isOnTablet)
        {
            return;
        }
        isOnTablet = true;

        SoundManager.Instance?.PlayGlobalSFX(SoundManager.Instance.Data.ingameTabletZoomIn);
        StartCoroutine(MovingTabletCamera(true));
    }

    private IEnumerator MovingTabletCamera(bool ismoving)
    {
        isMovingTabletCamera = true;
        timer = 0f;

        if (ismoving)
        {
            if (move != null && move.enabled)
            {
                move.enabled = false;
            }
        }

        Vector3 startPos;
        Quaternion startRot;
        float startFov;
        float startFar;

        if (ismoving) // ���� ��
        {
            // ���� �÷��̾� ��ġ�� start.. ���� ����
            startPos = playerCamera.transform.position;
            startRot = playerCamera.transform.rotation;
            //startFov = playerCamSetting.fieldOfView;
            //startFar = playerCamSetting.farClipPlane;
            startFov = playerCamSetting.Lens.FieldOfView;
            startFar = playerCamSetting.Lens.FarClipPlane;

            // ���߿� ���� ���� ���� ���� ��ġ ȸ�� �� ����
            startCameraPosition = startPos;
            startCameraRotation = startRot;
        }
        else // ���� ��
        {
            // �º������ �����ϱ⿡ ���� ���� ���� �º���� ������ ����
            startPos = tabletPosition;
            startRot = tabletRotation;
            startFov = tabletFov;
            startFar = tabletFar;
        }
        // ismoving �� ���� ���� ���� ������ ���� tablet �ƴϸ� ���� ���ϱ� �÷��̾� ��
        Vector3 endPos = ismoving ? tabletPosition : startCameraPosition;
        Quaternion endRot = ismoving ? tabletRotation : startCameraRotation;
        float endFov = ismoving ? tabletFov : this.startFov;
        float endFar = ismoving ? tabletFar : this.startFar;

        while (timer < moveDuration)
        {
            timer += Time.deltaTime;
            float t = timer / moveDuration;

            playerCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
            playerCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            //playerCamSetting.fieldOfView = Mathf.Lerp(startFov, endFov, t);
            //playerCamSetting.farClipPlane = Mathf.Lerp(startFar, endFar, t);
            playerCamSetting.Lens.FieldOfView = Mathf.Lerp(startFov, endFov, t);
            playerCamSetting.Lens.FarClipPlane = Mathf.Lerp(startFar, endFar, t);

            yield return null;
        }

        if (!ismoving) /// ������ ���� �ƴ� ��
        {
            yield return new WaitForSeconds(0.2f);

            if (move != null && !move.enabled)
            {
                move.enabled = true;
            }
        }

        isMovingTabletCamera = false;
    }
}
