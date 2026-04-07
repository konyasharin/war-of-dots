using UnityEngine;
using DotWars.Units;
using DotWars.Map;
using DotWars.CameraSystem;

namespace DotWars.Core
{
    public class GameSetup : MonoBehaviour
    {
        private void Start()
        {
            if (GameManager.Instance == null) return;

            GameManager.Instance.StartGame();
            SpawnStartingUnits();

            var map = MapManager.Instance;
            if (map != null)
            {
                var cam = UnityEngine.Camera.main?.GetComponent<CameraController>();
                if (cam != null)
                    cam.CenterOn((map.WorldMin + map.WorldMax) * 0.5f);
            }
        }

        private void SpawnStartingUnits()
        {
            var spawner = DivisionSpawner.Instance;
            if (spawner == null || MapManager.Instance == null) return;

            spawner.Spawn(DivisionType.Infantry, 0, new Vector2Int(5, 8));
            spawner.Spawn(DivisionType.Infantry, 0, new Vector2Int(6, 10));
            spawner.Spawn(DivisionType.Infantry, 0, new Vector2Int(5, 12));
            spawner.Spawn(DivisionType.Tank, 0, new Vector2Int(7, 10));

            spawner.Spawn(DivisionType.Infantry, 1, new Vector2Int(24, 8));
            spawner.Spawn(DivisionType.Infantry, 1, new Vector2Int(23, 10));
            spawner.Spawn(DivisionType.Infantry, 1, new Vector2Int(24, 12));
            spawner.Spawn(DivisionType.Tank, 1, new Vector2Int(22, 10));
        }
    }
}
