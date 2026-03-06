using UnityEngine;

namespace Match3Wizard
{
    /// <summary>
    /// Central config asset. Create via Assets > Create > Match3Wizard > GameConfig.
    /// Expose every balance variable here so designers can tweak without recompiling.
    /// </summary>
    [CreateAssetMenu(menuName = "Match3Wizard/GameConfig", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Board")]
        public int boardWidth       = 8;
        public int boardHeight      = 8;
        public float matchDelay     = 0.08f;   // seconds between cascade steps
        public float dropSpeed      = 10f;     // units/second for falling pieces

        [Header("Element Distribution (must sum to 1)")]
        [Range(0f, 1f)] public float firePct   = 0.22f;
        [Range(0f, 1f)] public float waterPct  = 0.22f;
        [Range(0f, 1f)] public float airPct    = 0.22f;
        [Range(0f, 1f)] public float earthPct  = 0.22f;
        [Range(0f, 1f)] public float lightPct  = 0.12f;

        [Header("Match -> Mana")]
        public int mana3  = 3;
        public int mana4  = 5;
        public int mana5  = 8;
        public int mana6  = 12;

        [Header("Match -> Magic Stones")]
        public int stonesForMatch4 = 1;
        public int stonesForMatch6 = 2;   // match 6+

        [Header("Mana Caps (0 = unlimited)")]
        public int manaCap = 99;

        [Header("Light Spell Cost")]
        public int lightCostPerElement = 15;

        [Header("Spell Base Costs")]
        public int spellCostFire  = 20;
        public int spellCostWater = 18;
        public int spellCostAir   = 18;
        public int spellCostEarth = 16;

        [Header("Crystals Drop")]
        public float crystalsPerPieceSpell    = 1f;
        public float crystalsPerPieceCascade  = 0.5f;

        [Header("Match Timer")]
        public float matchDuration = 90f;

        [Header("Spirit Goals (total crystals)")]
        public int spiritGoalFire  = 500;
        public int spiritGoalWater = 500;
        public int spiritGoalAir   = 500;
        public int spiritGoalEarth = 500;
        public int spiritGoalLight = 750;

        [Header("Gallery Part Costs")]
        public int[] galleryPartCosts = { 50, 100, 200, 350, 500 };

        [Header("Spell Level Upgrade Costs (crystals)")]
        public int[] spellUpgradeCosts = { 0, 10, 25, 50, 100 }; // index = target level

        // ── Helpers ───────────────────────────────────────────────────────────

        public int GetSpiritGoal(ElementType e) => e switch
        {
            ElementType.Fire  => spiritGoalFire,
            ElementType.Water => spiritGoalWater,
            ElementType.Air   => spiritGoalAir,
            ElementType.Earth => spiritGoalEarth,
            ElementType.Light => spiritGoalLight,
            _                 => 500
        };

        public int GetSpellBaseCost(ElementType e) => e switch
        {
            ElementType.Fire  => spellCostFire,
            ElementType.Water => spellCostWater,
            ElementType.Air   => spellCostAir,
            ElementType.Earth => spellCostEarth,
            ElementType.Light => 0, // handled separately
            _                 => 20
        };

        /// <summary>Returns mana generated for a match of <paramref name="count"/> pieces.</summary>
        public int GetManaForMatch(int count)
        {
            if (count >= 6) return mana6;
            if (count == 5) return mana5;
            if (count == 4) return mana4;
            return mana3;
        }

        /// <summary>Returns how many Magic Stones spawn for a match of <paramref name="count"/> pieces.</summary>
        public int GetStonesForMatch(int count)
        {
            if (count >= 6) return stonesForMatch6;
            if (count >= 4) return stonesForMatch4;
            return 0;
        }

        public ElementType GetRandomElement()
        {
            float r = Random.value;
            float acc = firePct;
            if (r < acc) return ElementType.Fire;
            acc += waterPct;
            if (r < acc) return ElementType.Water;
            acc += airPct;
            if (r < acc) return ElementType.Air;
            acc += earthPct;
            if (r < acc) return ElementType.Earth;
            return ElementType.Light;
        }
    }
}
