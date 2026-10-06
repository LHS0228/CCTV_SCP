using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Play Mode controls. This entire window is compiled only into the Editor assembly.</summary>
public sealed class RuntimeTestWindow : EditorWindow
{
    private const string AssetRoot = "Assets/Editor/RuntimeTesting/";
    [SerializeField] private int targetHour = 5;
    [SerializeField] private int targetMinute = 59;
    [SerializeField] private string selectedAnomalyName;
    private BasicEventAnomaly selectedAnomaly;
    private Label status, activeList;
    private HelpBox message;
    private VisualElement actions;
    private IntegerField hour, minute;
    private DropdownField anomalyPicker;
    private Toggle pauseClock, suspendSpawns;
    private Button setClock, finishDay, trigger;
    private BasicEventAnomaly[] choices = new BasicEventAnomaly[0];
    private AnomalySystem currentSystem;
    private double nextRefresh;

    [MenuItem("Tools/Testing/Runtime Test")]
    public static void Open()
    {
        var window = GetWindow<RuntimeTestWindow>();
        window.titleContent = new GUIContent("런타임 테스트");
        window.minSize = new Vector2(420, 440);
    }

    private void OnEnable() => EditorApplication.update += Tick;
    private void OnDisable() => EditorApplication.update -= Tick;

