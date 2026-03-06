using System;
using UnityEngine;

namespace Match3Wizard
{
    /// <summary>
    /// Drives the meta-game: spirit liberation and gallery illustration reveals.
    /// </summary>
    public class ProgressionSystem : MonoBehaviour
    {
        public static ProgressionSystem Instance { get; private set; }

        // Total illustrations and parts per illustration
        public const int IllustrationCount = 7;
        public const int PartsPerIllustration = 5;

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<ElementType>  OnSpiritFreed;
        public event Action<int, int>     OnGalleryPartRevealed; // (illustrationIndex, partIndex)

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Spirits ───────────────────────────────────────────────────────────

        /// <summary>Called at end of every match to check if any spirit thresholds were crossed.</summary>
        public void CheckSpiritUnlocks()
        {
            var save   = SaveSystem.Current;
            var config = GameManager.Instance.Config;

            for (int i = 0; i < 5; i++)
            {
                if (save.spiritsFreed[i]) continue;

                var element = (ElementType)i;
                int goal    = config.GetSpiritGoal(element);
                int current = SaveSystem.GetCrystals(element);

                if (current >= goal)
                {
                    save.spiritsFreed[i] = true;
                    OnSpiritFreed?.Invoke(element);
                    AchievementSystem.Instance?.OnSpiritFreed(element);
                    Debug.Log($"[Progression] Spirit freed: {element}");
                }
            }
        }

        public bool IsSpiritFreed(ElementType e) => SaveSystem.Current.spiritsFreed[(int)e];

        // ── Gallery ───────────────────────────────────────────────────────────

        public int GetPartsRevealed(int illustrationIndex) =>
            SaveSystem.Current.galleryPartsRevealed[illustrationIndex];

        public bool IsIllustrationComplete(int illustrationIndex) =>
            GetPartsRevealed(illustrationIndex) >= PartsPerIllustration;

        /// <summary>
        /// Try to reveal the next part of an illustration.
        /// The element used to pay is determined by which spirit the illustration belongs to
        /// (index 0-4 = element spirits; index 5 = Mago = any; index 6 = Reunião = any).
        /// </summary>
        public bool TryRevealNextPart(int illustrationIndex)
        {
            int partsRevealed = GetPartsRevealed(illustrationIndex);
            if (partsRevealed >= PartsPerIllustration)
            {
                Debug.Log("[Progression] Illustration already complete.");
                return false;
            }

            var config = GameManager.Instance.Config;
            int cost   = config.galleryPartCosts[partsRevealed]; // cumulative cost table

            // Determine which crystal type to spend
            ElementType? spendElement = GetCrystalTypeForIllustration(illustrationIndex);
            bool success;

            if (spendElement.HasValue)
            {
                success = CrystalSystem.Instance.TrySpendCrystals(spendElement.Value, cost);
            }
            else
            {
                // Illustrations 5 & 6: use any element (try each until one succeeds)
                success = false;
                for (int i = 0; i < 5 && !success; i++)
                    success = CrystalSystem.Instance.TrySpendCrystals((ElementType)i, cost);
            }

            if (!success) { Debug.Log("[Progression] Not enough crystals."); return false; }

            SaveSystem.Current.galleryPartsRevealed[illustrationIndex]++;
            SaveSystem.Save();

            OnGalleryPartRevealed?.Invoke(illustrationIndex, partsRevealed + 1);
            AchievementSystem.Instance?.OnGalleryPartRevealed(illustrationIndex);
            return true;
        }

        public bool AreAllIllustrationsComplete()
        {
            for (int i = 0; i < IllustrationCount; i++)
                if (!IsIllustrationComplete(i)) return false;
            return true;
        }

        // ── Illustration #7 unlock prerequisite ──────────────────────────────

        public bool CanUnlockReunionIllustration()
        {
            var save = SaveSystem.Current;
            // All 5 spirits freed + illustrations 1-6 complete
            for (int i = 0; i < 5; i++)
                if (!save.spiritsFreed[i]) return false;
            for (int i = 0; i < 6; i++)
                if (save.galleryPartsRevealed[i] < PartsPerIllustration) return false;
            return true;
        }

        // ── Illustration #6 (O Mago) unlock prerequisite ─────────────────────

        public bool CanUnlockMagoIllustration()
        {
            int count = 0;
            for (int i = 0; i < 5; i++)
                if (SaveSystem.GetSpellLevel((ElementType)i) >= 3) count++;
            return count >= 3;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private ElementType? GetCrystalTypeForIllustration(int idx) => idx switch
        {
            0 => ElementType.Fire,
            1 => ElementType.Water,
            2 => ElementType.Air,
            3 => ElementType.Earth,
            4 => ElementType.Light,
            _ => null  // 5 (Mago) and 6 (Reunião) = any element
        };
    }
}
