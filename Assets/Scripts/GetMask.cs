using UnityEngine;

public class GetMask : MonoBehaviour
{
    public GameObject guideUI;
    private AudioSource noise;
    private bool broken;
    private void Awake()
    {
        if(guideUI != null) guideUI.SetActive(false);
    }

    private void Start()
    {
        if (SoundManager.Instance == null)
            return;
        noise = SoundManager.Instance.Play3DSFX(SoundManager.Instance.Data.abnormalMaskNoise,
            transform.position, 12f, true);
        if (noise != null)
        {
            noise.minDistance = 0.5f;
            noise.rolloffMode = AudioRolloffMode.Linear;
            noise.transform.SetParent(transform, true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryBreak(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryBreak(other);
    }

    private void TryBreak(Collider other)
    {
        // A low footprint trigger and the body's feet detect stepping, not a nearby head.
        if (broken || other.GetComponentInParent<PlayerMove>() == null
            || other.bounds.min.y > transform.position.y + 0.2f)
            return;
        if (StabilityManager.Instance == null || DaySystem.Instance == null)
            return;
        int day = DaySystem.Instance.GetNowDay() - 1;
        if (day < 0 || day >= StabilityManager.Instance.dayGetMaskValue.Length)
            return;

        broken = true;
        StopNoise();
        SoundManager.Instance?.Play3DSFX(SoundManager.Instance.Data.abnormalMaskBreakCeramic,
            transform.position, 10f, false);
        StabilityManager.Instance.currentGetMask++;
        if (StabilityManager.Instance.currentGetMask >= StabilityManager.Instance.dayGetMaskValue[day])
        {
            StabilityManager.Instance.currentGetMask = 0;
            GameManager.Instance.anomalySystem.ClearMission(1);
        }
        Destroy(gameObject);
    }

    private void OnDisable() => StopNoise();

    private void StopNoise()
    {
        if (noise == null)
            return;
        if (SoundManager.Instance != null)
            SoundManager.Instance.StopSFX(noise);
        else
            Destroy(noise.gameObject);
        noise = null;
    }

}
