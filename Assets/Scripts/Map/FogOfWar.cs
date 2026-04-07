using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using DotWars.Units;

namespace DotWars.Map
{
    public class FogOfWar : MonoBehaviour
    {
        public static FogOfWar Instance { get; private set; }

        [SerializeField] private Tilemap fogTilemap;

        private bool[,] _playerVisible;
        private bool[,] _botVisible;
        private bool[,] _prevPlayerVisible;
        private int _width, _height;
        private BoundsInt _bounds;
        private Tile _fogTile;
        private float _updateTimer;

        private const float UpdateInterval = 0.3f;
        private const int UnitVisionRadius = 8;
        private const int RegionBorderVision = 2;
        private const int CityVisionRadius = 5;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            var map = MapManager.Instance;
            if (map == null) return;

            _bounds = map.Bounds;
            _width = _bounds.size.x;
            _height = _bounds.size.y;
            _playerVisible = new bool[_width, _height];
            _botVisible = new bool[_width, _height];
            _prevPlayerVisible = new bool[_width, _height];

            _fogTile = Resources.Load<Tile>("Tiles/Fog");

            // Initial fog — cover everything
            if (fogTilemap != null && _fogTile != null)
            {
                for (int x = 0; x < _width; x++)
                {
                    for (int y = 0; y < _height; y++)
                    {
                        var pos = new Vector3Int(_bounds.xMin + x, _bounds.yMin + y, 0);
                        fogTilemap.SetTile(pos, _fogTile);
                        fogTilemap.SetTileFlags(pos, TileFlags.None);
                        fogTilemap.SetColor(pos, new Color(0, 0, 0, 0.7f));
                    }
                }
            }

            UpdateVisibility();
            ApplyFog();
        }

        private void Update()
        {
            _updateTimer += Time.deltaTime;
            if (_updateTimer >= UpdateInterval)
            {
                _updateTimer = 0;
                UpdateVisibility();
                ApplyFog();
                UpdateUnitVisibility();
            }
        }

        private void UpdateVisibility()
        {
            // Clear
            System.Array.Clear(_playerVisible, 0, _playerVisible.Length);
            System.Array.Clear(_botVisible, 0, _botVisible.Length);

            // Units vision
            var divisions = FindObjectsByType<Division>(FindObjectsSortMode.None);
            foreach (var div in divisions)
            {
                var grid = MapManager.Instance.WorldToGrid(div.transform.position);
                var visMap = div.OwnerIndex == 0 ? _playerVisible : _botVisible;
                FillCircle(visMap, grid, UnitVisionRadius);
            }

            // Region border vision (own regions — 2 tiles from border)
            var rm = RegionManager.Instance;
            if (rm != null)
            {
                foreach (var region in rm.Regions)
                {
                    if (region.OwnerIndex < 0) continue;
                    var visMap = region.OwnerIndex == 0 ? _playerVisible : _botVisible;

                    foreach (var tile in region.Tiles)
                    {
                        if (rm.IsBorderTile(tile, region))
                            FillCircle(visMap, tile, RegionBorderVision);
                    }
                }
            }

            // Cities/ports vision
            var cities = FindObjectsByType<City>(FindObjectsSortMode.None);
            foreach (var c in cities)
            {
                if (c.OwnerIndex < 0) continue;
                var grid = MapManager.Instance.WorldToGrid(c.transform.position);
                var visMap = c.OwnerIndex == 0 ? _playerVisible : _botVisible;
                FillCircle(visMap, grid, CityVisionRadius);
            }
        }

        private void FillCircle(bool[,] visMap, Vector2Int center, int radius)
        {
            int cx = center.x - _bounds.xMin;
            int cy = center.y - _bounds.yMin;
            int r2 = radius * radius;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (dx * dx + dy * dy > r2) continue;
                    int gx = cx + dx, gy = cy + dy;
                    if (gx >= 0 && gx < _width && gy >= 0 && gy < _height)
                        visMap[gx, gy] = true;
                }
            }
        }

        private void ApplyFog()
        {
            if (fogTilemap == null || _fogTile == null) return;

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (_playerVisible[x, y] == _prevPlayerVisible[x, y]) continue;

                    var pos = new Vector3Int(_bounds.xMin + x, _bounds.yMin + y, 0);
                    if (_playerVisible[x, y])
                    {
                        fogTilemap.SetTile(pos, null); // Remove fog
                    }
                    else
                    {
                        fogTilemap.SetTile(pos, _fogTile);
                        fogTilemap.SetTileFlags(pos, TileFlags.None);
                        fogTilemap.SetColor(pos, new Color(0, 0, 0, 0.7f));
                    }

                    _prevPlayerVisible[x, y] = _playerVisible[x, y];
                }
            }
        }

        private void UpdateUnitVisibility()
        {
            var divisions = FindObjectsByType<Division>(FindObjectsSortMode.None);
            foreach (var div in divisions)
            {
                if (div.OwnerIndex == 0) continue; // Always show player units

                var grid = MapManager.Instance.WorldToGrid(div.transform.position);
                int gx = grid.x - _bounds.xMin;
                int gy = grid.y - _bounds.yMin;

                bool visible = gx >= 0 && gx < _width && gy >= 0 && gy < _height && _playerVisible[gx, gy];
                div.SetFogVisible(visible);
            }
        }

        public bool IsVisibleToPlayer(Vector2Int gridPos)
        {
            int gx = gridPos.x - _bounds.xMin;
            int gy = gridPos.y - _bounds.yMin;
            if (gx < 0 || gx >= _width || gy < 0 || gy >= _height) return false;
            return _playerVisible[gx, gy];
        }

        public bool IsVisibleToBot(Vector2Int gridPos)
        {
            int gx = gridPos.x - _bounds.xMin;
            int gy = gridPos.y - _bounds.yMin;
            if (gx < 0 || gx >= _width || gy < 0 || gy >= _height) return false;
            return _botVisible[gx, gy];
        }
    }
}
