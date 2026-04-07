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

        private GUIStyle _btnStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _infoStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _toggleStyle;
        private GUIStyle _closeBtnStyle;
        private bool _stylesInit;
        private Vector2 _scrollPos;

        private const float NearbyRadius = 6f;

        public bool IsVisible => _visible;

        private void InitStyles()
        {
            _btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold, fixedHeight = 50 };
            _headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _headerStyle.normal.textColor = Color.white;
            _infoStyle = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            _infoStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            _errorStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _errorStyle.normal.textColor = Color.red;
            _toggleStyle = new GUIStyle(GUI.skin.toggle) { fontSize = 16 };
            _toggleStyle.normal.textColor = Color.white;
            _toggleStyle.onNormal.textColor = Color.white;
            _closeBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold };
            _stylesInit = true;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            if (_errorTimer > 0) _errorTimer -= Time.unscaledDeltaTime;

            if (Input.GetMouseButtonDown(0) && !_visible)
            {
                var cam = UnityEngine.Camera.main;
                Vector3 wp = cam.ScreenToWorldPoint(Input.mousePosition);
                wp.z = 0;

                var ports = FindObjectsByType<Port>(FindObjectsSortMode.None);
                foreach (var p in ports)
                {
                    if (p.OwnerIndex == 0 && Vector2.Distance(wp, p.transform.position) < 1f)
                    {
                        OpenPort(p);
                        break;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape)) _visible = false;
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
                if (Vector2.Distance(div.transform.position, port.transform.position) < NearbyRadius)
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

            float w = Screen.width * 0.2f;
            float h = Screen.height;

            // Background
            GUI.color = new Color(0.05f, 0.05f, 0.1f, 0.92f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = new Color(0.8f, 0.6f, 0.2f, 0.5f);
            GUI.DrawTexture(new Rect(w - 2, 0, 2, h), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float pad = 20;
            float cy = pad;

            GUI.Label(new Rect(pad, cy, w - pad * 2, 40), "PORT", _headerStyle);
            if (GUI.Button(new Rect(w - 45, 10, 35, 30), "X", _closeBtnStyle))
            {
                _visible = false;
                return;
            }
            cy += 50;

            GUI.Label(new Rect(pad, cy, w - pad * 2, 28), "Board Ships", _infoStyle);
            cy += 35;

            GUI.color = new Color(0.4f, 0.4f, 0.5f, 0.4f);
            GUI.DrawTexture(new Rect(pad, cy, w - pad * 2, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
            cy += 15;

            if (_nearbyUnits.Count == 0)
            {
                GUI.Label(new Rect(pad, cy, w - pad * 2, 30), "No land units nearby", _infoStyle);
            }
            else
            {
                GUI.Label(new Rect(pad, cy, w - pad * 2, 25), $"Nearby units ({_nearbyUnits.Count}):", _infoStyle);
                cy += 30;

                float scrollH = _nearbyUnits.Count * 32;
                float viewH = Mathf.Min(scrollH, h * 0.4f);
                _scrollPos = GUI.BeginScrollView(new Rect(pad, cy, w - pad * 2, viewH), _scrollPos, new Rect(0, 0, w - pad * 3, scrollH));

                for (int i = 0; i < _nearbyUnits.Count; i++)
                {
                    var div = _nearbyUnits[i];
                    if (div == null) continue;
                    string label = $"  {div.Stats.divisionType} (HP: {(int)div.CurrentHP})";
                    _unitSelected[i] = GUI.Toggle(new Rect(5, i * 32, w - pad * 3 - 10, 28), _unitSelected[i], label, _toggleStyle);
                }
                GUI.EndScrollView();
                cy += viewH + 15;

                int count = 0;
                for (int i = 0; i < _unitSelected.Count; i++)
                    if (_unitSelected[i]) count++;

                if (GUI.Button(new Rect(pad, cy, w - pad * 2, 50), $"Board {count} units", _btnStyle))
                    BoardSelectedUnits();
                cy += 60;
            }

            if (_errorTimer > 0 && !string.IsNullOrEmpty(_errorMessage))
                GUI.Label(new Rect(pad, cy, w - pad * 2, 25), _errorMessage, _errorStyle);
        }

        private void BoardSelectedUnits()
        {
            var portGrid = MapManager.Instance.WorldToGrid(_selectedPort.transform.position);
            var waterTiles = FindFreeWaterTiles(portGrid);

            int needed = 0;
            for (int i = 0; i < _unitSelected.Count; i++)
                if (_unitSelected[i]) needed++;

            if (waterTiles.Count < needed)
            {
                _errorMessage = "Not enough free water nearby!";
                _errorTimer = 3f;
                return;
            }

            int wi = 0;
            for (int i = 0; i < _nearbyUnits.Count; i++)
            {
                if (!_unitSelected[i] || _nearbyUnits[i] == null) continue;
                _nearbyUnits[i].MoveToAndConvert(waterTiles[wi++]);
            }
            _visible = false;
        }

        private List<Vector2Int> FindFreeWaterTiles(Vector2Int portGrid)
        {
            var map = MapManager.Instance;
            var result = new List<Vector2Int>();
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
                        if (terrain != null && terrain.terrainType == TerrainType.Water && !result.Contains(pos))
                            result.Add(pos);
                    }
                }
            }
            return result;
        }
    }
}
