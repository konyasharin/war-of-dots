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
        private readonly Dictionary<Division, float> _orderCooldown = new();
        private List<Vector2Int> _cachedFrontline = new();
        private float _frontlineCacheTime;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            _thinkTimer += Time.deltaTime;
            if (_thinkTimer >= ThinkInterval) { _thinkTimer = 0; Think(); }
        }

        private void Think()
        {
            // Cleanup destroyed
            foreach (var k in _orderCooldown.Keys.Where(u => u == null).ToList())
                _orderCooldown.Remove(k);

            var map = MapManager.Instance;
            var rm = RegionManager.Instance;
            var myUnits = GetUnits(BotIndex);
            var enemyUnits = GetUnits(0);
            var allCities = FindObjectsByType<City>(FindObjectsSortMode.None);
            var myCities = allCities.Where(c => c.OwnerIndex == BotIndex).ToList();
            var myCapital = myCities.FirstOrDefault(c => c.IsCapital);

            // Refresh frontline every 6s
            if (Time.time - _frontlineCacheTime > 6f && rm != null)
            {
                _cachedFrontline = rm.GetFrontlineTiles(BotIndex);
                // Thin out — keep every 3rd tile for performance
                if (_cachedFrontline.Count > 30)
                    _cachedFrontline = _cachedFrontline.Where((_, i) => i % 3 == 0).ToList();
                _frontlineCacheTime = Time.time;
            }

            // === Phase 1: Garrisons ===
            var garrisonUnits = new HashSet<Division>();
            foreach (var city in myCities)
            {
                var cGrid = map.WorldToGrid(city.transform.position);
                var closest = myUnits
                    .Where(u => !garrisonUnits.Contains(u))
                    .OrderBy(u => Vector2Int.Distance(map.WorldToGrid(u.transform.position), cGrid))
                    .FirstOrDefault();

                if (closest != null && Vector2Int.Distance(map.WorldToGrid(closest.transform.position), cGrid) < 5f)
                {
                    garrisonUnits.Add(closest);
                    if (Vector2Int.Distance(map.WorldToGrid(closest.transform.position), cGrid) > 1.5f && !closest.IsMoving)
                        GiveOrder(closest, cGrid);
                }
            }

            // === Phase 2: Buy units ===
            TryBuyUnits(myCapital, myUnits.Count, enemyUnits.Count);

            // Available = not garrison, not combat, not cooldown
            var available = myUnits.Where(u => !garrisonUnits.Contains(u) && !u.InCombat && !IsOnCooldown(u)).ToList();

            // === Phase 3: Analyze front sectors ===
            // Split frontline into 3 sectors by Y
            float mapMidY = map.Bounds.yMin + map.Height * 0.5f;
            float sectorH = map.Height / 3f;
            float[] sectorMinY = { map.Bounds.yMin, map.Bounds.yMin + sectorH, map.Bounds.yMin + sectorH * 2 };

            int[] myStrength = new int[3];
            int[] enemyStrength = new int[3];

            foreach (var u in myUnits)
            {
                if (garrisonUnits.Contains(u)) continue;
                int sector = GetSector(map.WorldToGrid(u.transform.position).y, sectorMinY, sectorH);
                myStrength[sector]++;
            }
            foreach (var u in enemyUnits)
            {
                int sector = GetSector(map.WorldToGrid(u.transform.position).y, sectorMinY, sectorH);
                enemyStrength[sector]++;
            }

            // === Phase 4: Reinforce weak sectors ===
            var idleAvailable = available.Where(u => !u.IsMoving).ToList();
            for (int s = 0; s < 3; s++)
            {
                if (enemyStrength[s] > myStrength[s] && idleAvailable.Count > 0)
                {
                    // Find frontline positions in this sector
                    var sectorFront = _cachedFrontline
                        .Where(t => GetSector(t.y, sectorMinY, sectorH) == s)
                        .ToList();

                    if (sectorFront.Count == 0) continue;

                    int toSend = Mathf.Min(enemyStrength[s] - myStrength[s] + 1, idleAvailable.Count, 3);
                    for (int i = 0; i < toSend; i++)
                    {
                        var pos = sectorFront[Random.Range(0, sectorFront.Count)];
                        var unit = idleAvailable
                            .OrderBy(u => Vector2Int.Distance(map.WorldToGrid(u.transform.position), pos))
                            .First();
                        GiveOrder(unit, pos);
                        idleAvailable.Remove(unit);
                    }
                }
            }

            // === Phase 5: Attack if strong sector (2:1 advantage) ===
            for (int s = 0; s < 3; s++)
            {
                if (myStrength[s] >= 2 && myStrength[s] >= enemyStrength[s] * 2)
                {
                    // Find closest enemy/neutral city in this sector
                    var targetCity = allCities
                        .Where(c => c.OwnerIndex != BotIndex)
                        .Where(c => GetSector(map.WorldToGrid(c.transform.position).y, sectorMinY, sectorH) == s)
                        .OrderBy(c => c.OwnerIndex == -1 ? 0 : 1) // neutral first
                        .FirstOrDefault();

                    if (targetCity == null) continue;

                    var targetGrid = map.WorldToGrid(targetCity.transform.position);
                    var attackers = idleAvailable
                        .Where(u => GetSector(map.WorldToGrid(u.transform.position).y, sectorMinY, sectorH) == s)
                        .OrderBy(u => Vector2Int.Distance(map.WorldToGrid(u.transform.position), targetGrid))
                        .Take(3).ToList();

                    for (int i = 0; i < attackers.Count; i++)
                    {
                        GiveOrder(attackers[i], CalculateFormation(targetGrid, i, attackers.Count));
                        idleAvailable.Remove(attackers[i]);
                    }
                }
            }

            // === Phase 6: Distribute remaining idle to frontline ===
            idleAvailable = idleAvailable.Where(u => !u.IsMoving).ToList();
            if (_cachedFrontline.Count > 0 && idleAvailable.Count > 0)
            {
                foreach (var unit in idleAvailable.Take(3))
                {
                    var uGrid = map.WorldToGrid(unit.transform.position);
                    // Find a frontline position not too close to other units
                    var bestPos = _cachedFrontline
                        .OrderBy(t => Vector2Int.Distance(uGrid, t))
                        .FirstOrDefault(t =>
                        {
                            foreach (var other in myUnits)
                            {
                                if (other == unit || garrisonUnits.Contains(other)) continue;
                                if (Vector2Int.Distance(map.WorldToGrid(other.transform.position), t) < 3)
                                    return false;
                            }
                            return true;
                        });

                    if (bestPos != default)
                        GiveOrder(unit, bestPos);
                }
            }

            // === Phase 7: Retreat wounded ===
            foreach (var unit in myUnits)
            {
                if (garrisonUnits.Contains(unit)) continue;
                if (unit.CurrentHP < unit.Stats.maxHP * 0.2f && !unit.IsMoving && myCapital != null)
                    GiveOrder(unit, map.WorldToGrid(myCapital.transform.position));
            }
        }

        private int GetSector(float y, float[] sectorMinY, float sectorH)
        {
            for (int i = 2; i >= 0; i--)
                if (y >= sectorMinY[i]) return i;
            return 0;
        }

        private void GiveOrder(Division unit, Vector2Int target)
        {
            unit.MoveTo(target);
            _orderCooldown[unit] = Time.time + 10f;
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

            float minGold = myCount < enemyCount ? 50 : 150;
            if (gold < 100 + minGold) return;

            if (gold >= 350 && myCount >= 4 && Random.value > 0.5f)
            {
                if (EconomyManager.Instance.SpendGold(BotIndex, 200))
                    DivisionSpawner.Instance.Spawn(DivisionType.Tank, BotIndex, FindFreeAdjacentTile(capitalGrid));
            }
            else if (gold >= 100 + minGold)
            {
                if (EconomyManager.Instance.SpendGold(BotIndex, 100))
                    DivisionSpawner.Instance.Spawn(DivisionType.Infantry, BotIndex, FindFreeAdjacentTile(capitalGrid));
            }
        }

        private Vector2Int CalculateFormation(Vector2Int center, int index, int total)
        {
            if (index == 0) return center;
            Vector2Int[] offsets = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1), new(1, 1), new(-1, -1) };
            var off = offsets[(index - 1) % offsets.Length];
            var pos = center + off;
            return MapManager.Instance.IsPassable(pos) ? pos : center;
        }

        private Vector2Int FindFreeAdjacentTile(Vector2Int center)
        {
            Vector2Int[] offsets = { new(0, 1), new(0, -1), new(1, 0), new(-1, 0), new(1, 1), new(1, -1), new(-1, 1), new(-1, -1) };
            foreach (var off in offsets)
            {
                var pos = center + off;
                if (MapManager.Instance.IsPassable(pos)) return pos;
            }
            return center;
        }

        private List<Division> GetUnits(int owner)
        {
            return FindObjectsByType<Division>(FindObjectsSortMode.None)
                .Where(d => d.OwnerIndex == owner).ToList();
        }
    }
}
