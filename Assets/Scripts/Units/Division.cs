using System.Collections.Generic;
using UnityEngine;
using DotWars.Core;
using DotWars.Map;

namespace DotWars.Units
{
    public class Division : MonoBehaviour
    {
        public DivisionStats Stats { get; private set; }
        public int OwnerIndex { get; private set; }
        public float CurrentHP { get; private set; }
        public float CurrentMorale { get; private set; }
        public bool IsMoving => _path != null && _pathIndex < _path.Count;
        public bool InCombat { get; private set; }
        public bool IsShip { get; private set; }

        private Transform _visual;
        private SpriteRenderer _spriteRenderer;
        private SpriteRenderer _selectionRing;
        private SpriteRenderer _hpBarBg;
        private SpriteRenderer _hpBarFill;
        private SpriteRenderer _crackOverlay;
        private SpriteRenderer _tankDot;
        private Rigidbody2D _rigidbody;
        private LineRenderer _lineRenderer;

        private List<Vector2Int> _path;
        private int _pathIndex;
        private Vector3 _moveTarget;
        private bool _selected;

        private float _shakeTimer;
        private float _hpFillOffsetX;
        private bool _convertOnArrival;
        private float _stuckTimer;
        private Vector3 _lastStuckCheckPos;

        private Sprite _crackLightSprite;
        private Sprite _crackHeavySprite;
        private Sprite _originalSprite;
        private Sprite _shipSprite;
        private Sprite _shipOutlineSprite;
        private Sprite _originalOutlineSprite;
        private SpriteRenderer _outlineRenderer;
        private float _landingTimer;
        private bool _isLanding;

        private static readonly Color PlayerColor = new(0.2f, 0.5f, 1f);
        private static readonly Color BotColor = new(1f, 0.25f, 0.25f);

        public void Initialize(DivisionStats stats, int ownerIndex, Vector2Int gridPos)
        {
            Stats = stats;
            OwnerIndex = ownerIndex;
            CurrentHP = stats.maxHP;
            CurrentMorale = GameManager.Instance.Config.maxMorale;

            _rigidbody = GetComponent<Rigidbody2D>();
            _lineRenderer = GetComponent<LineRenderer>();
            if (_lineRenderer != null)
                _lineRenderer.positionCount = 0;

            _visual = transform.Find("Visual");
            if (_visual != null)
                _spriteRenderer = _visual.GetComponent<SpriteRenderer>();
            else
                _spriteRenderer = GetComponent<SpriteRenderer>();

            var ringT = _visual != null ? _visual.Find("SelectionRing") : transform.Find("SelectionRing");
            if (ringT != null) _selectionRing = ringT.GetComponent<SpriteRenderer>();

            var tankDotT = _visual != null ? _visual.Find("TankDot") : transform.Find("TankDot");
            if (tankDotT != null) _tankDot = tankDotT.GetComponent<SpriteRenderer>();

            var crackT = _visual != null ? _visual.Find("CrackOverlay") : transform.Find("CrackOverlay");
            if (crackT != null) _crackOverlay = crackT.GetComponent<SpriteRenderer>();

            var hpBgT = transform.Find("HPBarBg");
            if (hpBgT != null) _hpBarBg = hpBgT.GetComponent<SpriteRenderer>();

            var hpFillT = transform.Find("HPBarFill");
            if (hpFillT != null) _hpBarFill = hpFillT.GetComponent<SpriteRenderer>();

            _crackLightSprite = Resources.Load<Sprite>("Sprites/CrackLight");
            _crackHeavySprite = Resources.Load<Sprite>("Sprites/CrackHeavy");
            _shipSprite = Resources.Load<Sprite>("Sprites/Ship");
            _shipOutlineSprite = Resources.Load<Sprite>("Sprites/ShipOutline");
            _originalSprite = _spriteRenderer.sprite;

            var outlineT = _visual != null ? _visual.Find("Outline") : transform.Find("Outline");
            if (outlineT != null)
            {
                _outlineRenderer = outlineT.GetComponent<SpriteRenderer>();
                _originalOutlineSprite = _outlineRenderer?.sprite;
            }

            _spriteRenderer.color = ownerIndex == 0 ? PlayerColor : BotColor;

            // Tank: show center dot
            bool isTank = stats.divisionType == DivisionType.Tank;
            if (isTank && _tankDot != null)
                _tankDot.enabled = true;

            // Set position
            var worldPos = MapManager.Instance.GridToWorld(gridPos);
            if (_rigidbody != null)
            {
                _rigidbody.position = worldPos;
                _rigidbody.linearVelocity = Vector2.zero;
            }
            transform.position = worldPos;

            SetSelected(false);
            UpdateHPBar();
            EventBus.OnDivisionSpawned?.Invoke(gameObject);
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            if (_selectionRing != null)
                _selectionRing.enabled = selected;
        }

