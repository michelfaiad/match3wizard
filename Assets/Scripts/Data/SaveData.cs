using System;
using System.Collections.Generic;

namespace Match3Wizard
{
    [Serializable]
    public class SaveData
    {
        // Crystals accumulated per element (index = ElementType int value)
        public int[] totalCrystals = new int[5];

        // Mana accumulated per element
        public int[] totalMana = new int[5];

        // Spell level per element (1-5)
        public int[] spellLevels = new int[5] { 1, 1, 1, 1, 1 };

        // Spirits liberated
        public bool[] spiritsFreed = new bool[5];

        // Gallery: parts revealed per illustration (7 illustrations, 5 parts each)
        public int[] galleryPartsRevealed = new int[7];

        // Total sessions played
        public int sessionsPlayed;

        // Achievements unlocked
        public List<string> unlockedAchievements = new();

        // Timestamp of last save
        public long lastSaveTimestamp;
    }
}
