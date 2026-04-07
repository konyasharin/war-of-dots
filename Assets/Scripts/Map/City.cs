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

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            // Income ONLY when a friendly unit is present
            bool hasFriendlyUnit = false;
            var colliders = Physics2D.OverlapCircleAll(transform.position, 0.7f);
            foreach (var col in colliders)
            {
                var div = col.GetComponent<Division>();
                if (div != null && div.OwnerIndex == _ownerIndex)
                {
                    hasFriendlyUnit = true;
                    break;
                }
            }

            if (hasFriendlyUnit && _ownerIndex >= 0 && EconomyManager.Instance != null)
            {
                var config = GameManager.Instance.Config;
                float income = _isCapital ? config.capitalIncomePerSec : config.cityIncomePerSec;
                EconomyManager.Instance.AddGold(_ownerIndex, income * Time.deltaTime);
            }

            CheckCapture();
        }

        private void CheckCapture()
        {
            var colliders = Physics2D.OverlapCircleAll(transform.position, 0.5f);
            foreach (var col in colliders)
            {
                var div = col.GetComponent<Division>();
                if (div == null || div.OwnerIndex == _ownerIndex) continue;

                // Capture city + entire region
                int newOwner = div.OwnerIndex;
                if (_region != null)
                    RegionManager.Instance?.CaptureRegion(_region, newOwner);
                else
                    SetOwner(newOwner);
                break;
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
