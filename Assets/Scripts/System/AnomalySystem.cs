using System.Collections.Generic;
using UnityEngine;

public class AnomalySystem : MonoBehaviour
{
    [Header("--- 타이머 설정 ---")]
    [SerializeField, Header("기본 이상현상 발생 주기 (분)")]
    private int anomaly_MinMinute = 24;
    [SerializeField]
    private int anomaly_MaxMinute = 45;

    [SerializeField, Header("특수(약물) 이상현상 발생 주기 (분)")]
    private int special_MinMinute = 30;
    [SerializeField]
    private int special_MaxMinute = 60;

    [SerializeField, Header("클리어 제한 시간 (리얼타임 초)")]
    private float anomaly_Threshold = 40;

    [Header("--- 참조 객체 ---")]
    public GameObject[] monsters; // 1:왼쪽, 2:가운데, 3:오른쪽
    public GameObject[] specialObjects; // 0:먹이통, 1:사이렌, 2:시작문, 3:엘리베이터문

    [Header("--- 이벤트 데이터 풀 ---")]
    public BasicEventAnomaly[] standardEventPool; // 일반 이상현상들 (불 끄기, 문 닫기 등)
    public BasicEventAnomaly[] specialEventPool;  // 특수 이상현상들 (약물 먹기 등)

    [Header("--- 현재 진행 중인 현황 (디버깅용) ---")]
    // 일반 이상현상 리스트 (최대 2개)
    public List<ActiveAnomaly> activeStandardAnomalies = new List<ActiveAnomaly>();
    // 특수 이상현상 (최대 1개, null이면 없는 상태)
    private ActiveAnomaly activeSpecialAnomaly = null;

    // 다음 발생 시간 (분 단위)
    private int nextStandardTime;
    private int nextSpecialTime;

    private void Start()
    {
        // 게임 시작 시 첫 발생 시간 설정
        SetNextStandardTime();
        SetNextSpecialTime();
    }

    private void Update()
    {
        // 1. 게임 정지 체크
        if (GameManager.Instance.AllStopCheck()) return;

        int currentClock = DaySystem.Instance.GetClock();

        // 2. 일반 이상현상 스폰 체크
        // 시간이 됐고 + 현재 2개 미만이며 + 빈 방이 있을 때
        if (currentClock > nextStandardTime
#if UNITY_EDITOR
            && !EditorSuspendNaturalSpawns
#endif
            )
        {
            if (activeStandardAnomalies.Count < 2 && GetAvailableRoomCount() > 0)
            {
                SpawnStandardAnomaly();
            }
            SetNextStandardTime(); // 발생 여부와 상관없이 다음 타이머 갱신 (쿨타임)
        }

        // 3. 특수 이상현상 스폰 체크
        // 시간이 됐고 + 현재 특수 현상이 없을 때
        if (currentClock > nextSpecialTime
#if UNITY_EDITOR
            && !EditorSuspendNaturalSpawns
#endif
            )
        {
            if (activeSpecialAnomaly == null)
            {
                SpawnSpecialAnomaly();
            }
            SetNextSpecialTime(); // 다음 타이머 갱신
        }

        // 4. 타이머 업데이트 및 타임오버 체크
        UpdateAnomaliesTimer();
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance.AllStopCheck()) return;

        // 게임 오버 상태면 모든 이상현상 강제 종료
        if (GameManager.Instance.isDeadWarring)
        {
            FailAllAnomalies();
            return;
        }

        // ====================================================================
        // [수정됨] 안정도 감소 통합 로직
        // 방 3개(0, 1, 2)를 돌면서 각 방의 상태에 따라 감소 속도를 적용합니다.
        // ====================================================================

        int currentDayIndex = DaySystem.Instance.GetNowDay() - 1; // 0부터 시작하게 보정

