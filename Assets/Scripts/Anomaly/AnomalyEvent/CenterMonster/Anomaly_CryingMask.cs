using UnityEngine;

[CreateAssetMenu(menuName = "Anomalies/Event/CenterRoom/Anomaly_CryingMask")]
public class Anomaly_CryingMask : BasicEventAnomaly
{
    [Header("스크립트 들어가있는 가면 프리팹")]
    public GameObject sadMaskPreafab;
    [Header("각 생성 구역의 최소/최대 좌표")]
    public Vector3[] firstTransRange;
    public Vector3[] lastTransRange;
    [Header("가면 생성 제외 구역 (월드 X/Z 좌표)")]
    public Bounds[] excludedSpawnAreas = new Bounds[0];
    private static readonly Vector3 MaskHalfExtents = new Vector3(0.3f, 0.24f, 0.43f);
    private GameObject[] spawnObj;

    public override EventType Execute()
    {
        int day = DaySystem.Instance.GetNowDay() - 1;
        if (day < 0 || day >= StabilityManager.Instance.dayGetMaskValue.Length
            || sadMaskPreafab == null || firstTransRange.Length == 0
            || firstTransRange.Length != lastTransRange.Length)
        {
            Debug.LogError("우는 가면의 날짜 또는 생성 구역 설정을 확인하세요.");
            return eventType;
        }
        RemoveMasks();
        StabilityManager.Instance.currentGetMask = 0;
        int count = (int)StabilityManager.Instance.dayGetMaskValue[day];
        spawnObj = new GameObject[count];
        int zone = Random.Range(0, firstTransRange.Length);
        for (int i = 0; i < count; i++)
        {
            // Begin below desks, and ignore triggers/characters when finding the floor.
            bool placed = false;
            for (int attempt = 0; attempt < 40 && !placed; attempt++)
            {
                Vector3 point = new Vector3(
                    Random.Range(Mathf.Min(firstTransRange[zone].x, lastTransRange[zone].x), Mathf.Max(firstTransRange[zone].x, lastTransRange[zone].x)),
                    Mathf.Max(firstTransRange[zone].y, lastTransRange[zone].y),
                    Random.Range(Mathf.Min(firstTransRange[zone].z, lastTransRange[zone].z), Mathf.Max(firstTransRange[zone].z, lastTransRange[zone].z)));
                if (!Physics.Raycast(point + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit,
                    3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    || hit.normal.y < 0.95f || hit.collider.GetComponentInParent<PlayerMove>() != null)
                    continue;
                Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                if (IsInExcludedSpawnArea(hit.point, rotation))
                    continue;
                if (Physics.CheckBox(hit.point + Vector3.up * 0.26f,
                    MaskHalfExtents, rotation,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    continue;
                bool overlapsMask = false;
                for (int previous = 0; previous < i; previous++)
                    overlapsMask |= Vector3.Distance(spawnObj[previous].transform.position, hit.point) < 1f;
                if (overlapsMask)
                    continue;
                spawnObj[i] = Instantiate(sadMaskPreafab, hit.point, rotation);
                placed = true;
            }
            if (!placed)
            {
                RemoveMasks();
                Debug.LogError("우는 가면 생성 구역에서 바닥을 찾을 수 없습니다.");
                return eventType;
            }
        }
        if (count > 0)
            SoundManager.Instance?.PlayGlobalSFX(SoundManager.Instance.Data.abnormalMaskCreationWeepingMan);
        return eventType;
    }

    private bool IsInExcludedSpawnArea(Vector3 point, Quaternion rotation)
    {
        if (excludedSpawnAreas == null)
            return false;

        // Exclude the entire rotated footprint, including masks touching a table edge.
        Vector3 right = rotation * Vector3.right;
        Vector3 forward = rotation * Vector3.forward;
        float halfX = Mathf.Abs(right.x) * MaskHalfExtents.x + Mathf.Abs(forward.x) * MaskHalfExtents.z;
        float halfZ = Mathf.Abs(right.z) * MaskHalfExtents.x + Mathf.Abs(forward.z) * MaskHalfExtents.z;
        foreach (Bounds area in excludedSpawnAreas)
        {
            if (point.x + halfX >= area.min.x && point.x - halfX <= area.max.x
                && point.z + halfZ >= area.min.z && point.z - halfZ <= area.max.z)
                return true;
        }
        return false;
    }

    public override void Clear() => RemoveMasks();
    public override void Fail() => RemoveMasks();

    private void RemoveMasks()
    {
        if (spawnObj != null)
            foreach (GameObject mask in spawnObj)
                if (mask != null)
                    Destroy(mask);
        spawnObj = null;
    }
}
