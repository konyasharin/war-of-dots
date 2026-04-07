using UnityEngine;
using DotWars.Core;
using DotWars.Economy;
using DotWars.Units;
using DotWars.Map;

namespace DotWars.UI
{
    public class ShopPanel : MonoBehaviour
    {
        private bool _visible;
        private City _selectedCity;
        private GUIStyle _btnStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _infoStyle;
        private GUIStyle _closeBtnStyle;
        private bool _stylesInit;

        public bool IsVisible => _visible;
        public void Close() { _visible = false; }

        private void InitStyles()
        {
            _btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold, fixedHeight = 50 };
            _headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _headerStyle.normal.textColor = Color.white;
            _infoStyle = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            _infoStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            _closeBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold };
            _stylesInit = true;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            if (Input.GetMouseButtonDown(0) && !_visible)
            {
                var cam = UnityEngine.Camera.main;
                Vector3 wp = cam.ScreenToWorldPoint(Input.mousePosition);
                wp.z = 0;

                // Check if clicking on a port (PortPanel handles that)
                var ports = FindObjectsByType<Port>(FindObjectsSortMode.None);
                foreach (var p in ports)
                {
                    if (p.OwnerIndex == 0 && Vector2.Distance(wp, p.transform.position) < 1f)
                        return; // Let PortPanel handle it
                }

                var cities = FindObjectsByType<City>(FindObjectsSortMode.None);
                foreach (var c in cities)
                {
                    if (c.OwnerIndex == 0 && Vector2.Distance(wp, c.transform.position) < 1f)
                    {
                        _selectedCity = c;
                        _visible = true;
                        // Close port panel
                        var portPanel = FindAnyObjectByType<PortPanel>();
                        if (portPanel != null && portPanel.IsVisible)
                            portPanel.Close();
                        return;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape)) _visible = false;
        }

        private void OnGUI()
        {
            if (!_visible || _selectedCity == null) return;
            if (!_stylesInit) InitStyles();

            float w = Screen.width * 0.2f;
            float h = Screen.height;
            float x = 0;
            float y = 0;

            // Background
            GUI.color = new Color(0.05f, 0.05f, 0.1f, 0.92f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = new Color(0.3f, 0.5f, 1f, 0.5f);
            GUI.DrawTexture(new Rect(w - 2, 0, 2, h), Texture2D.whiteTexture); // right edge
            GUI.color = Color.white;

            float pad = 20;
            float cy = pad;

            // Header
            string name = _selectedCity.IsCapital ? "CAPITAL" : "CITY";
            GUI.Label(new Rect(x + pad, cy, w - pad * 2, 40), name, _headerStyle);
            cy += 50;

            // Close
            if (GUI.Button(new Rect(w - 45, 10, 35, 30), "X", _closeBtnStyle))
            {
                _visible = false;
                return;
            }

            // Gold
            float gold = EconomyManager.Instance != null ? EconomyManager.Instance.Gold[0] : 0;
            GUI.Label(new Rect(x + pad, cy, w - pad * 2, 28), $"Gold: ${(int)gold}", _infoStyle);
            cy += 40;

            // Separator
            GUI.color = new Color(0.4f, 0.4f, 0.5f, 0.4f);
            GUI.DrawTexture(new Rect(x + pad, cy, w - pad * 2, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
            cy += 15;

            GUI.Label(new Rect(x + pad, cy, w - pad * 2, 28), "RECRUIT", _infoStyle);
            cy += 35;

            if (GUI.Button(new Rect(x + pad, cy, w - pad * 2, 50), "Infantry  $100", _btnStyle))
                BuyUnit(DivisionType.Infantry, 100);
            cy += 60;

            if (GUI.Button(new Rect(x + pad, cy, w - pad * 2, 50), "Tank  $200", _btnStyle))
                BuyUnit(DivisionType.Tank, 200);
            cy += 70;

            // Separator
            GUI.color = new Color(0.4f, 0.4f, 0.5f, 0.4f);
            GUI.DrawTexture(new Rect(x + pad, cy, w - pad * 2, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
            cy += 15;

            GUI.Label(new Rect(x + pad, cy, w - pad * 2, 28), "BUILD", _infoStyle);
            cy += 35;

            GUI.enabled = false;
            GUI.Button(new Rect(x + pad, cy, w - pad * 2, 50), "Port  $150 (soon)", _btnStyle);
            GUI.enabled = true;
        }

        private void BuyUnit(DivisionType type, int cost)
        {
            if (EconomyManager.Instance == null || DivisionSpawner.Instance == null) return;
            if (!EconomyManager.Instance.SpendGold(0, cost)) return;

            var cityGrid = MapManager.Instance.WorldToGrid(_selectedCity.transform.position);
            var spawnPos = FindFreeSpot(cityGrid);
            DivisionSpawner.Instance.Spawn(type, 0, spawnPos);
        }

        private Vector2Int FindFreeSpot(Vector2Int center)
        {
            Vector2Int[] offsets = {
                new(0, -1), new(0, 1), new(1, 0), new(-1, 0),
                new(1, -1), new(1, 1), new(-1, -1), new(-1, 1),
                new(0, -2), new(0, 2), new(2, 0), new(-2, 0)
            };
            foreach (var off in offsets)
            {
                var pos = center + off;
                if (MapManager.Instance.IsPassable(pos))
                    return pos;
            }
            return center;
        }
    }
}
