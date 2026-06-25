using System.Collections.Generic;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

public enum CCTVLocation
{
    Left,
    Center,
    Right
}

/// <summary>
/// CCTV 화면 진입, 화면 전환, 일반 시점 복귀를 관리하는 책임을 가진다.
/// </summary>
public class CCTVManager : MonoBehaviour
{
    [SerializeField]
    private PlayerMove move;

    // �÷��̾� ī�޶�� �º�� ī�޶� ��ġ �� ȸ�� ��
    [SerializeField]
    private Transform playerCamera;

    /*[SerializeField]
    private MonoBehaviour playerMove;*/

    [SerializeField]
    private GameObject[] panels;

    // // �º�� �� ������ ���� �̵� �� �ð� ���� ���� [�ε巯�� ���� ��ȯ?]
    // �����ߴ� �� ���� ��ġ
    private Vector3 originalPlayerPos;
    private Quaternion originalPlayerRot;

    // ���� ��
    private Vector3 curCCTVPos;
    private Quaternion curCCTVRot;

    // ���� ��
    private Vector3 renewalPos;
    private Quaternion renewalRot;

    private float renewalFov;
    private float renewalFar;

    private float moveDuration = 0.5f;
    private float timer = 0f;

    // �º�� �� ���� ����
    public bool isOnCCTV = false;
    public bool isMovingCamera = false;
    // �÷��̾� ���� FOV, Far ��
    private float startFov;
    private float startFar;

    // ������ Test�� ��ġ, ȸ��, FOV, Far��
    // ���� ��� ������ 

    private Vector3 LeftCCTVPosition = new Vector3(-2.783f, 1.575f, -1.056f);
    private Quaternion LeftCCTVRotation = Quaternion.Euler(0f, -25.7f, 0f);
    //-0.483f, 1.58f, -1.08f

    private Vector3 CenterCCTVPosition = new Vector3(-2.2897f, 1.575f, -0.9564f);
    private Quaternion CenterCCTVRotation = Quaternion.Euler(0f, 0.25f, 0f);
    //0f, 1.58f, -0.982f

    private Vector3 RightCCTVPosition = new Vector3(-1.792f, 1.575f, -1.058f);
    private Quaternion RightCCTVRotation = Quaternion.Euler(0f, 25.9f, 0f);
    //0.483f, 1.58f, -1.08f



    /*private Vector3 LeftCCTVPosition = new Vector3(-0.364f, 1.571f, -1.39f);
    private Quaternion LeftCCTVRotation = Quaternion.Euler(0f, -24.904f, 0f);
    private Vector3 CenterCCTVPosition = new Vector3(0, 1.571f, -1.32f);
    private Quaternion CenterCCTVRotation = Quaternion.Euler(0f, 0f, 0f);
    private Vector3 RightCCTVPosition = new Vector3(0.364f, 1.571f, -1.39f);
    private Quaternion RightCCTVRotation = Quaternion.Euler(0f, 24.904f, 0f);*/
    private float CCTVFov = 26f;
    private float CCTVFar = 1000f;

    // ī�޶� ���� ����
    //private Camera playerCamSetting;
    private CinemachineCamera playerCamSetting;

    [SerializeField]
    private Button[] interactionCCTVButtons;

