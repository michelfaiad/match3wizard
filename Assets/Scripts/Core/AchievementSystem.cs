using System.Collections.Generic;
using UnityEngine;

namespace Match3Wizard
{
    /// <summary>
    /// Tracks all 12 MVP achievements and relays them to SteamManager.
    /// Each Check method is a no-op if already unlocked.
    /// </summary>
    public class AchievementSystem : MonoBehaviour
    {
        public static AchievementSystem Instance { get; private set; }

        // Steam achievement IDs (must match Steamworks dashboard)
        private const string ACH_01 = "ACH_01"; // Primeira Chama
        private const string ACH_02 = "ACH_02"; // Mago Aprendiz
        private const string ACH_03 = "ACH_03"; // Cadeia Elemental
        private const string ACH_04 = "ACH_04"; // Espirito da Chama
        private const string ACH_05 = "ACH_05"; // Espirito das Aguas
        private const string ACH_06 = "ACH_06"; // Espirito dos Ventos
        private const string ACH_07 = "ACH_07"; // Espirito da Terra
        private const string ACH_08 = "ACH_08"; // Espirito da Luz
        private const string ACH_09 = "ACH_09"; // Galeria Completa
        private const string ACH_10 = "ACH_10"; // Grande Mago
        private const string ACH_11 = "ACH_11"; // Tempestade Elemental
        private const string ACH_12 = "ACH_12"; // Colecionador

        // Track spells cast this session for ACH_03
        private readonly HashSet<ElementType> _spellsCastThisMatch = new();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Hooks called by other systems ─────────────────────────────────────

        public void OnMatchStarted()
        {
            _spellsCastThisMatch.Clear();
        }

        public void OnEndOfMatch()
        {
            // ACH_01: First match completed
            Unlock(ACH_01);
        }

        public void OnSpellCast(ElementType e)
        {
            Unlock(ACH_02); // First spell ever

            if (e == ElementType.Light)
                Unlock(ACH_11); // Aurora / Tempestade Elemental

            _spellsCastThisMatch.Add(e);
            if (_spellsCastThisMatch.Count >= 3)
                Unlock(ACH_03); // 3 different spells in one match
        }

        public void OnSpiritFreed(ElementType e)
        {
            string id = e switch
            {
                ElementType.Fire  => ACH_04,
                ElementType.Water => ACH_05,
                ElementType.Air   => ACH_06,
                ElementType.Earth => ACH_07,
                ElementType.Light => ACH_08,
                _                 => null
            };
            if (id != null) Unlock(id);
        }

        public void OnGalleryPartRevealed(int illustrationIndex)
        {
            // ACH_12: First part of all 7 illustrations revealed
            bool allHaveAtLeastOne = true;
            for (int i = 0; i < ProgressionSystem.IllustrationCount; i++)
                if (SaveSystem.Current.galleryPartsRevealed[i] < 1) { allHaveAtLeastOne = false; break; }
            if (allHaveAtLeastOne) Unlock(ACH_12);

            // ACH_09: All illustrations fully revealed
            if (ProgressionSystem.Instance.AreAllIllustrationsComplete())
                Unlock(ACH_09);
        }

        public void OnSpellUpgraded()
        {
            // ACH_10: All 5 spells at max level
            bool allMax = true;
            for (int i = 0; i < 5; i++)
                if (SaveSystem.GetSpellLevel((ElementType)i) < 5) { allMax = false; break; }
            if (allMax) Unlock(ACH_10);
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void Unlock(string id)
        {
            if (SaveSystem.IsAchievementUnlocked(id)) return;
            SaveSystem.UnlockAchievement(id);
            SteamManager.Instance?.UnlockAchievement(id);
            Debug.Log($"[Achievement] Unlocked: {id}");
        }

        // Legacy alias used by GameManager
        public void CheckEndOfMatch() => OnEndOfMatch();
    }
}
