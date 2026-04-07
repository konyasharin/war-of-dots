using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using DotWars.Core;
using DotWars.Map;
using DotWars.Units;
using DotWars.CameraSystem;

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
        var infantryStats = CreateDivisionStats("InfantryStats", DivisionType.Infantry, 100f, 10f, 6f, 100);
        var tankStats = CreateDivisionStats("TankStats", DivisionType.Tank, 200f, 20f, 6f, 200);
        var gameConfig = CreateGameConfig();
        var circleSprite = CreateCircleSprite();
        var ringSprite = CreateRingSprite();
        CreateCrackSprites();
        EnsureDivisionLayer();
        var prefab = CreateDivisionPrefab(circleSprite, ringSprite);
        CreateGameScene(tiles);

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
        return CreateSpriteAsset("Assets/Sprites/Ring.png", 64, 64, FilterMode.Bilinear, (tex) =>
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
        var path = "Assets/Resources/Prefabs/Division.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            AssetDatabase.DeleteAsset(path);

        int divisionLayer = LayerMask.NameToLayer("Division");
        if (divisionLayer == -1) divisionLayer = 0;

        var go = new GameObject("Division");
        go.layer = divisionLayer;

        // Main sprite
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.sortingOrder = 10;

        // Selection ring
        var ringGo = new GameObject("SelectionRing");
        ringGo.transform.SetParent(go.transform);
        ringGo.transform.localPosition = Vector3.zero;
        ringGo.transform.localScale = Vector3.one * 1.4f;
        var ringSr = ringGo.AddComponent<SpriteRenderer>();
        ringSr.sprite = ringSprite;
        ringSr.color = new Color(1f, 1f, 1f, 0.8f);
        ringSr.sortingOrder = 9;
        ringSr.enabled = false;

        // Tank border (thicker ring, visible only for tanks — Division.Initialize enables it)
        var borderGo = new GameObject("TankBorder");
        borderGo.transform.SetParent(go.transform);
        borderGo.transform.localPosition = Vector3.zero;
        borderGo.transform.localScale = Vector3.one * 1.15f;
        var borderSr = borderGo.AddComponent<SpriteRenderer>();
        borderSr.sprite = ringSprite;
        borderSr.color = new Color(1f, 1f, 1f, 0.6f);
        borderSr.sortingOrder = 11;
        borderSr.enabled = false;

        // Crack overlay
        var crackGo = new GameObject("CrackOverlay");
        crackGo.transform.SetParent(go.transform);
        crackGo.transform.localPosition = Vector3.zero;
        var crackSr = crackGo.AddComponent<SpriteRenderer>();
        crackSr.sortingOrder = 12;
        crackSr.enabled = false;

        // HP bar background
        var hpBgGo = new GameObject("HPBarBg");
        hpBgGo.transform.SetParent(go.transform);
        hpBgGo.transform.localPosition = new Vector3(0, 0.55f, 0);
        hpBgGo.transform.localScale = new Vector3(0.6f, 0.08f, 1f);
        var hpBgSr = hpBgGo.AddComponent<SpriteRenderer>();
        hpBgSr.sprite = circleSprite; // reuse circle as a rounded rect-ish bar
        hpBgSr.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        hpBgSr.sortingOrder = 13;

        // HP bar fill
        var hpFillGo = new GameObject("HPBarFill");
        hpFillGo.transform.SetParent(go.transform);
        hpFillGo.transform.localPosition = new Vector3(0, 0.55f, 0);
        hpFillGo.transform.localScale = new Vector3(0.6f, 0.06f, 1f);
        var hpFillSr = hpFillGo.AddComponent<SpriteRenderer>();
        hpFillSr.sprite = circleSprite;
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

    private static void CreateGameScene(Tile[] tiles)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Remove default directional light if exists
        var light = GameObject.Find("Directional Light");
        if (light != null) Object.DestroyImmediate(light);

        // Camera setup
        var cam = UnityEngine.Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 7f;
        cam.transform.position = new Vector3(15, 10, -10);
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        cam.gameObject.AddComponent<CameraController>();

        // Grid + Tilemap
        var gridGo = new GameObject("Grid");
        var grid = gridGo.AddComponent<Grid>();

        var tilemapGo = new GameObject("Terrain");
        tilemapGo.transform.SetParent(gridGo.transform);
        var tilemap = tilemapGo.AddComponent<Tilemap>();
        var renderer = tilemapGo.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = 0;

        // Paint the map
        PaintTestMap(tilemap, tiles);
        tilemap.RefreshAllTiles();

        // GameManager (loads GameConfig from Resources)
        new GameObject("GameManager").AddComponent<GameManager>();

        // MapManager (loads TerrainConfigs from Resources, needs tilemap ref)
        var mmGo = new GameObject("MapManager");
        var mm = mmGo.AddComponent<MapManager>();
        var mmSo = new SerializedObject(mm);
        mmSo.FindProperty("terrainTilemap").objectReferenceValue = tilemap;
        mmSo.ApplyModifiedProperties();

        // DivisionSpawner (loads prefab + stats from Resources)
        new GameObject("DivisionSpawner").AddComponent<DivisionSpawner>();

        // SelectionManager
        var smGo = new GameObject("SelectionManager");
        var sm = smGo.AddComponent<SelectionManager>();
        var smSo = new SerializedObject(sm);
        int divLayer = LayerMask.NameToLayer("Division");
        if (divLayer >= 0)
            smSo.FindProperty("divisionLayer").intValue = 1 << divLayer;
        smSo.ApplyModifiedProperties();

        // GameSetup
        var setupGo = new GameObject("GameSetup");
        setupGo.AddComponent<GameSetup>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Game.unity");
        Debug.Log("[DotWars] Game scene created at Assets/Scenes/Game.unity");
    }

    private static void PaintTestMap(Tilemap tilemap, Tile[] tiles)
    {
        // tiles order: Plain=0, Forest=1, Mountain=2, Water=3, Bridge=4, Port=5, City=6
        var plain = tiles[0];
        var forest = tiles[1];
        var mountain = tiles[2];
        var water = tiles[3];
        var bridge = tiles[4];
        var port = tiles[5];
        var city = tiles[6];

        int w = 30, h = 20;

        // Fill everything with water first
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tilemap.SetTile(new Vector3Int(x, y, 0), water);

        // Land mass (plain) — inset 2 tiles from edges
        for (int x = 2; x < w - 2; x++)
        {
            for (int y = 2; y < h - 2; y++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), plain);
            }
        }

        // Smooth coastline — round corners
        int[,] waterCorners = {
            {2,2}, {2,3}, {3,2},
            {2,h-3}, {2,h-4}, {3,h-3},
            {w-3,2}, {w-3,3}, {w-4,2},
            {w-3,h-3}, {w-3,h-4}, {w-4,h-3}
        };
        for (int i = 0; i < waterCorners.GetLength(0); i++)
            tilemap.SetTile(new Vector3Int(waterCorners[i, 0], waterCorners[i, 1], 0), water);

        // River through the middle (vertical, x=14..15)
        for (int y = 2; y < h - 2; y++)
        {
            tilemap.SetTile(new Vector3Int(14, y, 0), water);
            tilemap.SetTile(new Vector3Int(15, y, 0), water);
        }

        // Bridges over the river
        tilemap.SetTile(new Vector3Int(14, 7, 0), bridge);
        tilemap.SetTile(new Vector3Int(15, 7, 0), bridge);
        tilemap.SetTile(new Vector3Int(14, 13, 0), bridge);
        tilemap.SetTile(new Vector3Int(15, 13, 0), bridge);

        // Mountain range (top area)
        int[] mtX = { 8, 9, 10, 20, 21, 22 };
        foreach (int mx in mtX)
        {
            tilemap.SetTile(new Vector3Int(mx, 15, 0), mountain);
            tilemap.SetTile(new Vector3Int(mx, 16, 0), mountain);
        }

        // Forests — left side clusters
        int[,] forestL = {
            {4,5}, {4,6}, {5,5}, {5,6}, {5,7}, {6,6},
            {4,12}, {4,13}, {5,12}, {5,13}, {6,12}, {6,13},
            {7,4}, {8,4}, {8,5},
        };
        for (int i = 0; i < forestL.GetLength(0); i++)
            tilemap.SetTile(new Vector3Int(forestL[i, 0], forestL[i, 1], 0), forest);

        // Forests — right side clusters
        int[,] forestR = {
            {23,5}, {23,6}, {24,5}, {24,6}, {24,7}, {25,6},
            {23,12}, {23,13}, {24,12}, {24,13}, {25,12}, {25,13},
            {21,4}, {22,4}, {22,5},
        };
        for (int i = 0; i < forestR.GetLength(0); i++)
            tilemap.SetTile(new Vector3Int(forestR[i, 0], forestR[i, 1], 0), forest);

        // Ports
        tilemap.SetTile(new Vector3Int(2, 10, 0), port);
        tilemap.SetTile(new Vector3Int(w - 3, 10, 0), port);

        // Cities (capitals)
        tilemap.SetTile(new Vector3Int(5, 10, 0), city);   // Player capital
        tilemap.SetTile(new Vector3Int(24, 10, 0), city);  // Bot capital
    }
}