        for (int i = 0; i < 3; i++)
        {
            // 1. 현재 이 방(i)에 활성화된 '일반 이상현상'이 있는지 확인
            // mapValue는 (0:왼쪽, 1:중앙, 2:오른쪽)을 의미한다고 가정
            bool isStandardAnomalyActive = activeStandardAnomalies.Exists(anomaly => anomaly.mapValue == i);

            // 2. 이 방에 활성화된 '특수 이상현상'이 있는지 확인 (특수도 방 개념이 있다면)
            // 특수 이상현상은 mapValue가 999거나 10일 수 있으니 로직에 따라 포함/제외 결정
            // 여기서는 '일반 이상현상'만 안정도 가속에 영향을 준다고 가정했습니다.

            // 3. StabilityManager에게 계산 위임
            StabilityManager.Instance.UpdateStabilityDrain(i, currentDayIndex, isStandardAnomalyActive);
        }
    }

    // --- 핵심 로직: 타이머 갱신 및 실패 처리 ---
    private void UpdateAnomaliesTimer()
    {
        float dt = Time.deltaTime;

        // 1. 일반 이상현상 (기존 유지: 40초 지나면 실패)
        for (int i = activeStandardAnomalies.Count - 1; i >= 0; i--)
        {
            var anomaly = activeStandardAnomalies[i];
            anomaly.currentTimer += dt;

            if (anomaly.currentTimer >= anomaly_Threshold)
            {
                ProcessFail(anomaly, true);
            }
        }

        // 2. [수정됨] 특수 이상현상
        if (activeSpecialAnomaly != null)
        {
            activeSpecialAnomaly.currentTimer += dt;

            // ★ 수정된 부분: 타임오버 체크 로직을 제거했습니다.
            // 이제 시간이 아무리 흘러도 ProcessFail(실패)이 호출되지 않습니다.
            // 플레이어가 버튼을 눌러서 해결할 때까지 영원히 남아있습니다.

            /* (삭제된 코드)
            if (activeSpecialAnomaly.currentTimer >= anomaly_Threshold)
            {
                ProcessFail(activeSpecialAnomaly, false);
            }
            */
        }
    }

    // --- 스폰(발생) 로직 ---

    private void SpawnStandardAnomaly()
    {
        // 1. 현재 사용 중인 방 목록 만들기 (이 방에는 생성 불가)
        List<EventPlace> busyPlaces = new List<EventPlace>();
        foreach (var active in activeStandardAnomalies)
        {
            busyPlaces.Add(active.place);
        }

        // 2. 발생 가능한 이벤트 후보 추리기
        List<BasicEventAnomaly> validCandidates = new List<BasicEventAnomaly>();

        foreach (var candidateSO in standardEventPool)
        {
            // 조건 A: 이미 활성화된 이벤트(중복 몬스터)는 제외
            bool isAlreadyActive = activeStandardAnomalies.Exists(x => x.eventScript == candidateSO);
            if (isAlreadyActive) continue;

            // 조건 B: SO에 설정된 장소(candidateSO.eventPlace)가 이미 사용 중(busy)이면 제외
            // (즉, 오른쪽 방에 이미 몬스터가 있는데, 오른쪽 방 전용 이벤트를 또 실행하면 안 됨)
            if (busyPlaces.Contains(candidateSO.eventPlace)) continue;

            // 위 조건을 모두 통과한 녀석만 후보 리스트에 등록
            validCandidates.Add(candidateSO);
        }

        // 3. 발생 가능한 후보가 하나도 없으면 중단 (꽉 찼거나, 조건 맞는 게 없음)
        if (validCandidates.Count == 0)
        {
            Debug.Log("조건에 맞는 이상현상 후보가 없습니다. (방이 꽉 찼거나 중복)");
            return;
        }

        // 4. 후보 중에서 랜덤 하나 뽑기
        BasicEventAnomaly finalSelection = validCandidates[Random.Range(0, validCandidates.Count)];

        // 5. 실행! 
        // ★ 중요: 이제 script.eventPlace = place; 로 강제 할당하지 않습니다.
        // SO에 적혀있는 그 장소 그대로 실행됩니다.
        EventType type = finalSelection.Execute();

        // 6. 관리 리스트에 등록
        // 장소(Place)도 SO에 있는 것(finalSelection.eventPlace)을 그대로 가져옵니다.
        ActiveAnomaly newAnomaly = new ActiveAnomaly(finalSelection, finalSelection.eventPlace, type);
        activeStandardAnomalies.Add(newAnomaly);

        Debug.Log($"[일반] 이상현상 발생! SO: {finalSelection.name}, 위치: {finalSelection.eventPlace}");
    }

    private void SpawnSpecialAnomaly()
    {
        if (specialEventPool.Length == 0) return;

        // 특수 이벤트 선택
        BasicEventAnomaly script = specialEventPool[Random.Range(0, specialEventPool.Length)];

        // 실행 (특수는 장소를 스크립트 내부에서 정하거나, UI일 수 있음)
        EventType type = script.Execute();
        EventPlace place = script.eventPlace;

        // 특수 슬롯에 할당
        activeSpecialAnomaly = new ActiveAnomaly(script, place, type);
        Debug.Log($"[특수] 약물 이상현상 발생! 종류: {type}");
    }

    // --- 클리어(상호작용) 로직 ---
    // 버튼 클릭 시 호출되는 함수
    private void ProcessEventClear(EventType type, int index)
    {
        // 버튼 인덱스를 EventPlace enum 값으로 변환 (기존 로직 역산: index + 2)
        // 0->Left(2), 1->Center(3), 2->Right(4)
        int targetPlaceInt = index + 2;

        // 1. 일반 이상현상 리스트에서 일치하는 것 찾기
        ActiveAnomaly target = activeStandardAnomalies.Find(a => a.type == type && (int)a.place == targetPlaceInt);

        if (target != null)
        {
            Debug.Log("[성공] 일반 이상현상 해결!");
            target.eventScript.Clear();
            activeStandardAnomalies.Remove(target);
            return;
        }

        // 2. 특수 이상현상인지 확인
        // (특수는 장소와 상관없이 타입만 맞으면 되는지, 장소도 맞아야 하는지 기획에 따라 수정 가능)
        // 여기서는 타입과 장소 모두 맞아야 한다고 가정
        if (activeSpecialAnomaly != null && activeSpecialAnomaly.type == type)
        {
            // 특수가 UI형태라 장소 개념이 모호하다면 (int)activeSpecialAnomaly.place 체크는 제거하세요.
            if ((int)activeSpecialAnomaly.place == targetPlaceInt || activeSpecialAnomaly.place == EventPlace.All)
            {
                Debug.Log("[성공] 특수 이상현상 해결!");
                activeSpecialAnomaly.eventScript.Clear();
                activeSpecialAnomaly = null;
                return;
            }
        }

        // 3. 아무것도 해당 안 됨 -> 오답 패널티
        if (index == -1) return;
        Debug.Log("[오답] 이상현상이 아닌데 눌렀음 (-15)");
        StabilityManager.Instance?.StabilizationDown(15, index);
    }

    // --- 실패 처리 로직 ---
    // --- 실패 처리 로직 ---
    private void ProcessFail(ActiveAnomaly anomaly, bool isStandard)
    {
        Debug.Log($"이상현상 방어 실패 (타임오버): {anomaly.place}...");

        // [변경] 이미지 로직: 대처 실패 시 정해진 값(8)만큼 즉시 감소
        float dropAmount = StabilityManager.Instance.failureDropAmount;

        if (anomaly.mapValue >= 0 && anomaly.mapValue <= 2)
        {
            // 해당 방의 안정도만 8 감소
            StabilityManager.Instance?.StabilizationDown(dropAmount, anomaly.mapValue);
        }
        else if (anomaly.mapValue == 10)
        {
            // (예외 처리) 모든 방 감소가 필요한 특수 상황이라면
            for (int i = 0; i < 3; i++)
            {
                StabilityManager.Instance?.StabilizationDown(dropAmount, i);
            }
        }
        else
        {
            Debug.Log("None 상황 실패");
        }

        // (이하 기존 안전장치 및 제거 로직 동일)
        if (anomaly.eventScript != null)
        {
            try { anomaly.eventScript.Fail(); }
            catch (System.Exception e) { Debug.LogError(e.Message); }
        }

        if (isStandard)
        {
            if (activeStandardAnomalies.Contains(anomaly))
                activeStandardAnomalies.Remove(anomaly);
        }
        else
        {
            activeSpecialAnomaly = null;
        }
    }

    private void FailAllAnomalies()
    {
        foreach (var a in activeStandardAnomalies) a.eventScript.Fail();
        activeStandardAnomalies.Clear();

        if (activeSpecialAnomaly != null) activeSpecialAnomaly.eventScript.Fail();
        activeSpecialAnomaly = null;
    }

