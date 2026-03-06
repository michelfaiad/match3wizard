using System;
using System.IO;
using UnityEngine;

namespace Match3Wizard
{
    /// <summary>
    /// Static helper for reading and writing SaveData to disk (JSON).
    /// Also exposes hooks for Steam Cloud Save (implemented in SteamManager).
    /// </summary>
    public static class SaveSystem
    {
        private static readonly string SavePath =
            Path.Combine(Application.persistentDataPath, "save.json");

        public static SaveData Current { get; private set; } = new SaveData();

        // ── Load / Save ───────────────────────────────────────────────────────

        public static void Load()
        {
            if (!File.Exists(SavePath))
            {
                Current = new SaveData();
                Debug.Log("[SaveSystem] No save found – starting fresh.");
                return;
            }

            try
            {
                string json = File.ReadAllText(SavePath);
                Current = JsonUtility.FromJson<SaveData>(json);
                Debug.Log("[SaveSystem] Loaded from disk.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Load error: {e.Message}");
                Current = new SaveData();
            }
        }

        public static void Save()
        {
            try
            {
                Current.lastSaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string json = JsonUtility.ToJson(Current, prettyPrint: true);
                File.WriteAllText(SavePath, json);
                Debug.Log("[SaveSystem] Saved to disk.");

                // Notify SteamManager to sync to cloud (if available)
                SteamManager.Instance?.SyncCloudSave(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Save error: {e.Message}");
            }
        }

        // ── Crystal helpers ───────────────────────────────────────────────────

        public static int GetCrystals(ElementType e)        => Current.totalCrystals[(int)e];
        public static void AddCrystals(ElementType e, int n) => Current.totalCrystals[(int)e] += n;

        // ── Mana helpers ──────────────────────────────────────────────────────

        public static int GetStoredMana(ElementType e)         => Current.totalMana[(int)e];
        public static void AddStoredMana(ElementType e, int n) => Current.totalMana[(int)e] += n;
        public static bool ConsumeStoredMana(ElementType e, int n)
        {
            if (Current.totalMana[(int)e] < n) return false;
            Current.totalMana[(int)e] -= n;
            return true;
        }

        // ── Spell helpers ─────────────────────────────────────────────────────

        public static int GetSpellLevel(ElementType e)           => Current.spellLevels[(int)e];
        public static void SetSpellLevel(ElementType e, int lvl) => Current.spellLevels[(int)e] = lvl;

        // ── Achievement helpers ───────────────────────────────────────────────

        public static bool IsAchievementUnlocked(string id) =>
            Current.unlockedAchievements.Contains(id);

        public static void UnlockAchievement(string id)
        {
            if (!Current.unlockedAchievements.Contains(id))
                Current.unlockedAchievements.Add(id);
        }
    }
}
