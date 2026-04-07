using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DotWars.Map
{
    public class MapManager : MonoBehaviour
    {
        public static MapManager Instance { get; private set; }

        [SerializeField] private Tilemap terrainTilemap;

        private TerrainConfig[,] _terrainGrid;
        private int _width;
        private int _height;
        private BoundsInt _bounds;
        private Dictionary<string, TerrainConfig> _configByName;

        public int Width => _width;
        public int Height => _height;
        public BoundsInt Bounds => _bounds;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadTerrainConfigs();
            BuildTerrainGrid();
        }

        private void LoadTerrainConfigs()
        {
            _configByName = new Dictionary<string, TerrainConfig>();
            var configs = Resources.LoadAll<TerrainConfig>("Terrain");
            foreach (var config in configs)
            {
                _configByName[config.terrainType.ToString()] = config;
            }
            Debug.Log($"[MapManager] Loaded {configs.Length} terrain configs from Resources");
        }

        private void BuildTerrainGrid()
        {
            terrainTilemap.CompressBounds();
            _bounds = terrainTilemap.cellBounds;
            _width = _bounds.size.x;
            _height = _bounds.size.y;
            _terrainGrid = new TerrainConfig[_width, _height];

            int mapped = 0, unmapped = 0, nullTiles = 0;

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var cellPos = new Vector3Int(_bounds.xMin + x, _bounds.yMin + y, 0);
                    var tile = terrainTilemap.GetTile(cellPos);

                    if (tile == null) { nullTiles++; continue; }

                    if (_configByName.TryGetValue(tile.name, out var config))
                    {
                        _terrainGrid[x, y] = config;
                        mapped++;
                    }
                    else
                    {
                        unmapped++;
                    }
                }
            }

            Debug.Log($"[MapManager] Grid: {_width}x{_height}, {mapped} mapped, {unmapped} unmapped, {nullTiles} empty");
        }

        public TerrainConfig GetTerrainAt(Vector2Int gridPos)
        {
            int x = gridPos.x - _bounds.xMin;
            int y = gridPos.y - _bounds.yMin;

            if (x < 0 || x >= _width || y < 0 || y >= _height)
                return null;

            return _terrainGrid[x, y];
        }

        public TerrainConfig GetTerrainAtWorld(Vector3 worldPos)
        {
            var cellPos = terrainTilemap.WorldToCell(worldPos);
            return GetTerrainAt(new Vector2Int(cellPos.x, cellPos.y));
        }

        public bool IsPassable(Vector2Int gridPos)
        {
            var terrain = GetTerrainAt(gridPos);
            return terrain != null && terrain.isPassable;
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            var cellPos = new Vector3Int(gridPos.x, gridPos.y, 0);
            return terrainTilemap.GetCellCenterWorld(cellPos);
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            var cellPos = terrainTilemap.WorldToCell(worldPos);
            return new Vector2Int(cellPos.x, cellPos.y);
        }

        public bool InBounds(Vector2Int gridPos)
        {
            int x = gridPos.x - _bounds.xMin;
            int y = gridPos.y - _bounds.yMin;
            return x >= 0 && x < _width && y >= 0 && y < _height;
        }
    }
}