#if UNITY_EDITOR
    // Runtime state only: the test window does not change event assets or scene data.
    public bool EditorSuspendNaturalSpawns { get; set; }
    public ActiveAnomaly EditorActiveSpecialAnomaly => activeSpecialAnomaly;

    public bool EditorTriggerAnomaly(BasicEventAnomaly anomaly, out string message)
    {
        if (!Application.isPlaying || anomaly == null)
        {
            message = "Play Mode에서 이상현상을 선택하세요.";
            return false;
        }
        if (GameManager.Instance == null || GameManager.Instance.isDeadWarring
            || DaySystem.Instance == null || DaySystem.Instance.EditorIsDayClear)
        {
            message = "종료 또는 게임 오버 상태에서는 실행할 수 없습니다.";
            return false;
        }
        bool standard = System.Array.IndexOf(standardEventPool, anomaly) >= 0;
        bool special = System.Array.IndexOf(specialEventPool, anomaly) >= 0;
        if (!standard && !special)
        {
            message = "현재 씬의 이상현상 풀에 없는 항목입니다.";
            return false;
        }
        if (standard && (activeStandardAnomalies.Count >= 2
            || activeStandardAnomalies.Exists(active => active.eventScript == anomaly || active.place == anomaly.eventPlace)))
        {
            message = "일반 이상현상이 이미 실행 중이거나 해당 방이 사용 중입니다. 활성 이상현상을 정리한 뒤 실행하세요.";
            return false;
        }
        if (special && activeSpecialAnomaly != null)
        {
            message = "특수 이상현상이 이미 실행 중입니다. 활성 이상현상을 정리한 뒤 실행하세요.";
            return false;
        }
        EventType type = anomaly.Execute();
        var active = new ActiveAnomaly(anomaly, anomaly.eventPlace, type);
        if (standard)
            activeStandardAnomalies.Add(active);
        else
            activeSpecialAnomaly = active;
        message = $"{anomaly.name} 실행됨 ({anomaly.eventPlace}, {type})";
        return true;
    }
    public void EditorClearAnomalies()
    {
        if (!Application.isPlaying)
            return;
        foreach (ActiveAnomaly anomaly in activeStandardAnomalies)
            anomaly.eventScript.Clear();
        activeStandardAnomalies.Clear();
        if (activeSpecialAnomaly != null)
            activeSpecialAnomaly.eventScript.Clear();
        activeSpecialAnomaly = null;
    }
    public void EditorRescheduleSpawns()
    {
        if (!Application.isPlaying || DaySystem.Instance == null)
            return;
        SetNextStandardTime();
        SetNextSpecialTime();
    }
