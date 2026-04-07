using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using DotWars.Core;
using DotWars.Map;
using DotWars.Units;
using DotWars.CameraSystem;
using DotWars.Economy;
using DotWars.UI;

public class SetupWizard : Editor
{
    private static readonly (TerrainType type, string hex, float speed, float infantrySpeed, float damage, float tankDamage, bool passable)[] TerrainDefs =
    {
        (TerrainType.Plain,    "#7EC850", 1.0f, -1f,   1.0f, -1f,  true),
        (TerrainType.Forest,   "#3B7A2A", 0.7f, 0.85f, 1.0f, 0.7f, true),
        (TerrainType.Mountain, "#8B8B8B", 0.0f, -1f,   0.0f, -1f,  false),
        (TerrainType.Water,    "#3B8BD6", 0.3f, -1f,   0.3f, -1f,  true),
        (TerrainType.Bridge,   "#C8A55A", 1.0f, -1f,   1.0f, -1f,  true),
        (TerrainType.Port,     "#D4A04A", 1.0f, -1f,   1.0f, -1f,  true),
        (TerrainType.City,     "#E8D44D", 1.0f, -1f,   1.0f, -1f,  true),
    };

    [MenuItem("DotWars/Setup All")]
    public static void SetupAll()
    {
        EnsureFolders();
        var sprite = CreateWhiteSquareSprite();
        var tiles = CreateTiles(sprite);
        var terrainDatas = CreateTerrainDatas();
        var infantryStats = CreateDivisionStats("InfantryStats", DivisionType.Infantry, 100f, 10f, 1.5f, 100);
        var tankStats = CreateDivisionStats("TankStats", DivisionType.Tank, 200f, 20f, 1.5f, 200);
        var gameConfig = CreateGameConfig();
        var circleSprite = CreateCircleSprite();
        var ringSprite = CreateRingSprite();
        var shipSprite = CreateShipSprite();
        var flagSprite = CreateFlagSprite();
        CreateCrackSprites();
        EnsureDivisionLayer();
        var prefab = CreateDivisionPrefab(circleSprite, ringSprite);
        CreateGameScene(tiles, circleSprite, squareSprite: sprite, flagSprite: flagSprite, ringSprite: ringSprite);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[DotWars] Setup complete!");
    }

