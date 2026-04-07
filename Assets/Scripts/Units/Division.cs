using System.Collections.Generic;
using UnityEngine;
using DotWars.Core;
using DotWars.Map;

namespace DotWars.Units
{
    public class Division : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteRenderer selectionRing;

        public DivisionStats Stats { get; private set; }
        public int OwnerIndex { get; private set; } // 0 = player, 1 = bot
        public float CurrentHP { get; private set; }
        public float CurrentMorale { get; private set; }
        public bool IsMoving => _path != null && _pathIndex < _path.Count;

        private List<Vector2Int> _path;
        private int _pathIndex;
        private Vector3 _moveTarget;
        private bool _selected;
        private Rigidbody2D _rigidbody;
        private LineRenderer _lineRenderer;

        private static readonly Color PlayerColor = new(0.2f, 0.5f, 1f);
        private static readonly Color BotColor = new(1f, 0.25f, 0.25f);
        private static readonly Color PathColor = new(0.5f, 0.5f, 0.5f, 0.4f);

        public void Initialize(DivisionStats stats, int ownerIndex, Vector2Int gridPos)
        {
            Stats = stats;
            OwnerIndex = ownerIndex;
            CurrentHP = stats.maxHP;
            CurrentMorale = GameManager.Instance.Config.maxMorale;

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (selectionRing == null)
            {
                var ringTransform = transform.Find("SelectionRing");
                if (ringTransform != null)
                    selectionRing = ringTransform.GetComponent<SpriteRenderer>();
            }

            _rigidbody = GetComponent<Rigidbody2D>();
            _lineRenderer = GetComponent<LineRenderer>();
            if (_lineRenderer != null)
                _lineRenderer.positionCount = 0;

            spriteRenderer.color = ownerIndex == 0 ? PlayerColor : BotColor;
            transform.position = MapManager.Instance.GridToWorld(gridPos);

            SetSelected(false);
            EventBus.OnDivisionSpawned?.Invoke(gameObject);
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            if (selectionRing != null)
                selectionRing.enabled = selected;
        }

        public void MoveTo(Vector2Int targetGrid)
        {
            var currentGrid = MapManager.Instance.WorldToGrid(transform.position);
            bool isInfantry = Stats.divisionType == DivisionType.Infantry;
            _path = Pathfinding.FindPath(currentGrid, targetGrid, isInfantry);

            if (_path != null && _path.Count > 1)
            {
                _pathIndex = 1;
                _moveTarget = MapManager.Instance.GridToWorld(_path[_pathIndex]);
                UpdatePathLine();
            }
            else
            {
                ClearPathLine();
            }
        }

        private void Update()
        {
            if (GameManager.Instance.State != GameState.Playing) return;

            if (IsMoving)
                ProcessMovement();
        }

        private void ProcessMovement()
        {
            var terrain = MapManager.Instance.GetTerrainAtWorld(transform.position);
            bool isInfantry = Stats.divisionType == DivisionType.Infantry;
            float speedMod = terrain != null ? terrain.GetSpeedModifier(isInfantry) : 1f;
            float speed = Stats.baseSpeed * speedMod;

            var newPos = Vector3.MoveTowards(
                transform.position,
                _moveTarget,
                speed * Time.deltaTime
            );

            if (_rigidbody != null)
                _rigidbody.MovePosition(newPos);
            else
                transform.position = newPos;

            // Update path line start point
            UpdatePathLineStart();

            if (Vector3.Distance(transform.position, _moveTarget) < 0.01f)
            {
                transform.position = _moveTarget;
                _pathIndex++;

                if (_pathIndex < _path.Count)
                {
                    _moveTarget = MapManager.Instance.GridToWorld(_path[_pathIndex]);
                    UpdatePathLine();
                }
                else
                {
                    _path = null;
                    ClearPathLine();
                }
            }
        }

        private void UpdatePathLine()
        {
            if (_lineRenderer == null || _path == null) return;

            int remaining = _path.Count - _pathIndex + 1; // +1 for current position
            _lineRenderer.positionCount = remaining;
            _lineRenderer.SetPosition(0, transform.position);

            for (int i = 1; i < remaining; i++)
            {
                var worldPos = MapManager.Instance.GridToWorld(_path[_pathIndex + i - 1]);
                _lineRenderer.SetPosition(i, worldPos);
            }
        }

        private void UpdatePathLineStart()
        {
            if (_lineRenderer == null || _lineRenderer.positionCount == 0) return;
            _lineRenderer.SetPosition(0, transform.position);
        }

        private void ClearPathLine()
        {
            if (_lineRenderer != null)
                _lineRenderer.positionCount = 0;
        }

        public void TakeDamage(float amount)
        {
            CurrentHP -= amount;
            if (CurrentHP <= 0f)
                Die();
        }

        public void ModifyMorale(float amount)
        {
            CurrentMorale = Mathf.Clamp(CurrentMorale + amount, 0f, GameManager.Instance.Config.maxMorale);
        }

        public void Heal(float amount)
        {
            CurrentHP = Mathf.Min(CurrentHP + amount, Stats.maxHP);
        }

        private void Die()
        {
            EventBus.OnDivisionDestroyed?.Invoke(gameObject);
            Destroy(gameObject);
        }
    }
}
