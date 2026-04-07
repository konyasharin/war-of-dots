using UnityEngine;
using DotWars.Units;

namespace DotWars.Core
{
    public class GameSetup : MonoBehaviour
    {
        private void Start()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[GameSetup] GameManager.Instance is null!");
                return;
            }

            GameManager.Instance.StartGame();
            SpawnStartingUnits();
        }

        private void SpawnStartingUnits()
        {
            var spawner = DivisionSpawner.Instance;
            if (spawner == null)
            {
                Debug.LogError("[GameSetup] DivisionSpawner.Instance is null!");
                return;
            }

            var map = DotWars.Map.MapManager.Instance;
            if (map == null)
            {
                Debug.LogError("[GameSetup] MapManager.Instance is null!");
                return;
            }

            Debug.Log($"[GameSetup] Map bounds: {map.Bounds}, size: {map.Width}x{map.Height}");
            Debug.Log($"[GameSetup] IsPassable(5,8)={map.IsPassable(new Vector2Int(5, 8))}, IsPassable(6,10)={map.IsPassable(new Vector2Int(6, 10))}");

            // Player (blue, ownerIndex=0) — left side
            SpawnAndLog(spawner, DivisionType.Infantry, 0, new Vector2Int(5, 8));
            SpawnAndLog(spawner, DivisionType.Infantry, 0, new Vector2Int(6, 10));
            SpawnAndLog(spawner, DivisionType.Infantry, 0, new Vector2Int(5, 12));
            SpawnAndLog(spawner, DivisionType.Tank, 0, new Vector2Int(7, 10));

            // Bot (red, ownerIndex=1) — right side
            SpawnAndLog(spawner, DivisionType.Infantry, 1, new Vector2Int(24, 8));
            SpawnAndLog(spawner, DivisionType.Infantry, 1, new Vector2Int(23, 10));
            SpawnAndLog(spawner, DivisionType.Infantry, 1, new Vector2Int(24, 12));
            SpawnAndLog(spawner, DivisionType.Tank, 1, new Vector2Int(22, 10));
        }

        private void SpawnAndLog(DivisionSpawner spawner, DivisionType type, int owner, Vector2Int pos)
        {
            var div = spawner.Spawn(type, owner, pos);
            if (div == null)
                Debug.LogWarning($"[GameSetup] Failed to spawn {type} at {pos}");
            else
                Debug.Log($"[GameSetup] Spawned {type} at {pos} -> world {div.transform.position}");
        }
    }
}
