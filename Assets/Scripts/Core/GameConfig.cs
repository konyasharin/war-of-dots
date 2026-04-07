using UnityEngine;

namespace DotWars.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "DotWars/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Economy")]
        public int startingGold = 300;
        public float cityIncomePerSec = 10f;
        public float capitalIncomePerSec = 20f;

        [Header("Capture")]
        public float captureTime = 5f;

        [Header("Victory")]
        [Range(0f, 1f)]
        public float victoryTerritoryPercent = 0.8f;

        [Header("Combat")]
        public float detectionRadius = 1.5f;

        [Header("Morale")]
        public float maxMorale = 100f;
        public float moraleLossInCombat = 5f;
        public float moraleRecovery = 10f;
        public float hpRecovery = 3f;

        [Header("Ships")]
        public int shipConversionCost = 50;
        public float shipConversionTime = 10f;
    }
}
