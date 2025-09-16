using Unity.VisualScripting;
using UnityEngine;
using static System.TimeZoneInfo;
// �ش� �׽�Ʈ ��ũ��Ʈ�Դϴ�. 
public class TestManager : MonoBehaviour
{
    // �÷��̾� ī�޶�� �º� ī�޶� ��ġ �� ȸ�� ��
    [SerializeField]
    private Transform playerCamera;

    // �º��� ���� �� �̵� ����
    [SerializeField] 
    private MonoBehaviour playerMove;

    // // �º� �� ������ ���� �̵� �� �ð� ���� ���� [�ε巯�� ���� ��ȯ?]
    private Vector3 startCameraPosition;
    private Quaternion startCameraRotation;
    private float moveDuration = 0.5f;
    private float timer = 0f;

    // �º� �� ���� ����
    public bool isOnTablet = false;

    // �÷��̾� ���� FOV, Far ��
    private float startFOV;
    private float startFar;

    // ������ Test�� ��ġ, ȸ��, FOV, Far��
    private Vector3 tabletPosition = new Vector3(4f, 5f, 4.47f);
    private Quaternion tabletRotation = Quaternion.Euler(90f, 0f, 0f);
    private float tabletFOV = 11.8f;
    private float tabletFar = 6f;

    // ī�޶� ���� ����
    private Camera playerCamSetting;
     void Start()
    {
        // �׳� ���� �̰�
        if(playerMove != null)
        {
            playerMove.enabled = true;
        }

        playerCamSetting = playerCamera.GetComponent<Camera>();
        if (playerCamSetting != null)
        {
            startFOV = playerCamSetting.fieldOfView;
            startFar = playerCamSetting.farClipPlane;
        }
        else
        {
            Debug.Log("�÷��̾� ī�޶� ���� null");
        }

        timer = moveDuration + 5f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            isOnTablet = !isOnTablet;
            timer = 0;

            if (isOnTablet)
            {
                startCameraPosition = playerCamera.transform.position;
                startCameraRotation = playerCamera.transform.rotation;

                if (playerMove != null)
                {
                    playerMove.enabled = false;
                }
                Debug.Log($"{isOnTablet} is True");
            }
            else if (isOnTablet == false)
            {
                if (playerMove != null)
                {
                    playerMove.enabled = true;
                }

                Debug.Log($"{isOnTablet} is False");
            }
        }

        if (timer < moveDuration)
        {
            timer += Time.deltaTime;
            float t = timer / moveDuration;

            if (playerCamSetting != null)
            {
                if (isOnTablet)
                {
                    playerCamera.transform.position = Vector3.Lerp(startCameraPosition, tabletPosition, t);
                    playerCamera.transform.rotation = Quaternion.Slerp(startCameraRotation, tabletRotation, t);

                    playerCamSetting.fieldOfView = Mathf.Lerp(startFOV, tabletFOV, t);
                    playerCamSetting.farClipPlane = Mathf.Lerp(startFar, tabletFar, t);
                }
                else
                {
                    playerCamera.transform.position = Vector3.Lerp(tabletPosition, startCameraPosition, t);
                    playerCamera.transform.rotation = Quaternion.Slerp(tabletRotation, startCameraRotation, t);

                    playerCamSetting.fieldOfView = Mathf.Lerp(tabletFOV, startFOV, t);
                    playerCamSetting.farClipPlane = Mathf.Lerp(tabletFar, startFar, t);
                }
            }
        }

    }
}
