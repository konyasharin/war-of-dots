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

            if (Input.GetMouseButtonDown(0))
                HandleLeftClick();
            else if (Input.GetMouseButtonDown(1))
                HandleRightClick();
        }

        private void HandleLeftClick()
        {
            Vector3 worldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
            worldPos.z = 0;

            var hit = Physics2D.OverlapCircle(worldPos, clickRadius, divisionLayer);

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

            ClearSelection();
        }

        private void HandleRightClick()
        {
            if (_selected.Count == 0) return;

            Vector3 worldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
            var targetGrid = MapManager.Instance.WorldToGrid(worldPos);

            if (!MapManager.Instance.IsPassable(targetGrid)) return;

            foreach (var division in _selected)
            {
                if (division != null)
                    division.MoveTo(targetGrid);
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
    }
}
