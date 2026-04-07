using UnityEngine;
using DotWars.Core;
using DotWars.Economy;
using DotWars.Units;

namespace DotWars.Map
{
    public class Port : MonoBehaviour
    {
        private SpriteRenderer _outlineRenderer;
        private int _ownerIndex = -1;

        private Division _convertingUnit;
        private float _conversionTimer;
        private SpriteRenderer _progressBar;

        public int OwnerIndex => _ownerIndex;

        public void Initialize()
        {
            _outlineRenderer = transform.Find("Outline")?.GetComponent<SpriteRenderer>();
            UpdateVisuals();
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            // Check for units on port
            CheckForConversion();

            if (_convertingUnit != null)
            {
                // Unit moved away or destroyed
                if (_convertingUnit == null || _convertingUnit.IsMoving)
                {
                    CancelConversion();
                    return;
                }

                _conversionTimer -= Time.deltaTime;
                if (_conversionTimer <= 0)
                    CompleteConversion();
            }
        }

        private void CheckForConversion()
        {
            if (_convertingUnit != null) return;

            var colliders = Physics2D.OverlapCircleAll(transform.position, 0.8f);
            foreach (var col in colliders)
            {
                var div = col.GetComponent<Division>();
                if (div == null) continue;
                // Port usable by its owner (or anyone if neutral)
                if (_ownerIndex >= 0 && div.OwnerIndex != _ownerIndex) continue;
                if (div.IsMoving) continue;

                // Unit on friendly port — auto-convert
                if (!div.IsShip)
                {
                    StartConversion(div, false);
                    return;
                }
                else
                {
                    // Ship on port — convert back to land
                    StartConversion(div, true);
                    return;
                }
            }
        }

        private void StartConversion(Division unit, bool toLand)
        {
            if (toLand)
            {
                // Instant conversion back to land
                unit.ConvertToLand();
                return;
            }

            var config = GameManager.Instance.Config;
            if (!EconomyManager.Instance.SpendGold(unit.OwnerIndex, config.shipConversionCost))
                return;

            _convertingUnit = unit;
            _conversionTimer = config.shipConversionTime;
        }

        private void CompleteConversion()
        {
            if (_convertingUnit != null)
                _convertingUnit.ConvertToShip();
            _convertingUnit = null;
        }

        private void CancelConversion()
        {
            // Refund gold
            if (_convertingUnit != null)
            {
                var config = GameManager.Instance.Config;
                EconomyManager.Instance?.AddGold(_convertingUnit.OwnerIndex, config.shipConversionCost);
            }
            _convertingUnit = null;
            _conversionTimer = 0;
        }

        public void SetOwner(int newOwner)
        {
            _ownerIndex = newOwner;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_outlineRenderer == null) return;
            _outlineRenderer.color = _ownerIndex switch
            {
                0 => new Color(0.2f, 0.5f, 1f, 0.6f),
                1 => new Color(1f, 0.25f, 0.25f, 0.6f),
                _ => new Color(1f, 1f, 1f, 0.5f)
            };
        }
    }
}
