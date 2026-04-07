using UnityEngine;
using DotWars.Core;
using DotWars.Economy;
using DotWars.Units;

namespace DotWars.Map
{
    public class City : MonoBehaviour
    {
        [SerializeField] private bool isCapital;
        [SerializeField] private int ownerIndex; // 0=player, 1=bot, -1=neutral

        private SpriteRenderer _flagRenderer;
        private SpriteRenderer _outlineRenderer;

        private static readonly Color PlayerFlagColor = new(0.2f, 0.5f, 1f);
        private static readonly Color BotFlagColor = new(1f, 0.25f, 0.25f);
        private static readonly Color NeutralFlagColor = new(0.7f, 0.7f, 0.7f);

        public int OwnerIndex => ownerIndex;
        public bool IsCapital => isCapital;

        public void Initialize(int owner, bool capital)
        {
            ownerIndex = owner;
            isCapital = capital;

            _flagRenderer = transform.Find("Flag")?.GetComponent<SpriteRenderer>();
            _outlineRenderer = transform.Find("Outline")?.GetComponent<SpriteRenderer>();

            UpdateVisuals();
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            // Generate income
            var config = GameManager.Instance.Config;
            float income = isCapital ? config.capitalIncomePerSec : config.cityIncomePerSec;

            if (ownerIndex >= 0 && EconomyManager.Instance != null)
                EconomyManager.Instance.AddGold(ownerIndex, income * Time.deltaTime);

            // Check if a unit is capturing
            CheckCapture();
        }

        private void CheckCapture()
        {
            var colliders = Physics2D.OverlapCircleAll(transform.position, 0.5f);
            foreach (var col in colliders)
            {
                var div = col.GetComponent<Division>();
                if (div == null || div.OwnerIndex == ownerIndex) continue;

                // Enemy unit on city — capture instantly for now (capture timer later)
                SetOwner(div.OwnerIndex);
                break;
            }
        }

        public void SetOwner(int newOwner)
        {
            if (ownerIndex == newOwner) return;
            ownerIndex = newOwner;
            UpdateVisuals();
            EventBus.OnCityCaptured?.Invoke(gameObject, newOwner);
        }

        private void UpdateVisuals()
        {
            if (_flagRenderer != null)
            {
                _flagRenderer.color = ownerIndex switch
                {
                    0 => PlayerFlagColor,
                    1 => BotFlagColor,
                    _ => NeutralFlagColor
                };
            }

            if (_outlineRenderer != null)
            {
                _outlineRenderer.color = ownerIndex switch
                {
                    0 => new Color(0.2f, 0.5f, 1f, 0.5f),
                    1 => new Color(1f, 0.25f, 0.25f, 0.5f),
                    _ => new Color(0.7f, 0.7f, 0.7f, 0.3f)
                };
            }
        }
    }
}
