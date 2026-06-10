using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 프리팹화된 인게임 UI가 씬 전용 시스템 오브젝트의 메서드를 버튼 이벤트로 사용할 수 있도록 런타임에 연결하는 책임을 가진다.
/// </summary>
public static class IngameUICanvasSceneBinder
{
    private const string IngameCanvasName = "Ingame_UICanvas";
    private const string NextDayButtonName = "NextDayButton";
    private const string GoToTitleButtonName = "GoToTileButton";
    private const string RestartButtonName = "RestartButton";

    private static readonly HashSet<int> BoundButtonIds = new HashSet<int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        BindCurrentScene();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BoundButtonIds.Clear();
        BindCurrentScene();
    }

    private static void BindCurrentScene()
    {
        GameObject ingameCanvas = GameObject.Find(IngameCanvasName);
        if (ingameCanvas == null)
        {
            return;
        }

        GameManager gameManager = GameManager.Instance != null
            ? GameManager.Instance
            : Object.FindFirstObjectByType<GameManager>();
        DaySystem daySystem = Object.FindFirstObjectByType<DaySystem>();

        Button[] buttons = ingameCanvas.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            BindButton(button, gameManager, daySystem);
        }
    }

    private static void BindButton(Button button, GameManager gameManager, DaySystem daySystem)
    {
        if (button == null || BoundButtonIds.Contains(button.GetInstanceID()))
        {
            return;
        }

        switch (button.name)
        {
            case NextDayButtonName:
                if (daySystem != null)
                {
                    button.onClick.AddListener(daySystem.NextDayEnd);
                    BoundButtonIds.Add(button.GetInstanceID());
                }
                break;
            case GoToTitleButtonName:
            case RestartButtonName:
                if (gameManager != null)
                {
                    button.onClick.AddListener(gameManager.RestartGame);
                    BoundButtonIds.Add(button.GetInstanceID());
                }
                break;
        }
    }
}
