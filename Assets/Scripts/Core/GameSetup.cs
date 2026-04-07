using UnityEngine;
using DotWars.Units;

namespace DotWars.Core
{
    public class GameSetup : MonoBehaviour
    {
        private void Start()
        {
            GameManager.Instance.StartGame();
            SpawnStartingUnits();
        }

        private void SpawnStartingUnits()
        {
            var spawner = DivisionSpawner.Instance;
            if (spawner == null) return;

            // Player (blue, ownerIndex=0) — left side
            spawner.Spawn(DivisionType.Infantry, 0, new Vector2Int(5, 8));
            spawner.Spawn(DivisionType.Infantry, 0, new Vector2Int(6, 10));
            spawner.Spawn(DivisionType.Infantry, 0, new Vector2Int(5, 12));
            spawner.Spawn(DivisionType.Tank, 0, new Vector2Int(7, 10));

            // Bot (red, ownerIndex=1) — right side
            spawner.Spawn(DivisionType.Infantry, 1, new Vector2Int(24, 8));
            spawner.Spawn(DivisionType.Infantry, 1, new Vector2Int(23, 10));
            spawner.Spawn(DivisionType.Infantry, 1, new Vector2Int(24, 12));
            spawner.Spawn(DivisionType.Tank, 1, new Vector2Int(22, 10));
        }
    }
}
