using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DotWars.Map
{
    public class RegionManager : MonoBehaviour
    {
        public static RegionManager Instance { get; private set; }

        [SerializeField] private Tilemap borderTilemap;
        [SerializeField] private Tilemap overlayTilemap;

        private readonly List<Region> _regions = new();
        private readonly Dictionary<Vector2Int, Region> _tileToRegion = new();

        private Tile _borderTile;
        private Tile _overlayTile;

        private static readonly Color PlayerOverlay = new(0.2f, 0.5f, 1f, 0.08f);
        private static readonly Color BotOverlay = new(1f, 0.25f, 0.25f, 0.08f);
        private static readonly Color NeutralOverlay = new(0.5f, 0.5f, 0.5f, 0.05f);

        private static readonly Color PlayerBorder = new(0.2f, 0.5f, 1f, 0.3f);
        private static readonly Color BotBorder = new(1f, 0.25f, 0.25f, 0.3f);
        private static readonly Color NeutralBorder = new(0.5f, 0.5f, 0.5f, 0.2f);

        public IReadOnlyList<Region> Regions => _regions;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _borderTile = Resources.Load<Tile>("Tiles/Border");
            _overlayTile = Resources.Load<Tile>("Tiles/Overlay");
        }

        public Region CreateRegion(int ownerIndex)
        {
            var region = new Region(_regions.Count, ownerIndex);
            _regions.Add(region);
            return region;
        }

        public void AssignTile(Vector2Int tile, Region region)
        {
            _tileToRegion[tile] = region;
            region.Tiles.Add(tile);
        }

        public Region GetRegionAt(Vector2Int tile)
        {
            return _tileToRegion.TryGetValue(tile, out var r) ? r : null;
        }

        public void CaptureRegion(Region region, int newOwner)
        {
            region.SetOwner(newOwner);
            RefreshRegionVisuals(region);
        }

        public void RefreshAllVisuals()
        {
            if (borderTilemap != null) borderTilemap.ClearAllTiles();
            if (overlayTilemap != null) overlayTilemap.ClearAllTiles();

            foreach (var region in _regions)
                RefreshRegionVisuals(region);
        }

        private void RefreshRegionVisuals(Region region)
        {
            Color overlay = region.OwnerIndex switch
            {
                0 => PlayerOverlay,
                1 => BotOverlay,
                _ => NeutralOverlay
            };

            Color border = region.OwnerIndex switch
            {
                0 => PlayerBorder,
                1 => BotBorder,
                _ => NeutralBorder
            };

            foreach (var tile in region.Tiles)
            {
                var pos = new Vector3Int(tile.x, tile.y, 0);

                // Overlay
                if (overlayTilemap != null && _overlayTile != null)
                {
                    overlayTilemap.SetTile(pos, _overlayTile);
                    overlayTilemap.SetTileFlags(pos, TileFlags.None);
                    overlayTilemap.SetColor(pos, overlay);
                }

                // Border — only on edges of region
                if (borderTilemap != null && _borderTile != null && IsBorderTile(tile, region))
                {
                    borderTilemap.SetTile(pos, _borderTile);
                    borderTilemap.SetTileFlags(pos, TileFlags.None);
                    borderTilemap.SetColor(pos, border);
                }
            }
        }

        public bool IsBorderTile(Vector2Int tile, Region region)
        {
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var d in dirs)
            {
                var neighbor = tile + d;
                if (!region.Tiles.Contains(neighbor))
                    return true;
            }
            return false;
        }
    }
}
