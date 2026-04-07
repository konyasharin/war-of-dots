using UnityEngine;
using DotWars.Core;
using DotWars.Economy;
using DotWars.Units;

namespace DotWars.Map
{
    public class Port : MonoBehaviour
    {
        private SpriteRenderer _outlineRenderer;
        private float _conversionTimer;
        private Division _convertingUnit;

        public void Initialize()
        {
            _outlineRenderer = transform.Find("Outline")?.GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            if (_convertingUnit != null)
            {
                if (_convertingUnit == null || _convertingUnit.IsMoving)
                {
                    CancelConversion();
                    return;
                }

                _conversionTimer -= Time.deltaTime;
                if (_conversionTimer <= 0)
                {
                    CompleteConversion();
                }
            }
        }

        public bool StartConversion(Division unit)
        {
            if (_convertingUnit != null) return false;

            var config = GameManager.Instance.Config;
            if (!EconomyManager.Instance.SpendGold(unit.OwnerIndex, config.shipConversionCost))
                return false;

            _convertingUnit = unit;
            _conversionTimer = config.shipConversionTime;
            return true;
        }

        private void CompleteConversion()
        {
            // TODO: Replace unit with ship
            _convertingUnit = null;
        }

        private void CancelConversion()
        {
            _convertingUnit = null;
            _conversionTimer = 0;
        }
    }
}
