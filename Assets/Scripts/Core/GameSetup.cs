using System.Collections.Generic;
using UnityEngine;
using DotWars.Units;
using DotWars.Map;
using DotWars.CameraSystem;

namespace DotWars.Core
{
    public class GameSetup : MonoBehaviour
    {
        // City definitions: gridX, gridY, ownerIndex, isCapital
        private static readonly int[][] CityDefs =
        {
            new[] { 15, 30, 0, 1 },  // Player capital
            new[] { 10, 15, 0, 0 },  // Player city 2
            new[] { 10, 45, 0, 0 },  // Player city 3
            new[] { 35, 30, -1, 0 }, // Neutral center-left
            new[] { 84, 30, 1, 1 },  // Bot capital
            new[] { 89, 15, 1, 0 },  // Bot city 2
            new[] { 89, 45, 1, 0 },  // Bot city 3
            new[] { 64, 30, -1, 0 }, // Neutral center-right
        };

        // Port definitions: gridX, gridY, linked to city index
        private static readonly int[][] PortDefs =
        {
            new[] { 7, 30, 0 },   // Left coast, near player capital
            new[] { 7, 15, 1 },   // Left coast, near player city 2
            new[] { 92, 30, 4 },  // Right coast, near bot capital
            new[] { 92, 45, 5 },  // Right coast, near bot city 3
        };

        private Sprite _flagSprite;
        private Sprite _ringSprite;

        private void Start()
        {
            if (GameManager.Instance == null) return;

            _flagSprite = Resources.Load<Sprite>("Sprites/Flag");
            _ringSprite = Resources.Load<Sprite>("Sprites/Ring");

            GameManager.Instance.StartGame();

            var regions = SetupCitiesAndRegions();
            SetupPorts(regions);

            RegionManager.Instance?.RefreshAllVisuals();

            SpawnStartingUnits();

            // Center camera on player capital
            var map = MapManager.Instance;
            if (map != null)
            {
                var cam = UnityEngine.Camera.main?.GetComponent<CameraController>();
                if (cam != null)
                    cam.CenterOn(map.GridToWorld(new Vector2Int(15, 30)));
            }
        }

        private List<Region> SetupCitiesAndRegions()
        {
            var rm = RegionManager.Instance;
            var map = MapManager.Instance;
            var regions = new List<Region>();
            var cityCenters = new List<Vector2Int>();

            // Create regions and city objects
            foreach (var def in CityDefs)
            {
                int gx = def[0], gy = def[1], owner = def[2];
                bool isCapital = def[3] == 1;

                var region = rm?.CreateRegion(owner);
                cityCenters.Add(new Vector2Int(gx, gy));

                var cityGo = new GameObject(isCapital ? (owner == 0 ? "PlayerCapital" : "BotCapital") : $"City_{gx}_{gy}");
                cityGo.transform.position = map.GridToWorld(new Vector2Int(gx, gy));

                var outlineGo = new GameObject("Outline");
                outlineGo.transform.SetParent(cityGo.transform);
                outlineGo.transform.localPosition = Vector3.zero;
                outlineGo.transform.localScale = Vector3.one * 1.1f;
                var outlineSr = outlineGo.AddComponent<SpriteRenderer>();
                outlineSr.sprite = _ringSprite;
                outlineSr.sortingOrder = 2;

                var flagGo = new GameObject("Flag");
                flagGo.transform.SetParent(cityGo.transform);
                flagGo.transform.localPosition = new Vector3(0.2f, 0.3f, 0);
                flagGo.transform.localScale = Vector3.one * 0.7f;
                var flagSr = flagGo.AddComponent<SpriteRenderer>();
                flagSr.sprite = _flagSprite;
                flagSr.sortingOrder = 3;

                var cityComp = cityGo.AddComponent<City>();
                cityComp.Initialize(owner, isCapital);
                cityComp.Region = region;
                if (region != null) region.City = cityComp;

                regions.Add(region);
            }

            // Voronoi: assign EVERY tile to nearest city's region
            if (rm != null && map != null)
            {
                var bounds = map.Bounds;
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    for (int y = bounds.yMin; y < bounds.yMax; y++)
                    {
                        var tile = new Vector2Int(x, y);
                        float minDist = float.MaxValue;
                        int bestIdx = 0;

                        for (int i = 0; i < cityCenters.Count; i++)
                        {
                            float dist = (cityCenters[i] - tile).sqrMagnitude;
                            if (dist < minDist)
                            {
                                minDist = dist;
                                bestIdx = i;
                            }
                        }

                        rm.AssignTile(tile, regions[bestIdx]);
                    }
                }
            }

            return regions;
        }

        private void SetupPorts(List<Region> regions)
        {
            var map = MapManager.Instance;

            foreach (var def in PortDefs)
            {
                int gx = def[0], gy = def[1], cityIdx = def[2];
                var region = cityIdx < regions.Count ? regions[cityIdx] : null;

                var portGo = new GameObject($"Port_{gx}_{gy}");
                portGo.transform.position = map.GridToWorld(new Vector2Int(gx, gy));

                var outlineGo = new GameObject("Outline");
                outlineGo.transform.SetParent(portGo.transform);
                outlineGo.transform.localPosition = Vector3.zero;
                outlineGo.transform.localScale = Vector3.one * 1.1f;
                var outlineSr = outlineGo.AddComponent<SpriteRenderer>();
                outlineSr.sprite = _ringSprite;
                outlineSr.sortingOrder = 2;

                var portComp = portGo.AddComponent<Port>();
                portComp.Initialize();

                if (region != null)
                {
                    portComp.SetOwner(region.OwnerIndex);
                    region.Ports.Add(portComp);
                }
            }
        }

        private void SpawnStartingUnits()
        {
            var spawner = DivisionSpawner.Instance;
            if (spawner == null || MapManager.Instance == null) return;

            // Player (blue) — near capital at (15, 30)
            spawner.Spawn(DivisionType.Infantry, 0, new Vector2Int(14, 28));
            spawner.Spawn(DivisionType.Infantry, 0, new Vector2Int(16, 28));
            spawner.Spawn(DivisionType.Infantry, 0, new Vector2Int(15, 32));
            spawner.Spawn(DivisionType.Tank, 0, new Vector2Int(17, 30));

            // Bot (red) — near capital at (84, 30)
            spawner.Spawn(DivisionType.Infantry, 1, new Vector2Int(83, 28));
            spawner.Spawn(DivisionType.Infantry, 1, new Vector2Int(85, 28));
            spawner.Spawn(DivisionType.Infantry, 1, new Vector2Int(84, 32));
            spawner.Spawn(DivisionType.Tank, 1, new Vector2Int(82, 30));
        }
    }
}