        public void MoveTo(Vector2Int targetGrid)
        {
            var currentGrid = MapManager.Instance.WorldToGrid(transform.position);
            bool isInfantry = Stats.divisionType == DivisionType.Infantry;
            _path = Pathfinding.FindPath(currentGrid, targetGrid, isInfantry, IsShip);

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
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            if (IsMoving)
                ProcessMovement();

            ProcessCombat();
            ProcessLanding();
            ProcessShake();
        }

        private void ProcessMovement()
        {
            var terrain = MapManager.Instance.GetTerrainAtWorld(transform.position);
            bool isInfantry = Stats.divisionType == DivisionType.Infantry;
            float speedMod = terrain != null ? terrain.GetSpeedModifier(isInfantry) : 1f;
            float speed = Stats.baseSpeed * speedMod;
            if (IsShip) speed *= 3f;

            // Stuck detection
            _stuckTimer += Time.deltaTime;
            if (_stuckTimer > 0.8f)
            {
                float moved = Vector3.Distance(transform.position, _lastStuckCheckPos);
                if (moved < 0.02f && _path != null)
                {
                    // Nudge sideways
                    Vector2 toTarget = ((Vector2)_moveTarget - (Vector2)transform.position).normalized;
                    Vector2 side = new Vector2(-toTarget.y, toTarget.x) * 0.4f;
                    if (Random.value > 0.5f) side = -side;
                    transform.position += (Vector3)side;
                }
                _lastStuckCheckPos = transform.position;
                _stuckTimer = 0;
            }

            Vector2 direction = ((Vector2)_moveTarget - (Vector2)transform.position);
            if (direction.magnitude > 0.05f)
            {
                // Calculate desired position
                Vector2 desiredPos = (Vector2)transform.position + direction.normalized * speed * Time.deltaTime;

                var nearby = Physics2D.OverlapCircleAll(desiredPos, 0.35f);
                foreach (var col in nearby)
                {
                    if (col.gameObject == gameObject) continue;
                    var other = col.GetComponent<Division>();
                    if (other == null) continue;

                    Vector2 away = (desiredPos - (Vector2)other.transform.position).normalized;
                    Vector2 perpendicular = new Vector2(-direction.normalized.y, direction.normalized.x);
                    float dot = Vector2.Dot(away, perpendicular);
                    Vector2 steerDir = (dot >= 0 ? perpendicular : -perpendicular);

                    desiredPos = (Vector2)transform.position + (direction.normalized * 0.3f + steerDir * 0.7f).normalized * speed * Time.deltaTime;
                    break;
                }

                _rigidbody.MovePosition(desiredPos);
            }
            else
            {
                _rigidbody.MovePosition(_moveTarget);
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

                    if (_convertOnArrival)
                    {
                        _convertOnArrival = false;
                        if (!IsShip) ConvertToShip();
                        else ConvertToLand();
                    }
                }
            }

            UpdatePathLineStart();
        }

        private void ProcessCombat()
        {
            InCombat = false;

            var colliders = Physics2D.OverlapCircleAll(transform.position, 0.6f);
            foreach (var col in colliders)
            {
                if (col.gameObject == gameObject) continue;
                var other = col.GetComponent<Division>();
                if (other == null || other.OwnerIndex == OwnerIndex) continue;

                InCombat = true;

                // Ship can't attack land units
                if (IsShip && !other.IsShip)
                {
                    _shakeTimer = 0.1f;
                    break;
                }

                float damage = Stats.damagePerSec * Time.deltaTime;
                var terrain = MapManager.Instance.GetTerrainAtWorld(transform.position);
                bool isTank = Stats.divisionType == DivisionType.Tank;
                float dmgMod = terrain != null ? terrain.GetDamageModifier(isTank) : 1f;
                float moraleMod = 1f + CurrentMorale / 200f;

                // Land unit deals x5 damage to ships
                float shipPenalty = (!IsShip && other.IsShip) ? 5f : 1f;

                other.TakeDamage(damage * dmgMod * moraleMod * shipPenalty);
                _shakeTimer = 0.1f;
                break;
            }

            if (InCombat)
            {
                var config = GameManager.Instance.Config;
                ModifyMorale(-config.moraleLossInCombat * Time.deltaTime);
            }
        }

