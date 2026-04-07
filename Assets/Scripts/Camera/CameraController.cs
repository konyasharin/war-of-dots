using UnityEngine;
using DotWars.Map;

namespace DotWars.CameraSystem
{
    public class CameraController : MonoBehaviour
    {
        [Header("Pan")]
        [SerializeField] private float panSpeed = 10f;
        [SerializeField] private float edgePanThreshold = 20f;
        [SerializeField] private bool enableEdgePan = true;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float minZoom = 3f;
        [SerializeField] private float maxZoom = 30f;

        private UnityEngine.Camera _camera;
        private Vector3 _dragOrigin;
        private bool _isDragging;
        private bool _boundsReady;
        private float _mapMinX, _mapMaxX, _mapMinY, _mapMaxY;

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
        }

        private void Start()
        {
            // Default zoom — small enough to pan freely
            _camera.orthographicSize = 7f;
            CacheMapBounds();
        }

        private void CacheMapBounds()
        {
            var map = MapManager.Instance;
            if (map == null) return;

            var wMin = map.WorldMin;
            var wMax = map.WorldMax;
            _mapMinX = wMin.x - 0.5f;
            _mapMaxX = wMax.x + 0.5f;
            _mapMinY = wMin.y - 0.5f;
            _mapMaxY = wMax.y + 0.5f;
            _boundsReady = true;
        }

        private void Update()
        {
            HandleKeyboardPan();

            if (enableEdgePan)
                HandleEdgePan();

            HandleMiddleMouseDrag();
            HandleZoom();
            ClampToMap();
        }

        private void HandleKeyboardPan()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            if (h == 0 && v == 0) return;

            var move = new Vector3(h, v, 0).normalized * (panSpeed * Time.unscaledDeltaTime);
            transform.position += move;
        }

        private void HandleEdgePan()
        {
            var mousePos = Input.mousePosition;
            var move = Vector3.zero;

            if (mousePos.x < edgePanThreshold) move.x = -1;
            else if (mousePos.x > Screen.width - edgePanThreshold) move.x = 1;

            if (mousePos.y < edgePanThreshold) move.y = -1;
            else if (mousePos.y > Screen.height - edgePanThreshold) move.y = 1;

            if (move != Vector3.zero)
                transform.position += move.normalized * (panSpeed * Time.unscaledDeltaTime);
        }

        private void HandleMiddleMouseDrag()
        {
            if (Input.GetMouseButtonDown(2))
            {
                _dragOrigin = _camera.ScreenToWorldPoint(Input.mousePosition);
                _isDragging = true;
            }

            if (Input.GetMouseButtonUp(2))
                _isDragging = false;

            if (_isDragging)
            {
                var currentPos = _camera.ScreenToWorldPoint(Input.mousePosition);
                transform.position += _dragOrigin - currentPos;
            }
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Approximately(scroll, 0f)) return;

            float newSize = _camera.orthographicSize - scroll * zoomSpeed;
            _camera.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        }

        private void ClampToMap()
        {
            if (!_boundsReady)
            {
                CacheMapBounds();
                if (!_boundsReady) return;
            }

            float halfH = _camera.orthographicSize;
            float halfW = halfH * _camera.aspect;

            // Camera edge must not go more than 2 tiles past map edge
            float overflow = 2f;

            float minX = _mapMinX - overflow + halfW;
            float maxX = _mapMaxX + overflow - halfW;
            float minY = _mapMinY - overflow + halfH;
            float maxY = _mapMaxY + overflow - halfH;

            if (minX > maxX) { float mid = (_mapMinX + _mapMaxX) * 0.5f; minX = maxX = mid; }
            if (minY > maxY) { float mid = (_mapMinY + _mapMaxY) * 0.5f; minY = maxY = mid; }

            var pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            transform.position = pos;
        }

        public void CenterOn(Vector3 worldPos)
        {
            var pos = worldPos;
            pos.z = transform.position.z;
            transform.position = pos;
        }
    }
}
