using System;
using UnityEngine;

namespace DotWars.Core
{
    public static class EventBus
    {
        // Game state
        public static Action<GameState, GameState> OnGameStateChanged;

        // Units
        public static Action<GameObject> OnDivisionSpawned;
        public static Action<GameObject> OnDivisionDestroyed;

        // Economy
        public static Action<int, int> OnGoldChanged; // playerIndex, newAmount
        public static Action<GameObject, int> OnCityCaptured; // city, newOwnerIndex

        public static void Reset()
        {
            OnGameStateChanged = null;
            OnDivisionSpawned = null;
            OnDivisionDestroyed = null;
            OnGoldChanged = null;
            OnCityCaptured = null;
        }
    }
}
