using Unity.VisualScripting;
using UnityEngine;
using static System.TimeZoneInfo;
// 해당 테스트 스크립트입니다. 
public class TestManager : MonoBehaviour
{
    // 플레이어 카메라와 태블릿 카메라 위치 및 회전 값
    [SerializeField]
    private Transform playerCamera;

    // 태블릿을 켰을 때 이동 관련
    [SerializeField] 
    private MonoBehaviour playerMove;

    // // 태블릿 온 오프시 시점 이동 및 시간 관련 변수 [부드러운 시점 변환?]
    private Vector3 startCameraPosition;
    private Quaternion startCameraRotation;
    private float moveDuration = 0.5f;
    private float timer = 0f;

    // 태블릿 온 오프 여부
    public bool isOnTablet = false;

    // 플레이어 시작 FOV, Far 값
    private float startFOV;
    private float startFar;

    // 세팅한 Test의 위치, 회전, FOV, Far값
    private Vector3 tabletPosition = new Vector3(4f, 5f, 4.47f);
    private Quaternion tabletRotation = Quaternion.Euler(90f, 0f, 0f);
    private float tabletFOV = 11.8f;
    private float tabletFar = 6f;

    // 카메라 세팅 관련
    private Camera playerCamSetting;
     void Start()
    {
        // 그냥 넣음 이건
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
            Debug.Log("플레이어 카메라 세팅 null");
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
