using System.Threading;
using UnityEditor;
using UnityEngine;

public class InteractiveObject : MonoBehaviour
{
    [SerializeField]
    private Camera playerCamera;

    [Header("��ȣ�ۿ� �Ÿ�")]
    [SerializeField]
    private float InteractionDistance = 5f;

    [Header("��ȣ�ۿ� ������Ʈ")]
    [SerializeField]
    private GameObject LeftMonitor;

    [SerializeField]
    private GameObject CenterMonitor;

    [SerializeField]
    private GameObject RightMonitor;

    [SerializeField]
    private GameObject Tablet;

    [SerializeField]
    private GameObject ManualObject;

    [SerializeField]
    private GameObject BarrelObject;//Pills Obj
    [SerializeField]
    private GameObject CrossingGatObject;//Crossing gate Obj
    [SerializeField]
    private GameObject FeedBarObject;//FeedBar Obj

    private GameObject rayCollisionObject;

    [SerializeField]
    private CCTVManager cctvManager;

    [SerializeField]
    private TabletManager testManager;

    [SerializeField]
    private ManualManager manualManager;

    [SerializeField]
    private LayerMask outlineLayerMask;

    private MeshRenderer curOutlineScale = null;

    private MaterialPropertyBlock outlineBlock;

    [SerializeField]
    private string outlineProperty = "_Scale";

    private float offOutlineScale = 0f;

    private float onOutlineScale = 1.05f;

    private bool isOnRay = true;

    private Canvas curInteractUI = null;

    private void Start()
    {
        outlineBlock = new MaterialPropertyBlock();
    }

    // Update is called once per frame
    void Update()
    {
        InteractionObject();

        if (cctvManager.isOnCCTV || testManager.isOnTablet || !isOnRay || manualManager.isOnManual)
        {
            ShowOutLine(null);
        }

        if (Input.GetKeyDown(KeyCode.F) && rayCollisionObject != null && !cctvManager.IsMoving() && !testManager.IsMoving() && !manualManager.IsMoving())
        {
            if (rayCollisionObject == LeftMonitor)
            {
                InteractLeftMonitor();
            }
            else if (rayCollisionObject == CenterMonitor)
            {
                InteractCenterMonitor();
            }
            else if (rayCollisionObject == RightMonitor)
            {
                InteractRightMonitor();
            }
            else if (rayCollisionObject == Tablet)
            {
                InteractTablet();
            }
            else if (rayCollisionObject == ManualObject)
            {
                InteractManual();
            }
            else if(rayCollisionObject == BarrelObject)
            {
                InteractBarrel();
            }
            else if(rayCollisionObject == CrossingGatObject)
            {
                InteractCrossingGat();
            }
            else if(rayCollisionObject == FeedBarObject)
            {
                InteractFeedBar();
            }
        }
    }

