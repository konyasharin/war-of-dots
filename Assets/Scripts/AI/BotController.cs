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
        private const float ThinkInterval = 2.5f;
        private float _thinkTimer;
        private readonly Dictionary<Division, Vector2Int> _assignedTargets = new();

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
            // Clean up destroyed units from assignments
            var dead = _assignedTargets.Keys.Where(u => u == null).ToList();
            foreach (var d in dead) _assignedTargets.Remove(d);

            var myUnits = GetMyUnits();
            var allCities = FindObjectsByType<City>(FindObjectsSortMode.None);
            var enemyUnits = FindObjectsByType<Division>(FindObjectsSortMode.None)
                .Where(d => d.OwnerIndex != BotIndex).ToList();

            var myCapital = allCities.FirstOrDefault(c => c.OwnerIndex == BotIndex && c.IsCapital);
            var myCities = allCities.Where(c => c.OwnerIndex == BotIndex).ToList();
            var neutralCities = allCities.Where(c => c.OwnerIndex == -1).ToList();
            var enemyCities = allCities.Where(c => c.OwnerIndex == 0).ToList();

            // Phase 1: Buy units smartly
            TryBuyUnits(myCapital, myUnits.Count, enemyUnits.Count);

            // Phase 2: Ensure garrisons (1 unit per city for income)
            var idleUnits = myUnits.Where(u => !u.IsMoving && !u.InCombat).ToList();
            EnsureGarrisons(myCities, idleUnits);

            // Phase 3: Defend capital
            idleUnits = myUnits.Where(u => !u.IsMoving && !u.InCombat && !IsGarrison(u, myCities)).ToList();
            DefendCapital(myCapital, idleUnits, enemyUnits);

            // Phase 4: Group attack — send multiple units to same target
            idleUnits = myUnits.Where(u => !u.IsMoving && !u.InCombat && !IsGarrison(u, myCities)).ToList();
            if (idleUnits.Count >= 2)
            {
                // Prefer neutral cities first, then enemy
                City target = PickBestTargetCity(neutralCities, enemyCities, myCapital);
                if (target != null)
                {
                    var targetGrid = MapManager.Instance.WorldToGrid(target.transform.position);
                    int toSend = Mathf.Min(idleUnits.Count, 3);
                    // Sort by distance, send closest
                    idleUnits.Sort((a, b) =>
                    {
                        var ag = MapManager.Instance.WorldToGrid(a.transform.position);
                        var bg = MapManager.Instance.WorldToGrid(b.transform.position);
                        return Vector2Int.Distance(ag, targetGrid).CompareTo(Vector2Int.Distance(bg, targetGrid));
                    });

                    for (int i = 0; i < toSend; i++)
                    {
                        var formation = CalculateFormationAround(targetGrid, i, toSend);
                        idleUnits[i].MoveTo(formation);
                        _assignedTargets[idleUnits[i]] = targetGrid;
                    }
                }
            }
            else if (idleUnits.Count == 1)
            {
                // Single unit — only go to neutral cities (safer)
                var target = neutralCities
                    .OrderBy(c => Vector2Int.Distance(
                        MapManager.Instance.WorldToGrid(idleUnits[0].transform.position),
                        MapManager.Instance.WorldToGrid(c.transform.position)))
                    .FirstOrDefault();

                if (target != null)
                {
                    idleUnits[0].MoveTo(MapManager.Instance.WorldToGrid(target.transform.position));
                }
            }

            // Phase 5: Retreat wounded units
            foreach (var unit in myUnits)
            {
                if (unit.CurrentHP < unit.Stats.maxHP * 0.25f && !unit.IsMoving && myCapital != null)
                {
                    unit.MoveTo(MapManager.Instance.WorldToGrid(myCapital.transform.position));
                }
            }
        }

        private void TryBuyUnits(City capital, int myCount, int enemyCount)
        {
            if (capital == null || EconomyManager.Instance == null || DivisionSpawner.Instance == null) return;

            float gold = EconomyManager.Instance.Gold[BotIndex];
            var capitalGrid = MapManager.Instance.WorldToGrid(capital.transform.position);

            // Don't overspend — save some gold, buy more if outnumbered
            float minGold = myCount < enemyCount ? 50 : 150;
            if (gold < 100 + minGold) return;

            // Buy tank if rich and army is big enough
            if (gold >= 350 && myCount >= 3 && Random.value > 0.5f)
            {
                if (EconomyManager.Instance.SpendGold(BotIndex, 200))
                {
                    DivisionSpawner.Instance.Spawn(DivisionType.Tank, BotIndex, FindFreeAdjacentTile(capitalGrid));
                }
            }
            else if (gold >= 100 + minGold)
            {
                if (EconomyManager.Instance.SpendGold(BotIndex, 100))
                {
                    DivisionSpawner.Instance.Spawn(DivisionType.Infantry, BotIndex, FindFreeAdjacentTile(capitalGrid));
                }
            }
        }

        private void EnsureGarrisons(List<City> myCities, List<Division> idleUnits)
        {
            foreach (var city in myCities)
            {
                var cityGrid = MapManager.Instance.WorldToGrid(city.transform.position);
                bool hasGarrison = false;

                var all = FindObjectsByType<Division>(FindObjectsSortMode.None);
                foreach (var d in all)
                {
                    if (d.OwnerIndex != BotIndex) continue;
                    var dGrid = MapManager.Instance.WorldToGrid(d.transform.position);
                    if (Vector2Int.Distance(dGrid, cityGrid) < 1.5f)
                    {
                        hasGarrison = true;
                        break;
                    }
                }

                if (!hasGarrison && idleUnits.Count > 0)
                {
                    // Find closest idle unit
                    Division closest = null;
                    float bestDist = float.MaxValue;
                    foreach (var u in idleUnits)
                    {
                        var uGrid = MapManager.Instance.WorldToGrid(u.transform.position);
                        float dist = Vector2Int.Distance(uGrid, cityGrid);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            closest = u;
                        }
                    }

                    if (closest != null)
                    {
                        closest.MoveTo(cityGrid);
                        idleUnits.Remove(closest);
                    }
                }
            }
        }

        private void DefendCapital(City capital, List<Division> idleUnits, List<Division> enemyUnits)
        {
            if (capital == null || idleUnits.Count == 0) return;

            var capGrid = MapManager.Instance.WorldToGrid(capital.transform.position);
            var nearbyEnemies = enemyUnits
                .Where(e => Vector2Int.Distance(MapManager.Instance.WorldToGrid(e.transform.position), capGrid) < 20)
                .ToList();

            if (nearbyEnemies.Count == 0) return;

            // Send all available idle units to defend
            int defenders = Mathf.Min(idleUnits.Count, nearbyEnemies.Count + 1);
            for (int i = 0; i < defenders; i++)
            {
                var enemyGrid = MapManager.Instance.WorldToGrid(nearbyEnemies[i % nearbyEnemies.Count].transform.position);
                idleUnits[i].MoveTo(enemyGrid);
            }

            for (int i = 0; i < defenders; i++)
                idleUnits.RemoveAt(0);
        }

        private City PickBestTargetCity(List<City> neutral, List<City> enemy, City myCapital)
        {
            // Prefer closest neutral, then closest enemy non-capital, then enemy capital
            var capGrid = myCapital != null ? MapManager.Instance.WorldToGrid(myCapital.transform.position) : Vector2Int.zero;

            City best = null;
            float bestScore = float.MaxValue;

            foreach (var c in neutral)
            {
                float dist = Vector2Int.Distance(capGrid, MapManager.Instance.WorldToGrid(c.transform.position));
                if (dist < bestScore) { bestScore = dist; best = c; }
            }

            if (best != null) return best;

            bestScore = float.MaxValue;
            foreach (var c in enemy)
            {
                float dist = Vector2Int.Distance(capGrid, MapManager.Instance.WorldToGrid(c.transform.position));
                float weight = c.IsCapital ? 1.5f : 1f; // capitals slightly less priority (harder)
                if (dist * weight < bestScore) { bestScore = dist * weight; best = c; }
            }

            return best;
        }

        private bool IsGarrison(Division unit, List<City> myCities)
        {
            var uGrid = MapManager.Instance.WorldToGrid(unit.transform.position);
            foreach (var c in myCities)
            {
                var cGrid = MapManager.Instance.WorldToGrid(c.transform.position);
                if (Vector2Int.Distance(uGrid, cGrid) < 1.5f)
                {
                    // Only consider it garrison if it's the ONLY unit there
                    int count = 0;
                    var all = FindObjectsByType<Division>(FindObjectsSortMode.None);
                    foreach (var d in all)
                    {
                        if (d.OwnerIndex != BotIndex) continue;
                        if (Vector2Int.Distance(MapManager.Instance.WorldToGrid(d.transform.position), cGrid) < 1.5f)
                            count++;
                    }
                    return count <= 1;
                }
            }
            return false;
        }

        private Vector2Int CalculateFormationAround(Vector2Int center, int index, int total)
        {
            if (index == 0) return center;
            Vector2Int[] offsets = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1), new(1, 1), new(-1, -1) };
            var off = offsets[(index - 1) % offsets.Length];
            var pos = center + off;
            return MapManager.Instance.IsPassable(pos) ? pos : center;
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
                if (MapManager.Instance.IsPassable(pos)) return pos;
            }
            return center;
        }

        private List<Division> GetMyUnits()
        {
            return FindObjectsByType<Division>(FindObjectsSortMode.None)
                .Where(d => d.OwnerIndex == BotIndex).ToList();
        }
    }
}
