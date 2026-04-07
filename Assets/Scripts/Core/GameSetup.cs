using System.Collections;
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
            if (GameManager.Instance == null)
            {
                Debug.LogError("[GameSetup] GameManager.Instance is null!");
                return;
            }

            GameManager.Instance.StartGame();
            SpawnStartingUnits();

            // Center camera on map
            var map = MapManager.Instance;
            if (map != null)
            {
                var cam = UnityEngine.Camera.main?.GetComponent<CameraController>();
                if (cam != null)
                {
                    var center = (map.WorldMin + map.WorldMax) * 0.5f;
                    cam.CenterOn(center);
                }
            }

            StartCoroutine(DebugPositionsNextFrame());
        }

        private IEnumerator DebugPositionsNextFrame()
        {
            yield return new WaitForFixedUpdate();
            var divisions = FindObjectsByType<Division>(FindObjectsSortMode.None);
            foreach (var d in divisions)
                Debug.Log($"[AfterPhysics] {d.name} pos={d.transform.position}");
        }

        private void SpawnStartingUnits()
        {
            var spawner = DivisionSpawner.Instance;
            if (spawner == null)
            {
                Debug.LogError("[GameSetup] DivisionSpawner.Instance is null!");
                return;
            }

            var map = MapManager.Instance;
            if (map == null)
            {
                Debug.LogError("[GameSetup] MapManager.Instance is null!");
                return;
            }

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
