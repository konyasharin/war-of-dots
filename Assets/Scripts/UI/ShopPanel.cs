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
        private GUIStyle _buttonStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _infoStyle;
        private bool _stylesInit;

        private void InitStyles()
        {
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                fixedHeight = 35
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _headerStyle.normal.textColor = Color.white;

            _infoStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft
            };
            _infoStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

            _stylesInit = true;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            // Check if player clicked on own city
            if (Input.GetMouseButtonDown(0))
            {
                var cam = UnityEngine.Camera.main;
                Vector3 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
                worldPos.z = 0;

                var cities = FindObjectsByType<City>(FindObjectsSortMode.None);
                City closest = null;
                float closestDist = 1f;

                foreach (var c in cities)
                {
                    if (c.OwnerIndex != 0) continue;
                    float dist = Vector2.Distance(worldPos, c.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = c;
                    }
                }

                if (closest != null)
                {
                    _selectedCity = closest;
                    _visible = true;
                }
                else if (_visible)
                {
                    // Check if click is outside panel area
                    var panelRect = new Rect(10, Screen.height / 2 - 120, 200, 240);
                    var mouseGUI = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                    if (!panelRect.Contains(mouseGUI))
                        _visible = false;
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
                _visible = false;
        }

        private void OnGUI()
        {
            if (!_visible || _selectedCity == null) return;
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            if (!_stylesInit) InitStyles();

            float x = 10;
            float y = Screen.height / 2f - 120;
            float w = 200;
            float h = 260;

            // Background
            GUI.color = new Color(0, 0, 0, 0.75f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Border
            GUI.color = new Color(0.3f, 0.5f, 1f, 0.6f);
            GUI.DrawTexture(new Rect(x, y, w, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x, y + h - 2, w, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x, y, 2, h), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + w - 2, y, 2, h), Texture2D.whiteTexture);
            GUI.color = Color.white;

            string cityName = _selectedCity.IsCapital ? "Capital" : "City";
            GUI.Label(new Rect(x, y + 5, w, 25), cityName, _headerStyle);

            float gold = EconomyManager.Instance != null ? EconomyManager.Instance.Gold[0] : 0;
            GUI.Label(new Rect(x + 10, y + 30, w - 20, 20), $"Gold: ${(int)gold}", _infoStyle);

            float by = y + 55;

            // Units
            GUI.Label(new Rect(x + 10, by, w - 20, 20), "Units:", _infoStyle);
            by += 22;

            if (GUI.Button(new Rect(x + 10, by, w - 20, 35), "Infantry  $100", _buttonStyle))
                BuyUnit(DivisionType.Infantry, 100);
            by += 40;

            if (GUI.Button(new Rect(x + 10, by, w - 20, 35), "Tank  $200", _buttonStyle))
                BuyUnit(DivisionType.Tank, 200);
            by += 45;

            // Buildings (future)
            GUI.Label(new Rect(x + 10, by, w - 20, 20), "Buildings:", _infoStyle);
            by += 22;

            GUI.enabled = false;
            GUI.Button(new Rect(x + 10, by, w - 20, 35), "Port  $150 (soon)", _buttonStyle);
            GUI.enabled = true;
            by += 40;

            // Close
            if (GUI.Button(new Rect(x + w - 30, y + 5, 25, 20), "X"))
                _visible = false;
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
