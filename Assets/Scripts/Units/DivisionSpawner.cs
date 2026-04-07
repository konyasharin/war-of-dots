using UnityEngine;
using DotWars.Map;

namespace DotWars.Units
{
    public class DivisionSpawner : MonoBehaviour
    {
        public static DivisionSpawner Instance { get; private set; }

        [SerializeField] private GameObject divisionPrefab;
        [SerializeField] private DivisionStats infantryStats;
        [SerializeField] private DivisionStats tankStats;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public Division Spawn(DivisionType type, int ownerIndex, Vector2Int gridPos)
        {
            if (!MapManager.Instance.IsPassable(gridPos))
                return null;

            var stats = type == DivisionType.Infantry ? infantryStats : tankStats;
            var go = Instantiate(divisionPrefab, transform);
            var division = go.GetComponent<Division>();
            division.Initialize(stats, ownerIndex, gridPos);

            return division;
        }
    }
}