    private static void EnsureFolders()
    {
        string[] folders =
        {
            "Assets/Tiles", "Assets/Resources/Terrain", "Assets/Resources/Units",
            "Assets/Resources/Prefabs", "Assets/Resources/Sprites", "Assets/Resources",
            "Assets/Sprites", "Assets/Scenes", "Assets/Editor"
        };

        foreach (var folder in folders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                var parts = folder.Split('/');
                var current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    var next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }
        }
    }

    private static Sprite CreateWhiteSquareSprite()
    {
        return CreateSpriteAsset("Assets/Sprites/WhiteSquare.png", 32, 32, FilterMode.Point, (tex) =>
        {
            var pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
        });
    }

    private static Sprite CreateCircleSprite()
    {
        return CreateSpriteAsset("Assets/Sprites/Circle.png", 64, 64, FilterMode.Bilinear, (tex) =>
        {
            float center = 32f;
            float radius = 31f;
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(radius - dist + 0.5f)));
                }
        });
    }

    private static Sprite CreateRingSprite()
    {
        return CreateSpriteAsset("Assets/Resources/Sprites/Ring.png", 64, 64, FilterMode.Bilinear, (tex) =>
        {
            float center = 32f;
            float outerR = 31f;
            float innerR = 28f;
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float a = Mathf.Clamp01(outerR - dist + 0.5f) * Mathf.Clamp01(dist - innerR + 0.5f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
        });
    }

    private static Sprite CreateSpriteAsset(string path, int w, int h, FilterMode filter, System.Action<Texture2D> paint)
    {
        if (!File.Exists(path))
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            paint(tex);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = w;
            importer.filterMode = filter;
            importer.SaveAndReimport();
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning($"[DotWars] Failed to load sprite at {path}");
        return sprite;
    }

    private static Tile[] CreateTiles(Sprite sprite)
    {
        var tiles = new Tile[TerrainDefs.Length];
        for (int i = 0; i < TerrainDefs.Length; i++)
        {
            var def = TerrainDefs[i];
            var path = $"Assets/Tiles/{def.type}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }

            // Always update sprite and color (fixes null sprite from failed first run)
            tile.sprite = sprite;
            ColorUtility.TryParseHtmlString(def.hex, out var color);
            tile.color = color;
            EditorUtility.SetDirty(tile);

            tiles[i] = tile;
        }
        return tiles;
    }

    private static DotWars.Map.TerrainConfig[] CreateTerrainDatas()
    {
        var datas = new DotWars.Map.TerrainConfig[TerrainDefs.Length];
        for (int i = 0; i < TerrainDefs.Length; i++)
        {
            var def = TerrainDefs[i];
            var path = $"Assets/Resources/Terrain/{def.type}.asset";
            var data = AssetDatabase.LoadAssetAtPath<DotWars.Map.TerrainConfig>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<DotWars.Map.TerrainConfig>();
                data.terrainType = def.type;
                data.isPassable = def.passable;
                data.speedModifier = def.speed;
                data.infantrySpeedModifier = def.infantrySpeed;
                data.damageModifier = def.damage;
                data.tankDamageModifier = def.tankDamage;
                AssetDatabase.CreateAsset(data, path);
            }
            datas[i] = data;
        }
        return datas;
    }

    private static DivisionStats CreateDivisionStats(string name, DivisionType type, float hp, float damage, float speed, int cost)
    {
        var path = $"Assets/Resources/Units/{name}.asset";
        var stats = AssetDatabase.LoadAssetAtPath<DivisionStats>(path);
        if (stats == null)
        {
            stats = ScriptableObject.CreateInstance<DivisionStats>();
            AssetDatabase.CreateAsset(stats, path);
        }
        stats.divisionType = type;
        stats.maxHP = hp;
        stats.damagePerSec = damage;
        stats.baseSpeed = speed;
        stats.cost = cost;
        EditorUtility.SetDirty(stats);
        return stats;
    }

    private static GameConfig CreateGameConfig()
    {
        var path = "Assets/Resources/GameConfig.asset";
        var config = AssetDatabase.LoadAssetAtPath<GameConfig>(path);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(config, path);
        }
        return config;
    }

    private static Sprite CreateShipSprite()
    {
        return CreateSpriteAsset("Assets/Resources/Sprites/Ship.png", 64, 64, FilterMode.Bilinear, (tex) =>
        {
            float cx = 32f, cy = 32f;
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float dy = (y - cy) / 28f; // vertical radius
                    float dx = (x - cx) / 16f; // horizontal radius (narrower)

                    // Pointed ends: squeeze horizontal radius near top/bottom
                    float pointFactor = 1f - Mathf.Abs(dy) * 0.6f;
                    pointFactor = Mathf.Max(pointFactor, 0.15f);
                    float adjustedDx = dx / pointFactor;

                    float dist = adjustedDx * adjustedDx + dy * dy;
                    float alpha = Mathf.Clamp01(1f - dist + 0.02f);

                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
        });
    }

    private static Sprite CreateFlagSprite()
    {
        return CreateSpriteAsset("Assets/Resources/Sprites/Flag.png", 32, 32, FilterMode.Bilinear, (tex) =>
        {
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    tex.SetPixel(x, y, Color.clear);

            // Pole (vertical line at x=6)
            for (int y = 0; y < 28; y++)
                for (int px = 5; px <= 7; px++)
                    tex.SetPixel(px, y, Color.white);

            // Flag (rectangle 7-26, 16-28)
            for (int y = 16; y < 28; y++)
                for (int x = 7; x < 26; x++)
                    tex.SetPixel(x, y, Color.white);
        });
    }

    private static void CreateCrackSprites()
    {
        // Light cracks
        CreateSpriteAsset("Assets/Resources/Sprites/CrackLight.png", 64, 64, FilterMode.Bilinear, (tex) =>
        {
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                    tex.SetPixel(x, y, Color.clear);

            // Draw a few thin crack lines
            DrawCrackLine(tex, 10, 32, 35, 20);
            DrawCrackLine(tex, 30, 45, 50, 30);
        });

        // Heavy cracks
        CreateSpriteAsset("Assets/Resources/Sprites/CrackHeavy.png", 64, 64, FilterMode.Bilinear, (tex) =>
        {
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                    tex.SetPixel(x, y, Color.clear);

            DrawCrackLine(tex, 8, 32, 35, 15);
            DrawCrackLine(tex, 28, 48, 55, 28);
            DrawCrackLine(tex, 15, 10, 40, 50);
            DrawCrackLine(tex, 38, 55, 58, 35);
            DrawCrackLine(tex, 20, 25, 48, 18);
        });
    }

    private static void DrawCrackLine(Texture2D tex, int x0, int y0, int x1, int y1)
    {
        int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        while (true)
        {
            if (x0 >= 0 && x0 < 64 && y0 >= 0 && y0 < 64)
                tex.SetPixel(x0, y0, Color.white);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }

    private static void EnsureDivisionLayer()
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");

        for (int i = 8; i < layers.arraySize; i++)
        {
            var prop = layers.GetArrayElementAtIndex(i);
            if (prop.stringValue == "Division") return;
        }

        for (int i = 8; i < layers.arraySize; i++)
        {
            var prop = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(prop.stringValue))
            {
                prop.stringValue = "Division";
                tagManager.ApplyModifiedProperties();
                return;
            }
        }
    }

    private static GameObject CreateDivisionPrefab(Sprite circleSprite, Sprite ringSprite)
    {
        var squareSprite = CreateWhiteSquareSprite();

        var path = "Assets/Resources/Prefabs/Division.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            AssetDatabase.DeleteAsset(path);

        int divisionLayer = LayerMask.NameToLayer("Division");
        if (divisionLayer == -1) divisionLayer = 0;

        var go = new GameObject("Division");
        go.layer = divisionLayer;
        go.transform.localScale = Vector3.one * 0.67f;

        // Visual container (for shake effect)
        var visualGo = new GameObject("Visual");
        visualGo.transform.SetParent(go.transform);
        visualGo.transform.localPosition = Vector3.zero;

        // Black outline — slightly larger circle behind main sprite
        var outlineGo = new GameObject("Outline");
        outlineGo.transform.SetParent(visualGo.transform);
        outlineGo.transform.localPosition = Vector3.zero;
        outlineGo.transform.localScale = Vector3.one * 1.15f;
        var outlineSr = outlineGo.AddComponent<SpriteRenderer>();
        outlineSr.sprite = circleSprite;
        outlineSr.color = new Color(0, 0, 0, 0.9f);
        outlineSr.sortingOrder = 9;

        // Main sprite — child of Visual
        var sr = visualGo.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.sortingOrder = 10;

        // Selection ring — child of Visual
        var ringGo = new GameObject("SelectionRing");
        ringGo.transform.SetParent(visualGo.transform);
        ringGo.transform.localPosition = Vector3.zero;
        ringGo.transform.localScale = Vector3.one * 1.4f;
        var ringSr = ringGo.AddComponent<SpriteRenderer>();
        ringSr.sprite = ringSprite;
        ringSr.color = new Color(1f, 1f, 1f, 0.8f);
        ringSr.sortingOrder = 8;
        ringSr.enabled = false;

        // Tank dot (black circle in center) — child of Visual
        var tankDotGo = new GameObject("TankDot");
        tankDotGo.transform.SetParent(visualGo.transform);
        tankDotGo.transform.localPosition = Vector3.zero;
        tankDotGo.transform.localScale = Vector3.one * 0.4f;
        var tankDotSr = tankDotGo.AddComponent<SpriteRenderer>();
        tankDotSr.sprite = circleSprite;
        tankDotSr.color = new Color(0.05f, 0.05f, 0.05f, 1f);
        tankDotSr.sortingOrder = 11;
        tankDotSr.enabled = false;

        // Crack overlay — child of Visual
        var crackGo = new GameObject("CrackOverlay");
        crackGo.transform.SetParent(visualGo.transform);
        crackGo.transform.localPosition = Vector3.zero;
        var crackSr = crackGo.AddComponent<SpriteRenderer>();
        crackSr.sortingOrder = 12;
        crackSr.enabled = false;

        // HP bar background — child of root (NOT Visual, so it doesn't shake)
        var hpBgGo = new GameObject("HPBarBg");
        hpBgGo.transform.SetParent(go.transform);
        hpBgGo.transform.localPosition = new Vector3(0, 0.55f, 0);
        hpBgGo.transform.localScale = new Vector3(0.6f, 0.08f, 1f);
        var hpBgSr = hpBgGo.AddComponent<SpriteRenderer>();
        hpBgSr.sprite = squareSprite;
        hpBgSr.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        hpBgSr.sortingOrder = 13;

        // HP bar fill — child of root
        var hpFillGo = new GameObject("HPBarFill");
        hpFillGo.transform.SetParent(go.transform);
        hpFillGo.transform.localPosition = new Vector3(0, 0.55f, 0);
        hpFillGo.transform.localScale = new Vector3(0.6f, 0.06f, 1f);
        var hpFillSr = hpFillGo.AddComponent<SpriteRenderer>();
        hpFillSr.sprite = squareSprite;
        hpFillSr.color = Color.green;
        hpFillSr.sortingOrder = 14;

        // Physics — Dynamic for real collision
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.linearDamping = 10f;
        rb.angularDamping = 10f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var collider = go.AddComponent<CircleCollider2D>();
        collider.radius = 0.4f;

        // Path visualization
        var lr = go.AddComponent<LineRenderer>();
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.startColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);
        lr.endColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);
        lr.positionCount = 0;
        lr.sortingOrder = 5;
        lr.useWorldSpace = true;
        lr.material = new Material(Shader.Find("Sprites/Default"));

        go.AddComponent<Division>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static void CreateGameScene(Tile[] tiles, Sprite circleSprite, Sprite squareSprite, Sprite flagSprite, Sprite ringSprite)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var light = GameObject.Find("Directional Light");
        if (light != null) Object.DestroyImmediate(light);

        var cam = UnityEngine.Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 12f;
        cam.transform.position = new Vector3(50, 30, -10);
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f);
        cam.gameObject.AddComponent<CameraController>();

        // Grid + Tilemaps
        var gridGo = new GameObject("Grid");
        gridGo.AddComponent<Grid>();

        var tilemapGo = new GameObject("Terrain");
        tilemapGo.transform.SetParent(gridGo.transform);
        var tilemap = tilemapGo.AddComponent<Tilemap>();
        tilemapGo.AddComponent<TilemapRenderer>().sortingOrder = 0;

        // Overlay tilemap (region colors)
        var overlayGo = new GameObject("Overlay");
        overlayGo.transform.SetParent(gridGo.transform);
        var overlayTilemap = overlayGo.AddComponent<Tilemap>();
        overlayGo.AddComponent<TilemapRenderer>().sortingOrder = 1;

        // Border tilemap (region borders)
        var borderGo = new GameObject("Border");
        borderGo.transform.SetParent(gridGo.transform);
        var borderTilemap = borderGo.AddComponent<Tilemap>();
        borderGo.AddComponent<TilemapRenderer>().sortingOrder = 2;

        PaintLargeMap(tilemap, tiles);
        tilemap.RefreshAllTiles();

        // Create overlay/border tile assets
        CreateOverlayTiles(squareSprite);

        // Managers
        new GameObject("GameManager").AddComponent<GameManager>();

        var mmGo = new GameObject("MapManager");
        var mm = mmGo.AddComponent<MapManager>();
        var mmSo = new SerializedObject(mm);
        mmSo.FindProperty("terrainTilemap").objectReferenceValue = tilemap;
        mmSo.ApplyModifiedProperties();

        new GameObject("DivisionSpawner").AddComponent<DivisionSpawner>();
        new GameObject("EconomyManager").AddComponent<EconomyManager>();

        // RegionManager
        var rmGo = new GameObject("RegionManager");
        var rm = rmGo.AddComponent<RegionManager>();
        var rmSo = new SerializedObject(rm);
        rmSo.FindProperty("borderTilemap").objectReferenceValue = borderTilemap;
        rmSo.FindProperty("overlayTilemap").objectReferenceValue = overlayTilemap;
        rmSo.ApplyModifiedProperties();

        var smGo = new GameObject("SelectionManager");
        var sm = smGo.AddComponent<SelectionManager>();
        var smSo = new SerializedObject(sm);
        int divLayer = LayerMask.NameToLayer("Division");
        if (divLayer >= 0)
            smSo.FindProperty("divisionLayer").intValue = 1 << divLayer;
        smSo.ApplyModifiedProperties();

        new GameObject("EconomyHUD").AddComponent<EconomyHUD>();

        // Cities, ports, regions — see GameSetup for runtime creation
        // (Region assignment needs MapManager to be initialized first)

        new GameObject("GameSetup").AddComponent<GameSetup>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Game.unity");
    }

    private static void CreateOverlayTiles(Sprite squareSprite)
    {
        // Fog tile
        var fogPath = "Assets/Resources/Tiles/Fog.asset";
        if (AssetDatabase.LoadAssetAtPath<Tile>(fogPath) == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Tiles"))
                AssetDatabase.CreateFolder("Assets/Resources", "Tiles");
            var t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = squareSprite;
            t.color = new Color(0, 0, 0, 0.7f);
            AssetDatabase.CreateAsset(t, fogPath);
        }

        var overlayPath = "Assets/Resources/Tiles/Overlay.asset";
        if (AssetDatabase.LoadAssetAtPath<Tile>(overlayPath) == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Tiles"))
                AssetDatabase.CreateFolder("Assets/Resources", "Tiles");
            var t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = squareSprite;
            t.color = Color.white;
            AssetDatabase.CreateAsset(t, overlayPath);
        }

        var borderPath = "Assets/Resources/Tiles/Border.asset";
        if (AssetDatabase.LoadAssetAtPath<Tile>(borderPath) == null)
        {
            var t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = squareSprite;
            t.color = Color.white;
            AssetDatabase.CreateAsset(t, borderPath);
        }
    }

    private static void CreateCityObject(string name, Vector3 pos, int owner, bool isCapital, Sprite circleSprite, Sprite flagSprite, Sprite ringSprite)
    {
        var go = new GameObject(name);
        go.transform.position = pos;

        // Outline ring
        var outlineGo = new GameObject("Outline");
        outlineGo.transform.SetParent(go.transform);
        outlineGo.transform.localPosition = Vector3.zero;
        outlineGo.transform.localScale = Vector3.one * 1.1f;
        var outlineSr = outlineGo.AddComponent<SpriteRenderer>();
        outlineSr.sprite = ringSprite;
        outlineSr.sortingOrder = 2;

        // Flag
        var flagGo = new GameObject("Flag");
        flagGo.transform.SetParent(go.transform);
        flagGo.transform.localPosition = new Vector3(0.2f, 0.3f, 0);
        flagGo.transform.localScale = Vector3.one * 0.7f;
        var flagSr = flagGo.AddComponent<SpriteRenderer>();
        flagSr.sprite = flagSprite;
        flagSr.sortingOrder = 3;

        var city = go.AddComponent<City>();
        city.Initialize(owner, isCapital);
    }

    private static void CreatePortObject(string name, Vector3 pos, Sprite circleSprite, Sprite ringSprite)
    {
        var go = new GameObject(name);
        go.transform.position = pos;

        // Outline ring
        var outlineGo = new GameObject("Outline");
        outlineGo.transform.SetParent(go.transform);
        outlineGo.transform.localPosition = Vector3.zero;
        outlineGo.transform.localScale = Vector3.one * 1.1f;
        var outlineSr = outlineGo.AddComponent<SpriteRenderer>();
        outlineSr.sprite = ringSprite;
        outlineSr.color = new Color(0.8f, 0.6f, 0.2f, 0.5f);
        outlineSr.sortingOrder = 2;

        var port = go.AddComponent<Port>();
        port.Initialize();
    }

    // Found port positions (set during map painting, read by GameSetup)
    public static Vector2Int[] FoundPortPositions { get; private set; }

    private static void PaintLargeMap(Tilemap tilemap, Tile[] tiles)
    {
        var plain = tiles[0];
        var forest = tiles[1];
        var mountain = tiles[2];
        var water = tiles[3];
        var bridge = tiles[4];
        var port = tiles[5];
        var city = tiles[6];

        int w = 100, h = 60;
        float seed = 42.7f;

        // Water everywhere
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tilemap.SetTile(new Vector3Int(x, y, 0), water);

        // Landmass with organic coastline (Perlin noise)
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                float edgeDist = Mathf.Min(x, y, w - 1 - x, h - 1 - y);
                float noise = Mathf.PerlinNoise(x * 0.08f + seed, y * 0.08f + seed);
                float threshold = 3f + noise * 2.5f;
                if (edgeDist > threshold)
                    tilemap.SetTile(new Vector3Int(x, y, 0), plain);
            }
        }

        // === Rivers (with slight winding) ===
        // Vertical river ~x=49-50
        for (int y = 0; y < h; y++)
        {
            int rx = 49 + Mathf.RoundToInt(Mathf.PerlinNoise(y * 0.15f, seed + 10) * 2f - 1f);
            tilemap.SetTile(new Vector3Int(rx, y, 0), water);
            tilemap.SetTile(new Vector3Int(rx + 1, y, 0), water);
        }

        // Horizontal river ~y=20, from x=15 to x=85
        for (int x = 15; x <= 85; x++)
        {
            int ry = 20 + Mathf.RoundToInt(Mathf.PerlinNoise(x * 0.12f, seed + 20) * 2f - 1f);
            tilemap.SetTile(new Vector3Int(x, ry, 0), water);
            tilemap.SetTile(new Vector3Int(x, ry + 1, 0), water);
        }

        // === Bridges === (placed where rivers are straight, connecting land)
        // Over vertical river
        int[][] vBridges = { new[]{49, 12}, new[]{49, 30}, new[]{49, 45} };
        foreach (var b in vBridges)
        {
            // Find actual river x at this y
            int rx = 49 + Mathf.RoundToInt(Mathf.PerlinNoise(b[1] * 0.15f, seed + 10) * 2f - 1f);
            tilemap.SetTile(new Vector3Int(rx, b[1], 0), bridge);
            tilemap.SetTile(new Vector3Int(rx + 1, b[1], 0), bridge);
        }

        // Over horizontal river (avoid vertical river intersection)
        int[] hBridgeX = { 28, 72 };
        foreach (int bx in hBridgeX)
        {
            int ry = 20 + Mathf.RoundToInt(Mathf.PerlinNoise(bx * 0.12f, seed + 20) * 2f - 1f);
            tilemap.SetTile(new Vector3Int(bx, ry, 0), bridge);
            tilemap.SetTile(new Vector3Int(bx, ry + 1, 0), bridge);
        }

        // === Mountains (organic, noise-based) ===
        PaintNoisyMountains(tilemap, mountain, 18, 38, 32, 46, seed + 100);
        PaintNoisyMountains(tilemap, mountain, 68, 38, 82, 46, seed + 101);
        PaintNoisyMountains(tilemap, mountain, 33, 46, 67, 54, seed + 102);
        PaintNoisyMountains(tilemap, mountain, 13, 6, 27, 14, seed + 103);
        PaintNoisyMountains(tilemap, mountain, 73, 6, 87, 14, seed + 104);

        // === Forests (noise-based, irregular edges) ===
        PaintNoisyForest(tilemap, forest, 8, 22, 9, seed + 200);
        PaintNoisyForest(tilemap, forest, 28, 33, 11, seed + 201);
        PaintNoisyForest(tilemap, forest, 62, 33, 11, seed + 202);
        PaintNoisyForest(tilemap, forest, 87, 22, 9, seed + 203);
        PaintNoisyForest(tilemap, forest, 23, 8, 7, seed + 204);
        PaintNoisyForest(tilemap, forest, 72, 8, 7, seed + 205);
        PaintNoisyForest(tilemap, forest, 13, 43, 8, seed + 206);
        PaintNoisyForest(tilemap, forest, 82, 43, 8, seed + 207);
        PaintNoisyForest(tilemap, forest, 43, 6, 6, seed + 208);
        PaintNoisyForest(tilemap, forest, 57, 6, 6, seed + 209);
        // Extra scattered forest patches
        PaintNoisyForest(tilemap, forest, 38, 15, 5, seed + 210);
        PaintNoisyForest(tilemap, forest, 62, 15, 5, seed + 211);
        PaintNoisyForest(tilemap, forest, 20, 52, 4, seed + 212);
        PaintNoisyForest(tilemap, forest, 78, 52, 4, seed + 213);

        // === Cities ===
        tilemap.SetTile(new Vector3Int(15, 30, 0), city);
        tilemap.SetTile(new Vector3Int(10, 15, 0), city);
        tilemap.SetTile(new Vector3Int(10, 45, 0), city);
        tilemap.SetTile(new Vector3Int(35, 30, 0), city);
        tilemap.SetTile(new Vector3Int(84, 30, 0), city);
        tilemap.SetTile(new Vector3Int(89, 15, 0), city);
        tilemap.SetTile(new Vector3Int(89, 45, 0), city);
        tilemap.SetTile(new Vector3Int(64, 30, 0), city);

        // === Ports ===
        // Find coast positions for ports dynamically
        FoundPortPositions = new Vector2Int[4];
        FoundPortPositions[0] = FindCoastTile(tilemap, 30, true, w);   // left coast y=30
        FoundPortPositions[1] = FindCoastTile(tilemap, 15, true, w);   // left coast y=15
        FoundPortPositions[2] = FindCoastTile(tilemap, 30, false, w);  // right coast y=30
        FoundPortPositions[3] = FindCoastTile(tilemap, 45, false, w);  // right coast y=45

        foreach (var pp in FoundPortPositions)
            tilemap.SetTile(new Vector3Int(pp.x, pp.y, 0), port);
    }

    private static Vector2Int FindCoastTile(Tilemap tilemap, int y, bool fromLeft, int mapWidth)
    {
        if (fromLeft)
        {
            // Scan left→right: find first land tile that has water neighbor
            for (int x = 0; x < mapWidth; x++)
            {
                var tile = tilemap.GetTile(new Vector3Int(x, y, 0));
                if (tile != null && tile.name != "Water" && tile.name != "Mountain")
                {
                    // Check if left neighbor is water
                    if (x > 0)
                    {
                        var left = tilemap.GetTile(new Vector3Int(x - 1, y, 0));
                        if (left != null && left.name == "Water")
                            return new Vector2Int(x, y);
                    }
                }
            }
        }
        else
        {
            // Scan right→left
            for (int x = mapWidth - 1; x >= 0; x--)
            {
                var tile = tilemap.GetTile(new Vector3Int(x, y, 0));
                if (tile != null && tile.name != "Water" && tile.name != "Mountain")
                {
                    if (x < mapWidth - 1)
                    {
                        var right = tilemap.GetTile(new Vector3Int(x + 1, y, 0));
                        if (right != null && right.name == "Water")
                            return new Vector2Int(x, y);
                    }
                }
            }
        }
        // Fallback
        return fromLeft ? new Vector2Int(5, y) : new Vector2Int(mapWidth - 6, y);
    }

    private static void PaintNoisyMountains(Tilemap tilemap, Tile mt, int x1, int y1, int x2, int y2, float seed)
    {
        float cx = (x1 + x2) * 0.5f, cy = (y1 + y2) * 0.5f;
        float rx = (x2 - x1) * 0.5f, ry = (y2 - y1) * 0.5f;

        for (int x = x1 - 2; x <= x2 + 2; x++)
        {
            for (int y = y1 - 2; y <= y2 + 2; y++)
            {
                float dx = (x - cx) / rx;
                float dy = (y - cy) / ry;
                float dist = dx * dx + dy * dy;
                float noise = Mathf.PerlinNoise(x * 0.2f + seed, y * 0.2f + seed);
                if (dist < 0.7f + noise * 0.5f)
                {
                    var pos = new Vector3Int(x, y, 0);
                    var existing = tilemap.GetTile(pos);
                    if (existing != null && existing.name == "Plain")
                        tilemap.SetTile(pos, mt);
                }
            }
        }
    }

    private static void PaintNoisyForest(Tilemap tilemap, Tile forest, int cx, int cy, int radius, float seed)
    {
        for (int dx = -radius - 2; dx <= radius + 2; dx++)
        {
            for (int dy = -radius - 2; dy <= radius + 2; dy++)
            {
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float noise = Mathf.PerlinNoise((cx + dx) * 0.25f + seed, (cy + dy) * 0.25f + seed);
                float threshold = radius * (0.6f + noise * 0.6f);
                if (dist > threshold) continue;

                var pos = new Vector3Int(cx + dx, cy + dy, 0);
                var existing = tilemap.GetTile(pos);
                if (existing != null && existing.name == "Plain")
                    tilemap.SetTile(pos, forest);
            }
        }
    }
}
