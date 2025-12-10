using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class StabilityManager : MonoBehaviour
{
    private static StabilityManager instance;
    public static StabilityManager Instance => instance;

    // 안정 수치 최대치 (100)
    static public float maxStability = 100;

    // 현재 안정수치들
    private float[] currentStability;
    public float[] CurrentStability => currentStability;

    [Header("일차별 초당 기본 감소량 (1일차: 0.37, 2일차: 0.392 ...)")]
    public float[] dayBaseDecayRates;

    [Header("이상현상 활성화 중일 때 추가되는 초당 감소량")]
    public float activeAnomalyExtraDrain = 1.0f;

    [Header("이상현상 대처 실패 시 즉시 감소하는 양")]
    public float failureDropAmount = 8.0f;

    [Header("=== 가면 설정 ===")]
    [Header("Day 별 주워야하는 가면 개수")]
    public float[] dayGetMaskValue;
    [HideInInspector]
    public float currentGetMask = 0;


    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        currentStability = new float[3];
        // 시작 시 모든 방 100으로 초기화
        for (int i = 0; i < 3; i++) currentStability[i] = maxStability;
    }

    bool dam = true;

    private void Update()
    {
        // 1. 이미 프로토콜이 진행 중이거나 죽는 중이면 아무것도 안 함
        if (!dam) return;

        // 2. 안정도 감소 로직 (UpdateStabilityDrain 등으로 감소되고 있다고 가정)

        // 3. 0이 되었는지 체크 (우선순위 순서대로)
        if (currentStability[2] <= 0)
        {
            ReportAnomaly(2);
        }
        else if (currentStability[1] <= 0)
        {
            ReportAnomaly(1);
        }
        else if (currentStability[0] <= 0)
        {
            ReportAnomaly(0);
        }
    }

    // 단발성 감소 (대처 실패, 오답 등)
    public void StabilizationDown(float value, int index)
    {
        currentStability[index] = Mathf.Clamp(currentStability[index] - value, 0f, maxStability);
    }

    public void StabilizationUp(float value, int index)
    {
        currentStability[index] = Mathf.Clamp(currentStability[index] + value, 0f, maxStability);
    }

    // =========================================================
    // [핵심 변경] 이미지 로직 적용된 안정도 감소 함수
    // =========================================================
    public void UpdateStabilityDrain(int roomIndex, int dayIndex, bool isAnomalyActive)
    {
        // 1. 현재 일차에 맞는 기본 감소량 가져오기 (예: 1일차면 0.37)
        float currentDrainRate = 0f;

        if (dayBaseDecayRates != null && dayIndex < dayBaseDecayRates.Length)
        {
            currentDrainRate = dayBaseDecayRates[dayIndex];
        }
        else
        {
            // 배열 인덱스 에러 방지용 기본값 (혹시 설정 안했을 경우)
            currentDrainRate = 0.37f;
        }

        // 2. 이상현상 활성화 시 +1 추가 (이미지 로직)
        if (isAnomalyActive)
        {
            currentDrainRate += activeAnomalyExtraDrain;
        }

        // 3. 최종 적용 (FixedUpdate에서 호출되므로 Time.fixedDeltaTime 곱함)
        currentStability[roomIndex] = Mathf.Clamp(currentStability[roomIndex] -= currentDrainRate * Time.fixedDeltaTime, 0f, maxStability);
    }

    private void ReportAnomaly(int index)
    {
        dam = false; // 일단 StabilityManager는 멈춤
        ProtocolSystem.instance.StartProtocol(index);
        // -> 이후 살지 죽을지는 ProtocolSystem이 결정함
    }

    public void ProtocolSuccess()
    {
        dam = true;
    }
}