    void InteractionObject()
    {
        // ray�� ��ũ�� ���� ������ �߾��� �� ���߾�
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        Debug.DrawRay(ray.origin, ray.direction);
        RaycastHit hit; // Hit ���� ������Ʈ�� hit�� ����

        GameObject outLineObject = null;

        if (Physics.Raycast(ray, out hit, InteractionDistance, outlineLayerMask))
        {
            GameObject hitObject = hit.collider.gameObject;
            if (hitObject == LeftMonitor || hitObject == CenterMonitor ||
                hitObject == RightMonitor || hitObject == Tablet || hitObject == ManualObject)
            {
                rayCollisionObject = hitObject;
                outLineObject = hitObject;
            }
            else if(hitObject == BarrelObject)
            {
                if(ShaderEffect_CorruptedVram.Instance.shift == 1)
                {
                    rayCollisionObject = hitObject;
                    outLineObject = hitObject;
                }
            }

            else if (hitObject == CrossingGatObject)
            {
                if (!GameManager.Instance.isGameStop)
                {
                    //부모(transform.parent)에서 스크립트를 가져옵니다.
                    CrossingGats gatScript = CrossingGatObject.transform.parent.GetComponent<CrossingGats>();

                    // 스크립트가 존재하고, GuideOn()이 true일 때만 아웃라인 표시
                    if (gatScript != null && !gatScript.GuideOff())
                    {
                        rayCollisionObject = hitObject; // 상호작용 대상 설정
                        outLineObject = hitObject;      // 아웃라인 대상 설정
                    }
                    else
                    {
                        // GuideOn이 false면 상호작용도 안 되고 아웃라인도 안 뜸
                        // (만약 아웃라인은 안 떠도 F키는 먹히게 하고 싶다면 rayCollisionObject = hitObject;를 밖으로 빼세요)
                        rayCollisionObject = null;
                    }
                }
            }

            else if (hitObject == FeedBarObject)
            {
                //부모(transform.parent)에서 스크립트를 가져옵니다.
                FeedCetner gatScript = FeedBarObject.transform.parent.GetComponent<FeedCetner>();

                // 스크립트가 존재하고, GuideOn()이 true일 때만 아웃라인 표시
                if (gatScript != null && !gatScript.GuideOff())
                {
                    rayCollisionObject = hitObject; // 상호작용 대상 설정
                    outLineObject = hitObject;      // 아웃라인 대상 설정
                }
                else
                {
                    // GuideOn이 false면 상호작용도 안 되고 아웃라인도 안 뜸
                    // (만약 아웃라인은 안 떠도 F키는 먹히게 하고 싶다면 rayCollisionObject = hitObject;를 밖으로 빼세요)
                    rayCollisionObject = null;
                }
            }
            else
            {
                rayCollisionObject = null;
            }
        }
        else
        {
            rayCollisionObject = null;
        }

        ShowOutLine(outLineObject);
    }

    void InteractLeftMonitor()
    {
        cctvManager.CCTV_Pos_Rot(CCTVLocation.Left);
        Debug.Log("��");
    }
    void InteractCenterMonitor()
    {
        cctvManager.CCTV_Pos_Rot(CCTVLocation.Center);
        Debug.Log("��");
    }

    void InteractRightMonitor()
    {
        cctvManager.CCTV_Pos_Rot(CCTVLocation.Right);
        Debug.Log("��");
    }
    void InteractTablet()
    {
        testManager.MovingTabletView();
    }
    private void InteractManual()
    {
        manualManager.MovingManualView();
    }
    private void InteractBarrel()
    {
        GameManager.Instance.anomalySystem.ClearSpecial();
    }
    private void InteractCrossingGat()
    {
        CrossingGats gatComp = CrossingGatObject.transform.parent.GetComponent<CrossingGats>();
        if (gatComp)
            gatComp.TryActionEvent();
    }
    private void InteractFeedBar()
    {
        FeedCetner gatComp = FeedBarObject.transform.parent.GetComponent<FeedCetner>();
        if (gatComp)
            gatComp.TryActionEvent();
    }

    private void ShowOutLine(GameObject outlineable)
    {
        MeshRenderer newOutLineObject = null;
        Canvas newInteractUI = null;

        if (outlineable != null)
        {
            newOutLineObject = outlineable.GetComponent<MeshRenderer>();
            Transform parent = outlineable.transform.parent;
            if (parent != null)
            {
                newInteractUI = parent.GetComponentInChildren<Canvas>(true);
            }
        }

        if (newOutLineObject != curOutlineScale)
        {
            if (curOutlineScale != null)
            {
                outlineBlock.SetFloat(outlineProperty, offOutlineScale);
                curOutlineScale.SetPropertyBlock(outlineBlock, 1);

                Debug.Log("���");
            }
            if (curInteractUI != null)
            {
                curInteractUI.gameObject.SetActive(false);
            }

            if (newOutLineObject != null)
            {
                outlineBlock.SetFloat(outlineProperty, onOutlineScale);
                newOutLineObject.SetPropertyBlock(outlineBlock, 1);

                if (newInteractUI != null)
                {
                    newInteractUI.gameObject.SetActive(true);
                }
                Debug.Log("�ѱ�");
            }

            curOutlineScale = newOutLineObject;
            curInteractUI = newInteractUI;
        }

    }
    public void SetRay(bool ray)
    {
        isOnRay = ray;

        if (!isOnRay)
        {
            ShowOutLine(null);
        }
    }
}
