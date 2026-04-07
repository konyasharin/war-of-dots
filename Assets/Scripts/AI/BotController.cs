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
        private const float ThinkInterval = 3f;
        private float _thinkTimer;
        private readonly Dictionary<Division, Vector2Int> _assignedTargets = new();
        private readonly Dictionary<Division, float> _orderCooldown = new(); // prevent constant retasking

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
            // Cleanup
            foreach (var k in _assignedTargets.Keys.Where(u => u == null).ToList())
                _assignedTargets.Remove(k);
            foreach (var k in _orderCooldown.Keys.Where(u => u == null).ToList())
                _orderCooldown.Remove(k);

            var myUnits = GetMyUnits();
            var allCities = FindObjectsByType<City>(FindObjectsSortMode.None);
            var enemyUnits = FindObjectsByType<Division>(FindObjectsSortMode.None)
                .Where(d => d.OwnerIndex != BotIndex).ToList();

            var myCapital = allCities.FirstOrDefault(c => c.OwnerIndex == BotIndex && c.IsCapital);
            var myCities = allCities.Where(c => c.OwnerIndex == BotIndex).ToList();
            var neutralCities = allCities.Where(c => c.OwnerIndex == -1).ToList();
            var enemyCities = allCities.Where(c => c.OwnerIndex == 0).ToList();

            // Mark garrison units (locked to city, never reassigned)
            var garrisonUnits = new HashSet<Division>();
            foreach (var city in myCities)
            {
                var cGrid = MapManager.Instance.WorldToGrid(city.transform.position);
                Division bestGarrison = null;
                float bestDist = float.MaxValue;
                foreach (var u in myUnits)
                {
                    if (garrisonUnits.Contains(u)) continue;
                    var uGrid = MapManager.Instance.WorldToGrid(u.transform.position);
                    float dist = Vector2Int.Distance(uGrid, cGrid);
                    if (dist < bestDist) { bestDist = dist; bestGarrison = u; }
                }
                if (bestGarrison != null && bestDist < 3f)
                {
                    garrisonUnits.Add(bestGarrison);
                    // If not close enough, send to city
                    if (bestDist > 1.5f && !bestGarrison.IsMoving)
                        GiveOrder(bestGarrison, cGrid);
                }
            }

            // Buy units
            TryBuyUnits(myCapital, myUnits.Count, enemyUnits.Count);

            // Available units = not garrison, not in combat, not on cooldown
            var available = myUnits
                .Where(u => !garrisonUnits.Contains(u) && !u.InCombat && !IsOnCooldown(u))
                .ToList();

            // Defend: if enemy near any of our cities, intercept
            foreach (var city in myCities)
            {
                var cGrid = MapManager.Instance.WorldToGrid(city.transform.position);
                var threat = enemyUnits
                    .Where(e => Vector2Int.Distance(MapManager.Instance.WorldToGrid(e.transform.position), cGrid) < 15)
                    .OrderBy(e => Vector2Int.Distance(MapManager.Instance.WorldToGrid(e.transform.position), cGrid))
                    .FirstOrDefault();

                if (threat != null)
                {
                    var defenders = available.Where(u => !u.IsMoving)
                        .OrderBy(u => Vector2Int.Distance(MapManager.Instance.WorldToGrid(u.transform.position), cGrid))
                        .Take(2).ToList();

                    foreach (var d in defenders)
                    {
                        GiveOrder(d, MapManager.Instance.WorldToGrid(threat.transform.position));
                        available.Remove(d);
                    }
                }
            }

            // Attack: group idle available units
            var idle = available.Where(u => !u.IsMoving).ToList();
            if (idle.Count >= 2)
            {
                City target = PickBestTargetCity(neutralCities, enemyCities, myCapital);
                if (target != null)
                {
                    var targetGrid = MapManager.Instance.WorldToGrid(target.transform.position);
                    int toSend = Mathf.Min(idle.Count, 3);
                    idle.Sort((a, b) =>
                    {
                        var ag = MapManager.Instance.WorldToGrid(a.transform.position);
                        var bg = MapManager.Instance.WorldToGrid(b.transform.position);
                        return Vector2Int.Distance(ag, targetGrid).CompareTo(Vector2Int.Distance(bg, targetGrid));
                    });

                    for (int i = 0; i < toSend; i++)
                        GiveOrder(idle[i], CalculateFormationAround(targetGrid, i, toSend));
                }
            }

            // Retreat wounded
            foreach (var unit in myUnits)
            {
                if (garrisonUnits.Contains(unit)) continue;
                if (unit.CurrentHP < unit.Stats.maxHP * 0.25f && !unit.IsMoving && myCapital != null)
                    GiveOrder(unit, MapManager.Instance.WorldToGrid(myCapital.transform.position));
            }
        }

        private void GiveOrder(Division unit, Vector2Int target)
        {
            unit.MoveTo(target);
            _assignedTargets[unit] = target;
            _orderCooldown[unit] = Time.time + 8f; // 8 sec cooldown before new order
        }

        private bool IsOnCooldown(Division unit)
        {
            return _orderCooldown.TryGetValue(unit, out float until) && Time.time < until;
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
