using System.Collections.Generic;
using UnityEngine;
using DotWars.Core;
using DotWars.Map;

namespace DotWars.Units
{
    public class SelectionManager : MonoBehaviour
    {
        public static SelectionManager Instance { get; private set; }

        [SerializeField] private LayerMask divisionLayer;
        [SerializeField] private float clickRadius = 0.3f;

        private readonly List<Division> _selected = new();
        private UnityEngine.Camera _camera;

        // Box selection
        private bool _isBoxSelecting;
        private Vector3 _boxStartScreen;
        private Vector3 _boxStartWorld;

        // Line formation (Ctrl + RMB drag)
        private bool _isLineForming;
        private Vector3 _lineStartWorld;
        private float _lineSpacing = 1.5f;

        public IReadOnlyList<Division> Selected => _selected;

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
            _camera = UnityEngine.Camera.main;
        }

        private void Update()
        {
            if (GameManager.Instance.State != GameState.Playing) return;

            HandleBoxSelection();

            if (Input.GetMouseButtonDown(1))
            {
                if (Input.GetKey(KeyCode.LeftControl) && _selected.Count > 1)
                {
                    _isLineForming = true;
                    _lineStartWorld = _camera.ScreenToWorldPoint(Input.mousePosition);
                    _lineStartWorld.z = 0;
                }
                else
                {
                    HandleRightClick();
                }
            }

            CameraSystem.CameraController.BlockZoom = _isLineForming;

            if (_isLineForming)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (!Mathf.Approximately(scroll, 0f))
                    _lineSpacing = Mathf.Clamp(_lineSpacing + scroll * 2f, 0.5f, 5f);

                if (Input.GetMouseButtonUp(1))
                {
                    FinishLineFormation();
                    _isLineForming = false;
                }
            }
        }

        private void HandleBoxSelection()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _boxStartScreen = Input.mousePosition;
                _boxStartWorld = _camera.ScreenToWorldPoint(Input.mousePosition);
                _boxStartWorld.z = 0;
                _isBoxSelecting = false;

                // Try click-select first
                var hit = Physics2D.OverlapCircle(_boxStartWorld, clickRadius, divisionLayer);
                if (hit != null)
                {
                    var division = hit.GetComponent<Division>();
                    if (division != null && division.OwnerIndex == 0)
                    {
                        if (!Input.GetKey(KeyCode.LeftShift))
                            ClearSelection();

                        if (!_selected.Contains(division))
                        {
                            _selected.Add(division);
                            division.SetSelected(true);
                        }
                        return;
                    }
                }
            }

            if (Input.GetMouseButton(0))
            {
                float dragDist = Vector3.Distance(_boxStartScreen, Input.mousePosition);
                if (dragDist > 10f)
                    _isBoxSelecting = true;
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (_isBoxSelecting)
                {
                    FinishBoxSelection();
                    _isBoxSelecting = false;
                }
                else if (!Input.GetKey(KeyCode.LeftShift))
                {
                    // Clicked on empty space without drag
                    var worldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
                    worldPos.z = 0;
                    var hit = Physics2D.OverlapCircle(worldPos, clickRadius, divisionLayer);
                    if (hit == null)
                        ClearSelection();
                }
            }
        }

        private void FinishBoxSelection()
        {
            if (!Input.GetKey(KeyCode.LeftShift))
                ClearSelection();

            Vector3 endWorld = _camera.ScreenToWorldPoint(Input.mousePosition);
            endWorld.z = 0;

            float minX = Mathf.Min(_boxStartWorld.x, endWorld.x);
            float maxX = Mathf.Max(_boxStartWorld.x, endWorld.x);
            float minY = Mathf.Min(_boxStartWorld.y, endWorld.y);
            float maxY = Mathf.Max(_boxStartWorld.y, endWorld.y);

            var divisions = FindObjectsByType<Division>(FindObjectsSortMode.None);
            foreach (var div in divisions)
            {
                if (div.OwnerIndex != 0) continue;

                var pos = div.transform.position;
                if (pos.x >= minX && pos.x <= maxX && pos.y >= minY && pos.y <= maxY)
                {
                    if (!_selected.Contains(div))
                    {
                        _selected.Add(div);
                        div.SetSelected(true);
                    }
                }
            }
        }

        private void HandleRightClick()
        {
            if (_selected.Count == 0) return;

            Vector3 worldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
            var targetGrid = MapManager.Instance.WorldToGrid(worldPos);

            if (!MapManager.Instance.IsPassable(targetGrid)) return;

            if (_selected.Count == 1)
            {
                _selected[0].MoveTo(targetGrid);
                return;
            }

            // Spread units around target in a formation
            var targets = CalculateFormation(targetGrid, _selected.Count);
            for (int i = 0; i < _selected.Count; i++)
            {
                if (_selected[i] != null)
                    _selected[i].MoveTo(targets[i]);
            }
        }

        private List<Vector2Int> CalculateFormation(Vector2Int center, int count)
        {
            var result = new List<Vector2Int>(count);
            var used = new HashSet<Vector2Int>();
            var map = MapManager.Instance;

            // First unit goes to center
            result.Add(center);
            used.Add(center);

            if (count == 1) return result;

            // Spiral outward from center to find passable positions
            int radius = 1;
            while (result.Count < count && radius < 10)
            {
                for (int dx = -radius; dx <= radius && result.Count < count; dx++)
                {
                    for (int dy = -radius; dy <= radius && result.Count < count; dy++)
                    {
                        if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius) continue;

                        var pos = new Vector2Int(center.x + dx, center.y + dy);
                        if (!used.Contains(pos) && map.IsPassable(pos))
                        {
                            result.Add(pos);
                            used.Add(pos);
                        }
                    }
                }
                radius++;
            }

            return result;
        }

        private void FinishLineFormation()
        {
            Vector3 endWorld = _camera.ScreenToWorldPoint(Input.mousePosition);
            endWorld.z = 0;

            Vector2 dir = ((Vector2)endWorld - (Vector2)_lineStartWorld).normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;

            // Perpendicular direction for the line
            Vector2 lineDir = new Vector2(-dir.y, dir.x);
            Vector2 center = (Vector2)_lineStartWorld;

            int count = _selected.Count;
            float totalWidth = (count - 1) * _lineSpacing;
            Vector2 start = center - lineDir * (totalWidth * 0.5f);

            for (int i = 0; i < count; i++)
            {
                if (_selected[i] == null) continue;
                Vector2 pos = start + lineDir * (i * _lineSpacing);
                var gridPos = MapManager.Instance.WorldToGrid(pos);
                if (MapManager.Instance.IsPassable(gridPos))
                    _selected[i].MoveTo(gridPos);
            }
        }

        public void ClearSelection()
        {
            foreach (var div in _selected)
            {
                if (div != null)
                    div.SetSelected(false);
            }
            _selected.Clear();
        }

        private void OnGUI()
        {
            if (_isLineForming)
                DrawLineFormationPreview();

            if (!_isBoxSelecting) return;

            var start = _boxStartScreen;
            var end = Input.mousePosition;

            // Flip Y for GUI coordinates
            start.y = Screen.height - start.y;
            end.y = Screen.height - end.y;

            var rect = new Rect(
                Mathf.Min(start.x, end.x),
                Mathf.Min(start.y, end.y),
                Mathf.Abs(end.x - start.x),
                Mathf.Abs(end.y - start.y)
            );

            // Semi-transparent fill
            GUI.color = new Color(0.2f, 0.5f, 1f, 0.15f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            // Border
            GUI.color = new Color(0.2f, 0.5f, 1f, 0.6f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1, rect.width, 1), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 1, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - 1, rect.y, 1, rect.height), Texture2D.whiteTexture);
        }

        private void DrawLineFormationPreview()
        {
            if (_selected.Count < 2) return;

            Vector3 endWorld = _camera.ScreenToWorldPoint(Input.mousePosition);
            endWorld.z = 0;

            Vector2 dir = ((Vector2)endWorld - (Vector2)_lineStartWorld).normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
            Vector2 lineDir = new Vector2(-dir.y, dir.x);
            Vector2 center = (Vector2)_lineStartWorld;

            int count = _selected.Count;
            float totalWidth = (count - 1) * _lineSpacing;
            Vector2 start = center - lineDir * (totalWidth * 0.5f);

            // Draw dots at formation positions
            for (int i = 0; i < count; i++)
            {
                Vector2 worldPos = start + lineDir * (i * _lineSpacing);
                Vector3 screenPos = _camera.WorldToScreenPoint(worldPos);
                screenPos.y = Screen.height - screenPos.y;

                GUI.color = new Color(0.2f, 0.8f, 0.2f, 0.7f);
                GUI.DrawTexture(new Rect(screenPos.x - 4, screenPos.y - 4, 8, 8), Texture2D.whiteTexture);
            }

            // Info text
            GUI.color = Color.white;
            var style = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = new Color(0.2f, 0.8f, 0.2f);
            GUI.Label(new Rect(Screen.width / 2f - 200, Screen.height - 50, 400, 30),
                $"Line Formation — Spacing: {_lineSpacing:F1}  (Scroll to adjust)", style);
        }
    }
}