        private void ProcessLanding()
        {
            if (!IsShip || IsMoving) return;

            // Check if ship is on land
            var terrain = MapManager.Instance.GetTerrainAtWorld(transform.position);
            if (terrain == null) return;

            bool onLand = terrain.terrainType != TerrainType.Water && terrain.terrainType != TerrainType.Port;

            if (onLand && !InCombat)
            {
                if (!_isLanding)
                {
                    _isLanding = true;
                    _landingTimer = 10f;
                }

                _landingTimer -= Time.deltaTime;

                // Update HP bar to show landing progress
                if (_hpBarBg != null && _hpBarFill != null)
                {
                    float ratio = 1f - (_landingTimer / 10f);
                    var scale = _hpBarFill.transform.localScale;
                    scale.x = ratio * 0.6f;
                    _hpBarFill.transform.localScale = scale;
                    _hpBarFill.color = new Color(0.5f, 0.8f, 1f); // cyan for landing
                    _hpBarFill.transform.localPosition = new Vector3(-(1f - ratio) * 0.3f, 0.55f, 0);
                }

                if (_landingTimer <= 0)
                    ConvertToLand();
            }
            else
            {
                if (_isLanding)
                {
                    _isLanding = false;
                    _landingTimer = 0;
                    UpdateHPBar(); // Restore normal HP bar
                }
            }
        }

        private void ProcessShake()
        {
            if (_visual == null) return;

            if (_shakeTimer > 0)
            {
                _shakeTimer -= Time.deltaTime;
                float intensity = 0.02f;
                _visual.localPosition = new Vector3(
                    Random.Range(-intensity, intensity),
                    Random.Range(-intensity, intensity),
                    0
                );
            }
            else
            {
                _visual.localPosition = Vector3.zero;
            }
        }

        private void UpdatePathLine()
        {
            if (_lineRenderer == null || _path == null) return;

            int remaining = _path.Count - _pathIndex + 1;
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
            CurrentHP = Mathf.Max(0, CurrentHP - amount);
            UpdateHPBar();
            UpdateCracks();
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
            UpdateHPBar();
            UpdateCracks();
        }

        private void UpdateHPBar()
        {
            if (_hpBarFill == null || _hpBarBg == null) return;

            float ratio = CurrentHP / Stats.maxHP;

            var scale = _hpBarFill.transform.localScale;
            scale.x = ratio * 0.6f;
            _hpBarFill.transform.localScale = scale;

            _hpFillOffsetX = -(1f - ratio) * 0.3f;
            _hpBarFill.transform.localPosition = new Vector3(_hpFillOffsetX, 0.55f, 0);

            Color barColor;
            if (ratio > 0.5f)
                barColor = Color.Lerp(Color.yellow, Color.green, (ratio - 0.5f) * 2f);
            else
                barColor = Color.Lerp(Color.red, Color.yellow, ratio * 2f);
            _hpBarFill.color = barColor;
        }

        private void UpdateCracks()
        {
            if (_crackOverlay == null) return;

            float ratio = CurrentHP / Stats.maxHP;
            if (ratio > 0.6f)
            {
                _crackOverlay.enabled = false;
            }
            else if (ratio > 0.3f)
            {
                _crackOverlay.enabled = true;
                _crackOverlay.sprite = _crackLightSprite;
                _crackOverlay.color = new Color(0, 0, 0, 0.4f);
            }
            else
            {
                _crackOverlay.enabled = true;
                _crackOverlay.sprite = _crackHeavySprite;
                _crackOverlay.color = new Color(0, 0, 0, 0.6f);
            }
        }

        public void MoveToAndConvert(Vector2Int targetGrid)
        {
            _convertOnArrival = true;
            MoveTo(targetGrid);
        }

        public void SetFogVisible(bool visible)
        {
            // Hide/show enemy units in fog
            if (_visual != null) _visual.gameObject.SetActive(visible);
            if (_hpBarBg != null) _hpBarBg.enabled = visible;
            if (_hpBarFill != null) _hpBarFill.enabled = visible;
            if (_lineRenderer != null) _lineRenderer.enabled = visible;
        }

        public void ConvertToShip()
        {
            if (IsShip) return;
            IsShip = true;
            _isLanding = false;
            _landingTimer = 0;
            if (_spriteRenderer != null && _shipSprite != null)
                _spriteRenderer.sprite = _shipSprite;
            if (_outlineRenderer != null && _shipOutlineSprite != null)
                _outlineRenderer.sprite = _shipOutlineSprite;
            if (_tankDot != null) _tankDot.enabled = false;
        }

        public void ConvertToLand()
        {
            if (!IsShip) return;
            IsShip = false;
            _isLanding = false;
            _landingTimer = 0;
            if (_spriteRenderer != null && _originalSprite != null)
                _spriteRenderer.sprite = _originalSprite;
            if (_outlineRenderer != null && _originalOutlineSprite != null)
                _outlineRenderer.sprite = _originalOutlineSprite;
            if (Stats.divisionType == DivisionType.Tank && _tankDot != null)
                _tankDot.enabled = true;
        }

        private void Die()
        {
            EventBus.OnDivisionDestroyed?.Invoke(gameObject);
            Destroy(gameObject);
        }

    }
}
