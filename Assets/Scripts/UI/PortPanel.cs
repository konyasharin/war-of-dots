using System.Collections.Generic;
using UnityEngine;
using DotWars.Core;
using DotWars.Map;
using DotWars.Units;

namespace DotWars.UI
{
    public class PortPanel : MonoBehaviour
    {
        private bool _visible;
        private Port _selectedPort;
        private List<Division> _nearbyUnits = new();
        private List<bool> _unitSelected = new();
        private string _errorMessage;
        private float _errorTimer;

        private GUIStyle _buttonStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _infoStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _toggleStyle;
        private bool _stylesInit;
        private Vector2 _scrollPos;

        private const float NearbyRadius = 6f;

        private void InitStyles()
        {
            _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold, fixedHeight = 40 };
            _headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _headerStyle.normal.textColor = Color.white;
            _infoStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            _infoStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            _errorStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _errorStyle.normal.textColor = Color.red;
            _toggleStyle = new GUIStyle(GUI.skin.toggle) { fontSize = 14 };
            _toggleStyle.normal.textColor = Color.white;
            _toggleStyle.onNormal.textColor = Color.white;
            _stylesInit = true;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            if (_errorTimer > 0) _errorTimer -= Time.deltaTime;

            if (Input.GetMouseButtonDown(0) && !_visible)
            {
                var cam = UnityEngine.Camera.main;
                Vector3 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
                worldPos.z = 0;

                var ports = FindObjectsByType<Port>(FindObjectsSortMode.None);
                foreach (var p in ports)
                {
                    if (p.OwnerIndex != 0) continue;
                    if (Vector2.Distance(worldPos, p.transform.position) < 1f)
                    {
                        OpenPort(p);
                        break;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
                _visible = false;
        }

        private void OpenPort(Port port)
        {
            _selectedPort = port;
            _visible = true;
            _nearbyUnits.Clear();
            _unitSelected.Clear();

            var divisions = FindObjectsByType<Division>(FindObjectsSortMode.None);
            foreach (var div in divisions)
            {
                if (div.OwnerIndex != 0 || div.IsShip) continue;
                float dist = Vector2.Distance(div.transform.position, port.transform.position);
                if (dist < NearbyRadius)
                {
                    _nearbyUnits.Add(div);
                    _unitSelected.Add(true);
                }
            }
        }

        private void OnGUI()
        {
            if (!_visible || _selectedPort == null) return;
            if (!_stylesInit) InitStyles();

            float w = 320;
            float h = 300 + _nearbyUnits.Count * 30;
            h = Mathf.Min(h, 500);
            float x = Screen.width / 2f - w / 2f;
            float y = Screen.height / 2f - h / 2f;

            // Background
            GUI.color = new Color(0, 0, 0, 0.85f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Border
            GUI.color = new Color(0.8f, 0.6f, 0.2f, 0.7f);
            DrawBorder(new Rect(x, y, w, h), 2);
            GUI.color = Color.white;

            GUI.Label(new Rect(x, y + 8, w, 30), "Port — Board Ships", _headerStyle);

            if (GUI.Button(new Rect(x + w - 35, y + 8, 28, 22), "X"))
            {
                _visible = false;
                return;
            }

            float cy = y + 45;

            if (_nearbyUnits.Count == 0)
            {
                GUI.Label(new Rect(x + 15, cy, w - 30, 30), "No land units nearby", _infoStyle);
            }
            else
            {
                GUI.Label(new Rect(x + 15, cy, w - 30, 25), $"Select units to board ({_nearbyUnits.Count} nearby):", _infoStyle);
                cy += 28;

                float scrollH = _nearbyUnits.Count * 28;
                float viewH = Mathf.Min(scrollH, 200);
                _scrollPos = GUI.BeginScrollView(new Rect(x + 10, cy, w - 20, viewH), _scrollPos, new Rect(0, 0, w - 40, scrollH));

                for (int i = 0; i < _nearbyUnits.Count; i++)
                {
                    var div = _nearbyUnits[i];
                    if (div == null) continue;
                    string label = $"  {div.Stats.divisionType} (HP: {(int)div.CurrentHP}/{(int)div.Stats.maxHP})";
                    _unitSelected[i] = GUI.Toggle(new Rect(5, i * 28, w - 50, 25), _unitSelected[i], label, _toggleStyle);
                }

                GUI.EndScrollView();
                cy += viewH + 10;

                int selectedCount = 0;
                for (int i = 0; i < _unitSelected.Count; i++)
                    if (_unitSelected[i]) selectedCount++;

                if (GUI.Button(new Rect(x + 15, cy, w - 30, 40), $"Board {selectedCount} units", _buttonStyle))
                {
                    BoardSelectedUnits();
                }
                cy += 45;
            }

            // Error message
            if (_errorTimer > 0 && !string.IsNullOrEmpty(_errorMessage))
            {
                GUI.Label(new Rect(x + 15, cy, w - 30, 25), _errorMessage, _errorStyle);
            }
        }

        private void BoardSelectedUnits()
        {
            var map = MapManager.Instance;
            var portGrid = map.WorldToGrid(_selectedPort.transform.position);
            var waterTiles = FindFreeWaterTiles(portGrid);

            int needed = 0;
            for (int i = 0; i < _unitSelected.Count; i++)
                if (_unitSelected[i]) needed++;

            if (waterTiles.Count < needed)
            {
                _errorMessage = "Not enough free water tiles nearby!";
                _errorTimer = 3f;
                return;
            }

            int waterIdx = 0;
            for (int i = 0; i < _nearbyUnits.Count; i++)
            {
                if (!_unitSelected[i] || _nearbyUnits[i] == null) continue;
                _nearbyUnits[i].MoveToAndConvert(waterTiles[waterIdx]);
                waterIdx++;
            }

            _visible = false;
        }

        private List<Vector2Int> FindFreeWaterTiles(Vector2Int portGrid)
        {
            var map = MapManager.Instance;
            var result = new List<Vector2Int>();

            // Search in expanding rings around port for water tiles adjacent to land
            for (int r = 1; r <= 3; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                        var pos = new Vector2Int(portGrid.x + dx, portGrid.y + dy);
                        if (!map.InBounds(pos)) continue;

                        var terrain = map.GetTerrainAt(pos);
                        if (terrain == null || terrain.terrainType != TerrainType.Water) continue;

                        if (!result.Contains(pos))
                            result.Add(pos);
                    }
                }
            }

            return result;
        }

        private void DrawBorder(Rect r, float t)
        {
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), Texture2D.whiteTexture);
        }
    }
}
