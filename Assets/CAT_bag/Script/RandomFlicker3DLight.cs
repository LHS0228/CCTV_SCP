using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class OldLampFlickerLight : MonoBehaviour
{
    [Header("Target Light")]
    public Light targetLight;              // 비워두면 자동으로 자기 Light 사용

    [Header("Intensity Settings")]
    public float baseIntensity = 2.0f;     // 평소 밝기
    public float minIntensity = 0.0f;      // 거의 꺼진 상태 (0 = 완전 꺼짐)
    public float maxIntensity = 2.5f;      // 순간적으로 번쩍일 때 최대 밝기

    [Tooltip("값이 클수록 전체 깜빡임이 빨라짐 (1 = 기본)")]
    public float flickerSpeed = 1.0f;      // 낡은 전등 전체 속도 조절용

    [Header("Color Settings")]
    public Color baseColor = new Color(1.0f, 0.95f, 0.8f); // 따뜻한 전구색
    public bool enableColorJitter = true;  // 색깔도 약간씩 흔들리게
    [Range(0f, 0.2f)]
    public float colorJitterAmount = 0.04f;

    private float currentIntensity;
    private Coroutine flickerRoutine;

    private void Awake()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();

        if (targetLight != null)
        {
            currentIntensity = baseIntensity;
            targetLight.intensity = baseIntensity;
            targetLight.color = baseColor;
        }

        if (flickerSpeed < 0.01f)
            flickerSpeed = 0.01f;
    }

    private void OnEnable()
    {
        StartFlicker();
    }

    private void OnDisable()
    {
        StopFlicker();
    }

    private void OnValidate()
    {
        if (flickerSpeed < 0.01f)
            flickerSpeed = 0.01f;

        if (maxIntensity < baseIntensity)
            maxIntensity = baseIntensity;

        if (minIntensity < 0f)
            minIntensity = 0f;
    }

    public void StartFlicker()
    {
        if (targetLight == null)
            return;

        if (flickerRoutine != null)
            StopCoroutine(flickerRoutine);

        flickerRoutine = StartCoroutine(FlickerRoutine());
    }

    public void StopFlicker()
    {
        if (flickerRoutine != null)
            StopCoroutine(flickerRoutine);

        flickerRoutine = null;

        if (targetLight != null)
        {
            targetLight.enabled = true;
            targetLight.intensity = baseIntensity;
            targetLight.color = baseColor;
        }
    }

    private IEnumerator FlickerRoutine()
    {
        while (true)
        {
            // 상태 랜덤 결정
            float r = Random.value;
            float targetIntensity;

            if (r < 0.55f)
            {
                // 대부분은 평범한 밝기 근처
                targetIntensity = Random.Range(baseIntensity * 0.8f, baseIntensity * 1.05f);
            }
            else if (r < 0.75f)
            {
                // 살짝 어두워지는 구간
                targetIntensity = Random.Range(baseIntensity * 0.3f, baseIntensity * 0.7f);
            }
            else if (r < 0.9f)
            {
                // 번쩍! 살짝 더 밝아지는 구간
                targetIntensity = Random.Range(baseIntensity * 1.1f, maxIntensity);
            }
            else
            {
                // 거의 꺼지거나 완전 꺼짐
                targetIntensity = Random.Range(minIntensity, baseIntensity * 0.2f);
            }

            float duration = Random.Range(0.02f, 0.15f) / flickerSpeed;
            float t = 0f;
            float startIntensity = currentIntensity;

            if (duration < 0.005f)
            {
                currentIntensity = targetIntensity;
                ApplyToLight();
                yield return null;
            }
            else
            {
                while (t < duration)
                {
                    t += Time.deltaTime;
                    float lerp = t / duration;
                    currentIntensity = Mathf.Lerp(startIntensity, targetIntensity, lerp);
                    ApplyToLight();
                    yield return null;
                }
            }

            // 가끔 완전 꺼진 상태 유지 (툭, 꺼졌다가 다시 들어오는 느낌)
            if (r > 0.88f)
            {
                targetLight.intensity = 0f;
                targetLight.enabled = true; // enabled는 켜두고 밝기만 0
                yield return new WaitForSeconds(Random.Range(0.04f, 0.12f) / flickerSpeed);
            }
        }
    }

    private void ApplyToLight()
    {
        if (targetLight == null)
            return;

        float clamped = Mathf.Clamp(currentIntensity, 0f, maxIntensity);
        targetLight.intensity = clamped;
        targetLight.enabled = clamped > 0.01f;

        if (enableColorJitter)
        {
            float j = Random.Range(-colorJitterAmount, colorJitterAmount);

            float r = Mathf.Clamp01(baseColor.r + j);
            float g = Mathf.Clamp01(baseColor.g + j * 0.5f);
            float b = Mathf.Clamp01(baseColor.b - j * 0.4f);

            targetLight.color = new Color(r, g, b);
        }
        else
        {
            targetLight.color = baseColor;
        }
    }
}