    public void CreateGUI()
    {
        AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetRoot + "RuntimeTestWindow.uxml").CloneTree(rootVisualElement);
        rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetRoot + "RuntimeTestWindow.uss"));
        status = rootVisualElement.Q<Label>("status");
        activeList = rootVisualElement.Q<Label>("active-list");
        message = rootVisualElement.Q<HelpBox>("message");
        actions = rootVisualElement.Q<VisualElement>("actions");
        hour = rootVisualElement.Q<IntegerField>("hour");
        minute = rootVisualElement.Q<IntegerField>("minute");
        anomalyPicker = rootVisualElement.Q<DropdownField>("anomaly");
        pauseClock = rootVisualElement.Q<Toggle>("pause-clock");
        suspendSpawns = rootVisualElement.Q<Toggle>("suspend-spawns");
        setClock = rootVisualElement.Q<Button>("set-clock");
        finishDay = rootVisualElement.Q<Button>("finish-day");
        trigger = rootVisualElement.Q<Button>("trigger");
        hour.SetValueWithoutNotify(targetHour);
        minute.SetValueWithoutNotify(targetMinute);
        hour.RegisterValueChangedCallback(evt => targetHour = evt.newValue);
        minute.RegisterValueChangedCallback(evt => targetMinute = evt.newValue);
        setClock.clicked += () => ChangeClock(Mathf.Clamp(targetHour, 0, 6) * 60 + Mathf.Clamp(targetMinute, 0, 59));
        finishDay.clicked += () => ChangeClock(360);
        pauseClock.RegisterValueChangedCallback(evt =>
        {
            if (IsReady())
                GameManager.Instance.isTimeStop = evt.newValue;
        });
        suspendSpawns.RegisterValueChangedCallback(evt =>
        {
            if (!IsReady()) return;
            currentSystem.EditorSuspendNaturalSpawns = evt.newValue;
            currentSystem.EditorRescheduleSpawns();
        });
        anomalyPicker.RegisterValueChangedCallback(evt =>
        {
            if (anomalyPicker.index >= 0 && anomalyPicker.index < choices.Length)
            {
                selectedAnomaly = choices[anomalyPicker.index];
                selectedAnomalyName = selectedAnomaly.name;
            }
        });
        trigger.clicked += Trigger;
        rootVisualElement.Q<Button>("clear").clicked += () =>
        {
            if (!IsReady()) return;
            currentSystem.EditorClearAnomalies();
            ShowMessage("활성 이상현상을 해결 처리했습니다.", false);
            Refresh();
        };
        Refresh();
    }

    private bool IsReady() => EditorApplication.isPlaying && !EditorApplication.isCompiling
        && DaySystem.Instance != null && GameManager.Instance != null && currentSystem != null;

    private void Tick()
    {
        if (status == null || EditorApplication.timeSinceStartup < nextRefresh)
            return;
        nextRefresh = EditorApplication.timeSinceStartup + 0.2;
        Refresh();
    }

    private void Refresh()
    {
        AnomalySystem system = GameManager.Instance != null ? GameManager.Instance.anomalySystem : null;
        if (currentSystem != system)
        {
            currentSystem = system;
            choices = system == null ? new BasicEventAnomaly[0]
                : (system.standardEventPool ?? new BasicEventAnomaly[0])
                    .Concat(system.specialEventPool ?? new BasicEventAnomaly[0]).Where(item => item != null).Distinct().ToArray();
            anomalyPicker.choices = choices.Length == 0 ? new List<string> { "게임 씬을 실행하세요" }
                : choices.Select(item => $"{(system.specialEventPool.Contains(item) ? "특수" : "일반")} · "
                    + $"{(string.IsNullOrWhiteSpace(item.eventName) ? item.name : item.eventName)} · {item.eventPlace}").ToList();
            int index = System.Array.FindIndex(choices, item => item.name == selectedAnomalyName);
            if (index < 0 && choices.Length > 0)
                index = 0;
            selectedAnomaly = index >= 0 ? choices[index] : null;
            anomalyPicker.SetValueWithoutNotify(anomalyPicker.choices[Mathf.Max(index, 0)]);
        }
        bool ready = IsReady();
        actions.SetEnabled(ready);
        if (!ready)
        {
            status.text = "게임 씬을 Play Mode로 실행하면 사용할 수 있습니다.";
            activeList.text = "활성 이상현상 없음";
            return;
        }
        DaySystem day = DaySystem.Instance;
        GameManager game = GameManager.Instance;
        status.text = $"{day.GetNowDay()}일차 · {day.GetClockText()}  |  "
            + (day.EditorIsDayClear ? "근무 종료" : game.isGameStop ? "게임 진행 대기" : game.isTimeStop ? "시간 정지" : "진행 중");
        pauseClock.SetValueWithoutNotify(game.isTimeStop);
        suspendSpawns.SetValueWithoutNotify(currentSystem.EditorSuspendNaturalSpawns);
        setClock.SetEnabled(!day.EditorIsDayClear);
        finishDay.SetEnabled(!day.EditorIsDayClear);
        trigger.SetEnabled(!day.EditorIsDayClear && !game.isDeadWarring && choices.Length > 0);
        IEnumerable<string> running = currentSystem.activeStandardAnomalies.Select(item => item.eventScript.name);
        if (currentSystem.EditorActiveSpecialAnomaly != null)
            running = running.Concat(new[] { currentSystem.EditorActiveSpecialAnomaly.eventScript.name });
        activeList.text = "활성 이상현상: " + (running.Any() ? string.Join(", ", running) : "없음");
    }

    private void ChangeClock(int total)
    {
        if (!IsReady()) return;
        total = Mathf.Clamp(total, 0, 360);
        if (!DaySystem.Instance.EditorSetClock(total))
        {
            ShowMessage("근무가 종료된 뒤에는 시간을 바꿀 수 없습니다.", true);
            return;
        }
        currentSystem.EditorRescheduleSpawns();
        targetHour = total / 60;
        targetMinute = total % 60;
        hour.SetValueWithoutNotify(targetHour);
        minute.SetValueWithoutNotify(targetMinute);
        ShowMessage(total == 360 && GameManager.Instance.isGameStop
            ? "06:00으로 설정했습니다. 게임 진행 대기가 풀리면 근무 종료가 실행됩니다."
            : $"시간을 {DaySystem.Instance.GetClockText()}으로 변경했습니다.", false);
        Refresh();
    }

    private void Trigger()
    {
        if (!IsReady()) return;
        bool success = currentSystem.EditorTriggerAnomaly(selectedAnomaly, out string result);
        ShowMessage(result, !success);
        Refresh();
    }

    private void ShowMessage(string text, bool error)
    {
        message.text = text;
        message.messageType = error ? HelpBoxMessageType.Warning : HelpBoxMessageType.Info;
    }
}
