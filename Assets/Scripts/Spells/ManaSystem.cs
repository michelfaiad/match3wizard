using System;
using System.Collections.Generic;
using UnityEngine;

namespace Match3Wizard
{
    /// <summary>
    /// Tracks mana per element during a session.
    /// Fires events consumed by UIManager (bar updates) and SpellSystem (can-cast checks).
    /// </summary>
    public class ManaSystem : MonoBehaviour
    {
        public static ManaSystem Instance { get; private set; }

        // Session mana (resets each match)
        private readonly int[] _sessionMana = new int[5];

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<ElementType, int> OnManaChanged;  // (element, newTotal)

        private GameConfig Config => GameManager.Instance.Config;

        // ── Unity ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void ResetSession()
        {
            for (int i = 0; i < 5; i++) _sessionMana[i] = 0;
            NotifyAll();
        }

        public void FlushSessionToSave()
        {
            for (int i = 0; i < 5; i++)
                SaveSystem.AddStoredMana((ElementType)i, _sessionMana[i]);
        }

        public int GetMana(ElementType e) => _sessionMana[(int)e];

        public bool CanActivateSpell(ElementType e)
        {
            if (e == ElementType.Light)
            {
                // Light costs from all 4 other elements
                int cost = Config.lightCostPerElement;
                for (int i = 0; i < 4; i++)
                    if (_sessionMana[i] < cost) return false;
                return true;
            }
            int baseCost = SpellSystem.Instance != null
                ? SpellSystem.Instance.GetCurrentCost(e)
                : Config.GetSpellBaseCost(e);
            return _sessionMana[(int)e] >= baseCost;
        }

        public bool TryConsumeMana(ElementType e)
        {
            if (!CanActivateSpell(e)) return false;

            if (e == ElementType.Light)
            {
                int cost = Config.lightCostPerElement;
                for (int i = 0; i < 4; i++) AddMana((ElementType)i, -cost);
            }
            else
            {
                int cost = SpellSystem.Instance != null
                    ? SpellSystem.Instance.GetCurrentCost(e)
                    : Config.GetSpellBaseCost(e);
                AddMana(e, -cost);
            }
            return true;
        }

        // Called by BoardManager after a match resolves
        public void OnMatchResolved(MatchResult match, bool isCascade)
        {
            if (!match.Element.GeneratesMana()) return;

            int mana = Config.GetManaForMatch(match.Count);
            AddMana(match.Element, mana);
        }

        // Called by SpellSystem to grant Light bonus mana
        public void AddBonusMana(int amountPerElement)
        {
            for (int i = 0; i < 4; i++)
                AddMana((ElementType)i, amountPerElement);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void AddMana(ElementType e, int delta)
        {
            int cap = Config.manaCap;
            int idx = (int)e;
            _sessionMana[idx] = Mathf.Clamp(_sessionMana[idx] + delta, 0, cap > 0 ? cap : int.MaxValue);
            OnManaChanged?.Invoke(e, _sessionMana[idx]);
        }

        private void NotifyAll()
        {
            for (int i = 0; i < 5; i++)
                OnManaChanged?.Invoke((ElementType)i, _sessionMana[i]);
        }
    }
}
