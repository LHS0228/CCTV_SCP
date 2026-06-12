using System;
using Steamworks;
using UnityEngine;

public sealed class SteamManager : MonoBehaviour
{
    private static SteamManager instance;

    public static bool Initialized { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeSteam();
    }

    private void Update()
    {
        if (Initialized)
        {
            SteamAPI.RunCallbacks();
        }
    }

    private void OnDestroy()
    {
        if (instance != this)
        {
            return;
        }

        if (Initialized)
        {
            SteamAPI.Shutdown();
            Initialized = false;
        }

        instance = null;
    }

    private static void InitializeSteam()
    {
        try
        {
            if (SteamAPI.RestartAppIfNecessary(AppId_t.Invalid))
            {
                Application.Quit();
                return;
            }

            Initialized = SteamAPI.Init();
        }
        catch (DllNotFoundException exception)
        {
            Debug.LogError($"Steam initialization failed because a native Steamworks library was not found. {exception}");
            Initialized = false;
            return;
        }

        if (!Initialized)
        {
            Debug.LogWarning("SteamAPI.Init failed. Make sure Steam is running and steam_appid.txt contains the correct App ID.");
        }
    }
}
