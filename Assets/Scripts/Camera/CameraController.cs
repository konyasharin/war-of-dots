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
        [SerializeField] private float maxZoom = 15f;

        private UnityEngine.Camera _camera;
        private Vector3 _dragOrigin;
        private bool _isDragging;

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
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
            var map = MapManager.Instance;
            if (map == null) return;

            var bounds = map.Bounds;
            float halfH = _camera.orthographicSize;
            float halfW = halfH * _camera.aspect;

            float minX = bounds.xMin + halfW;
            float maxX = bounds.xMax - halfW;
            float minY = bounds.yMin + halfH;
            float maxY = bounds.yMax - halfH;

            // If map is smaller than camera view, center on map
            if (minX > maxX) minX = maxX = (bounds.xMin + bounds.xMax) * 0.5f;
            if (minY > maxY) minY = maxY = (bounds.yMin + bounds.yMax) * 0.5f;

            var pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            transform.position = pos;
        }
    }
}
