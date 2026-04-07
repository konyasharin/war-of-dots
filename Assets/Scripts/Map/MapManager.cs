using UnityEngine;
using UnityEngine.Tilemaps;

namespace DotWars.Map
{
    public class MapManager : MonoBehaviour
    {
        public static MapManager Instance { get; private set; }

        [SerializeField] private Tilemap terrainTilemap;
        [SerializeField] private TerrainTileMapping[] tileMappings;

        private TerrainData[,] _terrainGrid;
        private int _width;
        private int _height;
        private BoundsInt _bounds;

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

            BuildTerrainGrid();
        }

        private void BuildTerrainGrid()
        {
            terrainTilemap.CompressBounds();
            _bounds = terrainTilemap.cellBounds;
            _width = _bounds.size.x;
            _height = _bounds.size.y;
            _terrainGrid = new TerrainData[_width, _height];

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var cellPos = new Vector3Int(_bounds.xMin + x, _bounds.yMin + y, 0);
                    var tile = terrainTilemap.GetTile(cellPos);

                    _terrainGrid[x, y] = GetTerrainDataForTile(tile);
                }
            }
        }

        private TerrainData GetTerrainDataForTile(TileBase tile)
        {
            if (tile == null) return null;

            foreach (var mapping in tileMappings)
            {
                if (mapping.tile == tile)
                    return mapping.terrainData;
            }

            return null;
        }

        public TerrainData GetTerrainAt(Vector2Int gridPos)
        {
            int x = gridPos.x - _bounds.xMin;
            int y = gridPos.y - _bounds.yMin;

            if (x < 0 || x >= _width || y < 0 || y >= _height)
                return null;

            return _terrainGrid[x, y];
        }

        public TerrainData GetTerrainAtWorld(Vector3 worldPos)
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

    [System.Serializable]
    public struct TerrainTileMapping
    {
        public TileBase tile;
        public TerrainData terrainData;
    }
}
