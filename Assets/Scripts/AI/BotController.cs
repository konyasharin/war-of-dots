using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DotWars.Core;
using DotWars.Map;
using DotWars.Units;
using DotWars.Economy;

namespace DotWars.AI
{
    public class BotController : MonoBehaviour
    {
        public static BotController Instance { get; private set; }

        private const int BotIndex = 1;
        private const float ThinkInterval = 2f;
        private float _thinkTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            _thinkTimer += Time.deltaTime;
            if (_thinkTimer >= ThinkInterval)
            {
                _thinkTimer = 0;
                Think();
            }
        }

        private void Think()
        {
            var myUnits = GetMyUnits();
            var allCities = FindObjectsByType<City>(FindObjectsSortMode.None);
            var myCapital = allCities.FirstOrDefault(c => c.OwnerIndex == BotIndex && c.IsCapital);

            // 1. Buy units if affordable
            TryBuyUnits(myCapital);

            // 2. Assign tasks to idle units
            foreach (var unit in myUnits)
            {
                if (unit.IsMoving || unit.InCombat) continue;

                var action = EvaluateBestAction(unit, allCities, myCapital);
                if (action.HasValue)
                    unit.MoveTo(action.Value);
            }
        }

        private void TryBuyUnits(City capital)
        {
            if (capital == null || EconomyManager.Instance == null) return;
            if (DivisionSpawner.Instance == null) return;

            var gold = EconomyManager.Instance.Gold[BotIndex];
            var capitalGrid = MapManager.Instance.WorldToGrid(capital.transform.position);

            // Prefer tanks if rich, otherwise infantry
            if (gold >= 200 && Random.value > 0.6f)
            {
                if (EconomyManager.Instance.SpendGold(BotIndex, 200))
                {
                    var spawnPos = FindFreeAdjacentTile(capitalGrid);
                    DivisionSpawner.Instance.Spawn(DivisionType.Tank, BotIndex, spawnPos);
                }
            }
            else if (gold >= 100)
            {
                if (EconomyManager.Instance.SpendGold(BotIndex, 100))
                {
                    var spawnPos = FindFreeAdjacentTile(capitalGrid);
                    DivisionSpawner.Instance.Spawn(DivisionType.Infantry, BotIndex, spawnPos);
                }
            }
        }

        private Vector2Int? EvaluateBestAction(Division unit, City[] allCities, City myCapital)
        {
            var unitGrid = MapManager.Instance.WorldToGrid(unit.transform.position);
            float bestScore = -1;
            Vector2Int? bestTarget = null;

            // Check if unit is in a city generating income — stay put
            foreach (var c in allCities)
            {
                if (c.OwnerIndex != BotIndex) continue;
                var cityGrid = MapManager.Instance.WorldToGrid(c.transform.position);
                if (Vector2Int.Distance(unitGrid, cityGrid) < 1.5f)
                {
                    // Already in own city — check if only garrison
                    if (!HasOtherFriendlyUnitNear(cityGrid, unit))
                        return null; // Stay as garrison
                }
            }

            // Defend capital if enemy nearby
            if (myCapital != null)
            {
                var capGrid = MapManager.Instance.WorldToGrid(myCapital.transform.position);
                if (IsEnemyNear(capGrid, 15))
                {
                    float dist = Vector2Int.Distance(unitGrid, capGrid);
                    float score = 100f / (dist + 1);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTarget = capGrid;
                    }
                }
            }

            // Capture neutral cities
            foreach (var c in allCities)
            {
                if (c.OwnerIndex == BotIndex) continue;
                var cityGrid = MapManager.Instance.WorldToGrid(c.transform.position);
                float dist = Vector2Int.Distance(unitGrid, cityGrid);

                float score;
                if (c.OwnerIndex == -1) // Neutral
                    score = 50f / (dist + 1);
                else // Enemy
                    score = 30f / (dist + 1);

                if (c.IsCapital && c.OwnerIndex == 0)
                    score *= 1.5f; // High priority: enemy capital

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = cityGrid;
                }
            }

            // Retreat if low HP
            if (unit.CurrentHP < unit.Stats.maxHP * 0.3f && myCapital != null)
            {
                var capGrid = MapManager.Instance.WorldToGrid(myCapital.transform.position);
                bestTarget = capGrid;
            }

            return bestTarget;
        }

        private bool IsEnemyNear(Vector2Int pos, float radius)
        {
            var divisions = FindObjectsByType<Division>(FindObjectsSortMode.None);
            foreach (var d in divisions)
            {
                if (d.OwnerIndex == BotIndex) continue;
                var dGrid = MapManager.Instance.WorldToGrid(d.transform.position);
                if (Vector2Int.Distance(pos, dGrid) < radius)
                    return true;
            }
            return false;
        }

        private bool HasOtherFriendlyUnitNear(Vector2Int pos, Division exclude)
        {
            var divisions = FindObjectsByType<Division>(FindObjectsSortMode.None);
            foreach (var d in divisions)
            {
                if (d == exclude || d.OwnerIndex != BotIndex) continue;
                var dGrid = MapManager.Instance.WorldToGrid(d.transform.position);
                if (Vector2Int.Distance(pos, dGrid) < 2f)
                    return true;
            }
            return false;
        }

        private Vector2Int FindFreeAdjacentTile(Vector2Int center)
        {
            Vector2Int[] offsets = {
                new(0, 1), new(0, -1), new(1, 0), new(-1, 0),
                new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
            };
            foreach (var off in offsets)
            {
                var pos = center + off;
                if (MapManager.Instance.IsPassable(pos))
                    return pos;
            }
            return center;
        }

        private List<Division> GetMyUnits()
        {
            return FindObjectsByType<Division>(FindObjectsSortMode.None)
                .Where(d => d.OwnerIndex == BotIndex)
                .ToList();
        }
    }
}
