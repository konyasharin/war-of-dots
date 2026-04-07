using UnityEngine;
using DotWars.Core;
using DotWars.Economy;
using DotWars.Units;

namespace DotWars.Map
{
    public class City : MonoBehaviour
    {
        private bool _isCapital;
        private int _ownerIndex;
        private Region _region;

        private SpriteRenderer _flagRenderer;
        private SpriteRenderer _outlineRenderer;

        private static readonly Color PlayerFlagColor = new(0.2f, 0.5f, 1f);
        private static readonly Color BotFlagColor = new(1f, 0.25f, 0.25f);
        private static readonly Color NeutralFlagColor = new(0.7f, 0.7f, 0.7f);

        public int OwnerIndex => _ownerIndex;
        public bool IsCapital => _isCapital;
        public Region Region { get => _region; set => _region = value; }

        public void Initialize(int owner, bool capital)
        {
            _ownerIndex = owner;
            _isCapital = capital;

            _flagRenderer = transform.Find("Flag")?.GetComponent<SpriteRenderer>();
            _outlineRenderer = transform.Find("Outline")?.GetComponent<SpriteRenderer>();

            UpdateVisuals();
        }

        private float _checkTimer;
        private const float CheckInterval = 0.2f;

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            if (GameManager.Instance.Config == null) return;

            _checkTimer += Time.deltaTime;
            if (_checkTimer < CheckInterval) return;
            _checkTimer = 0;

            // Single physics query for both income and capture
            var colliders = Physics2D.OverlapCircleAll(transform.position, 0.7f);
            bool hasFriendlyUnit = false;
            Division enemyUnit = null;

            foreach (var col in colliders)
            {
                var div = col.GetComponent<Division>();
                if (div == null) continue;
                if (div.OwnerIndex == _ownerIndex) hasFriendlyUnit = true;
                else if (enemyUnit == null) enemyUnit = div;
            }

            // Income (only with garrison)
            if (hasFriendlyUnit && _ownerIndex >= 0 && EconomyManager.Instance != null)
            {
                var config = GameManager.Instance.Config;
                float income = _isCapital ? config.capitalIncomePerSec : config.cityIncomePerSec;
                EconomyManager.Instance.AddGold(_ownerIndex, income * CheckInterval);
            }

            // Capture
            if (enemyUnit != null)
            {
                int newOwner = enemyUnit.OwnerIndex;
                if (_region != null)
                    RegionManager.Instance?.CaptureRegion(_region, newOwner);
                else
                    SetOwner(newOwner);
            }
        }

        public void SetOwner(int newOwner)
        {
            if (_ownerIndex == newOwner) return;
            _ownerIndex = newOwner;
            UpdateVisuals();
            EventBus.OnCityCaptured?.Invoke(gameObject, newOwner);
        }

        public void SetOwnerSilent(int newOwner)
        {
            _ownerIndex = newOwner;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_flagRenderer != null)
            {
                _flagRenderer.color = _ownerIndex switch
                {
                    0 => PlayerFlagColor,
                    1 => BotFlagColor,
                    _ => NeutralFlagColor
                };
            }

            if (_outlineRenderer != null)
            {
                _outlineRenderer.color = _ownerIndex switch
                {
                    0 => new Color(0.2f, 0.5f, 1f, 0.5f),
                    1 => new Color(1f, 0.25f, 0.25f, 0.5f),
                    _ => new Color(0.7f, 0.7f, 0.7f, 0.3f)
                };
            }
        }
    }
}
