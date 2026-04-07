using UnityEngine;
using DotWars.Core;

namespace DotWars.Map
{
    public class Port : MonoBehaviour
    {
        private SpriteRenderer _outlineRenderer;
        private int _ownerIndex = -1;

        public int OwnerIndex => _ownerIndex;

        public void Initialize()
        {
            _outlineRenderer = transform.Find("Outline")?.GetComponent<SpriteRenderer>();
            UpdateVisuals();
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
