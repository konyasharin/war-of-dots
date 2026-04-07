using UnityEngine;
using DotWars.Map;

namespace DotWars.Units
{
    public class DivisionSpawner : MonoBehaviour
    {
        public static DivisionSpawner Instance { get; private set; }

        private GameObject _divisionPrefab;
        private DivisionStats _infantryStats;
        private DivisionStats _tankStats;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _divisionPrefab = Resources.Load<GameObject>("Prefabs/Division");
            _infantryStats = Resources.Load<DivisionStats>("Units/InfantryStats");
            _tankStats = Resources.Load<DivisionStats>("Units/TankStats");
        }

        public Division Spawn(DivisionType type, int ownerIndex, Vector2Int gridPos)
        {
            if (!MapManager.Instance.IsPassable(gridPos))
                return null;

            if (_divisionPrefab == null) return null;

            var stats = type == DivisionType.Infantry ? _infantryStats : _tankStats;
            var go = Instantiate(_divisionPrefab, transform);
            var division = go.GetComponent<Division>();
            division.Initialize(stats, ownerIndex, gridPos);

            return division;
        }
    }
}
