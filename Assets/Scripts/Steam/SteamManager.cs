using UnityEngine;

// ── Steamworks.NET integration ────────────────────────────────────────────────
// 1. Import Steamworks.NET from https://steamworks.github.io/ (or Unity Package Manager)
// 2. Add steam_appid.txt (with your App ID) to the project root
// 3. Uncomment the #define below after import

// #define STEAMWORKS_ENABLED

#if STEAMWORKS_ENABLED
using Steamworks;
#endif

namespace Match3Wizard
{
    /// <summary>
    /// Thin wrapper around Steamworks.NET.
    /// Handles: init, achievements, and cloud save sync.
    /// All calls are no-ops when Steamworks is not available (editor / DRM-free builds).
    /// </summary>
    public class SteamManager : MonoBehaviour
    {
        public static SteamManager Instance { get; private set; }

        private bool _steamInit;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitSteam();
        }

        private void OnDestroy()
        {
#if STEAMWORKS_ENABLED
            if (_steamInit) SteamAPI.Shutdown();
#endif
        }

        private void Update()
        {
#if STEAMWORKS_ENABLED
            if (_steamInit) SteamAPI.RunCallbacks();
#endif
        }

        // ── Init ──────────────────────────────────────────────────────────────

        private void InitSteam()
        {
#if STEAMWORKS_ENABLED
            if (!Packsize.Test())
            {
                Debug.LogError("[Steam] Packsize test failed. Wrong Steamworks.NET build?");
                return;
            }
            if (!DllCheck.Test())
            {
                Debug.LogError("[Steam] DllCheck failed. Missing steam_api64.dll?");
                return;
            }

            try
            {
                _steamInit = SteamAPI.Init();
                if (!_steamInit)
                    Debug.LogWarning("[Steam] SteamAPI.Init() failed. Running without Steam.");
                else
                    Debug.Log($"[Steam] Initialized. App: {SteamUtils.GetAppID()}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Steam] Exception during init: {e.Message}");
            }
#else
            Debug.Log("[Steam] Steamworks not enabled. Define STEAMWORKS_ENABLED to activate.");
#endif
        }

        // ── Achievements ──────────────────────────────────────────────────────

        public void UnlockAchievement(string apiName)
        {
#if STEAMWORKS_ENABLED
            if (!_steamInit) return;
            SteamUserStats.SetAchievement(apiName);
            SteamUserStats.StoreStats();
            Debug.Log($"[Steam] Achievement unlocked: {apiName}");
#else
            Debug.Log($"[Steam] (stub) Achievement: {apiName}");
#endif
        }

        public void ClearAchievement(string apiName)
        {
#if STEAMWORKS_ENABLED
            if (!_steamInit) return;
            SteamUserStats.ClearAchievement(apiName);
            SteamUserStats.StoreStats();
#endif
        }

        // ── Cloud Save ────────────────────────────────────────────────────────

        public void SyncCloudSave(string json)
        {
#if STEAMWORKS_ENABLED
            if (!_steamInit) return;
            byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
            SteamRemoteStorage.FileWrite("save.json", data, data.Length);
            Debug.Log("[Steam] Cloud save synced.");
#endif
        }

        public string LoadCloudSave()
        {
#if STEAMWORKS_ENABLED
            if (!_steamInit) return null;
            if (!SteamRemoteStorage.FileExists("save.json")) return null;
            int size = SteamRemoteStorage.GetFileSize("save.json");
            byte[] data = new byte[size];
            SteamRemoteStorage.FileRead("save.json", data, size);
            return System.Text.Encoding.UTF8.GetString(data);
#else
            return null;
#endif
        }
    }
}