#endif

    // --- 유틸리티 함수 ---

    private void SetNextStandardTime()
    {
        int current = DaySystem.Instance.GetClock();
        nextStandardTime = Random.Range(current + anomaly_MinMinute, current + anomaly_MaxMinute);
    }

    private void SetNextSpecialTime()
    {
        int current = DaySystem.Instance.GetClock();
        nextSpecialTime = Random.Range(current + special_MinMinute, current + special_MaxMinute);
    }

    public void ClearProtocol()
    {
        foreach(ActiveAnomaly anomaly in activeStandardAnomalies)
        {
            anomaly.eventScript.Clear();
        }
        activeStandardAnomalies.Clear();
        activeSpecialAnomaly.eventScript.Clear();
        activeSpecialAnomaly = null;
    }

    // 빈 방 찾기 (중복 방지)
    private EventPlace GetRandomAvailablePlace()
    {
        List<EventPlace> available = new List<EventPlace> { EventPlace.LeftRoom, EventPlace.CenterRoom, EventPlace.RightRoom };

        // 현재 진행 중인 일반 이상현상들의 위치를 리스트에서 뺌
        foreach (var anomaly in activeStandardAnomalies)
        {
            available.Remove(anomaly.place);
        }

        if (available.Count == 0) return EventPlace.None;

        return available[Random.Range(0, available.Count)];
    }

    private int GetAvailableRoomCount()
    {
        return 3 - activeStandardAnomalies.Count;
    }

    // --- 유니티 버튼 연결부 (변경 없음) ---
    public void ClearCCTVSystemCheck(int index) {
        AudioClip clip = SoundManager.Instance.Data.ingameCctvSystemCheckButtonClick;
        SoundManager.Instance.PlayGlobalSFX(clip);
        ProcessEventClear(EventType.CCTV_SystemCheck, index); }
    public void ClearCCTVResonance(int index)
    {
        AudioClip clip = SoundManager.Instance.Data.ingameCctvResonanceButtonClick;
        SoundManager.Instance.PlayGlobalSFX(clip);
        ProcessEventClear(EventType.CCTV_Resonance, index); }
    public void ClearCCTVIncinerate(int index)
    {
        AudioClip clip = SoundManager.Instance.Data.ingameCctvBurnButtonClick;
        SoundManager.Instance.PlayGlobalSFX(clip);
        ProcessEventClear(EventType.CCTV_Incinerate, index); }
    public void ClearCCTVElectricity(int index)
    {
        AudioClip clip = SoundManager.Instance.Data.ingameCctvShockButtonClick;
        SoundManager.Instance.PlayGlobalSFX(clip);
        ProcessEventClear(EventType.CCTV_Electricity, index); }
    public void ClearCCTVFoodRefeel(int index) { 
        AudioClip clip = SoundManager.Instance.Data.ingameCctvFeedButtonClick;
        SoundManager.Instance.PlayGlobalSFX(clip);
        ProcessEventClear(EventType.CCTV_FoodRefeel, index); }
    public void ClearMission(int index) { ProcessEventClear(EventType.Mission, index); }
    public void ClearSpecial() { ProcessEventClear(EventType.Special, -1); }
}