    private void Awake()
    {
        panels[0].transform.Find("RightBt").GetComponent<Button>().onClick.AddListener(() => CCTV_Pos_Rot(CCTVLocation.Center));
        panels[1].transform.Find("LeftBt").GetComponent<Button>().onClick.AddListener(() => CCTV_Pos_Rot(CCTVLocation.Left));
        panels[1].transform.Find("RightBt").GetComponent<Button>().onClick.AddListener(() => CCTV_Pos_Rot(CCTVLocation.Right));
        panels[2].transform.Find("LeftBt").GetComponent<Button>().onClick.AddListener(() => CCTV_Pos_Rot(CCTVLocation.Center));
    }

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
            //    startFov = playerCamSetting.fieldOfView;
            //    startFar = playerCamSetting.farClipPlane;
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
        if (!isOnCCTV)
        {
            for (int i = 0; i < interactionCCTVButtons.Length; i++)
            {
                interactionCCTVButtons[i].interactable = false;
            }
        }
        else
        {
            for (int i = 0; i < interactionCCTVButtons.Length; i++)
            {
                interactionCCTVButtons[i].interactable = true;
            }
        }
    }
    public bool IsMoving()
    {
        return isMovingCamera;
    }

    public bool TryHandleBackInput()
    {
        if (!isOnCCTV || isMovingCamera)
        {
            return false;
        }

        ExitCCTVView();
        return true;
    }

    public void CCTV_Pos_Rot(CCTVLocation Location)
    {

        if (isMovingCamera)
        {
            return;
        }
        
        renewalPos = playerCamera.transform.position;
        renewalRot = playerCamera.transform.rotation;

        //renewalFov = playerCamSetting.fieldOfView;
        //renewalFar = playerCamSetting.farClipPlane;
        renewalFov = playerCamSetting.Lens.FieldOfView;
        renewalFar = playerCamSetting.Lens.FarClipPlane;

        Vector3 targetPos;
        Quaternion targetRot;

        if (Location == CCTVLocation.Left)
        {
            targetPos = LeftCCTVPosition;
            targetRot = LeftCCTVRotation;

        }
        else if (Location == CCTVLocation.Center)
        {
            targetPos = CenterCCTVPosition;
            targetRot = CenterCCTVRotation;
        }
        else
        {
            targetPos = RightCCTVPosition;
            targetRot = RightCCTVRotation;
        }

        if (isOnCCTV && curCCTVPos == targetPos)
        {
            ExitCCTVView();
        }
        else
        {
            if (!isOnCCTV)
            {
                originalPlayerPos = renewalPos;
                originalPlayerRot = renewalRot;
            }

            isOnCCTV = true;
            curCCTVPos = targetPos;
            curCCTVRot = targetRot;
            StartCoroutine(MovingCamera(true));
        }
    }

    private void ExitCCTVView()
    {
        renewalPos = playerCamera.transform.position;
        renewalRot = playerCamera.transform.rotation;
        renewalFov = playerCamSetting.Lens.FieldOfView;
        renewalFar = playerCamSetting.Lens.FarClipPlane;

        isOnCCTV = false;
        StartCoroutine(MovingCamera(false));
    }

    private IEnumerator MovingCamera(bool ismoving)
    {
        isMovingCamera = true;
        float timer = 0f;

        if (ismoving)
        {
            if (move != null && move.enabled)
            {
                move.enabled = false;
            }
        }

        Vector3 startPos = renewalPos;
        Quaternion startRot = renewalRot;
        float startFov = renewalFov;
        float startFar = renewalFar;

        Vector3 endPos = ismoving ? curCCTVPos : originalPlayerPos;
        Quaternion endRot = ismoving ? curCCTVRot : originalPlayerRot;
        float endFov = ismoving ? CCTVFov : this.startFov;
        float endFar = ismoving ? CCTVFar : this.startFar;

        while (timer < moveDuration)
        {
            timer += Time.deltaTime;
            float t = timer / moveDuration;

            t = Mathf.SmoothStep(0f, 1f, t);

            playerCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
            playerCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            //playerCamSetting.fieldOfView = Mathf.Lerp(startFov, endFov, t);
            //playerCamSetting.farClipPlane = Mathf.Lerp(startFar, endFar, t);
            playerCamSetting.Lens.FieldOfView = Mathf.Lerp(startFov, endFov, t);
            playerCamSetting.Lens.FarClipPlane = Mathf.Lerp(startFar, endFar, t);

            yield return null;
        }

        if (!ismoving)
        {
            yield return new WaitForSeconds(0.1f);

            if (move != null && !move.enabled)
            {
                move.enabled = true;
            }
        }

        isMovingCamera = false;
    }
}

