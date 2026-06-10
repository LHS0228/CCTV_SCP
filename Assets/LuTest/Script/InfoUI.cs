using UnityEngine;

/// <summary>
/// 상호작용 안내 UI가 플레이어 카메라를 향하도록 회전시키는 책임을 가진다.
/// </summary>
public class InfoUI : MonoBehaviour
{
    [SerializeField]
    private Transform maincam;

    [SerializeField]
    private bool lockYAxis = true;

    void OnEnable()
    {
        EnsureCamera();
    }

    void LateUpdate()
    {
        EnsureCamera();

        if (maincam == null)
        {
            return;
        }

        if (lockYAxis)
        {
            Vector3 direction = transform.position - maincam.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            return;
        }

        transform.LookAt(transform.position + maincam.rotation * Vector3.forward,
                         maincam.rotation * Vector3.up);
    }

    private void EnsureCamera()
    {
        if (maincam != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            maincam = mainCamera.transform;
        }
    }
}
