# Dot Wars

Solo PvE real-time стратегия (2D, вид сверху). Игрок (синий) vs 1 бот (красный).
Дивизии — цветные кружки с чёрной обводкой, карта — tilemap с рельефом.

## Tech Stack

- **Движок:** Unity 6 (6000.3.10f1), Universal 2D (URP)
- **Язык:** C#
- **GitHub:** https://github.com/konyasharin/war-of-dots.git

## Setup

1. Меню **DotWars > Setup All** (создаёт все ассеты, тайлы, префаб, сцену)
2. Открыть **Assets/Scenes/Game.unity**
3. Play

## Структура Assets/

```
Scripts/
  Core/           GameManager, EventBus, GameConfig (SO), GameSetup (bootstrap)
  Map/            MapManager, Pathfinding (A*), TerrainType, TerrainConfig
                  City, Port, Region, RegionManager, FogOfWar
  Units/          Division, DivisionSpawner, SelectionManager, DivisionType, DivisionStats
  Economy/        EconomyManager
  AI/             BotController (фронтовой Utility AI)
  UI/             EconomyHUD, ShopPanel, PortPanel, TimeControlHUD
  Camera/         CameraController
Editor/
  SetupWizard.cs  Меню DotWars > Setup All — генерация ассетов и сцены
Resources/
  Terrain/        TerrainConfig SO (Plain, Forest, Mountain, Water, Bridge, Port, City)
  Units/          InfantryStats, TankStats
  Prefabs/        Division.prefab
  Sprites/        CrackLight, CrackHeavy, Ship, ShipOutline, Flag, Ring
  Tiles/          Fog, Overlay, Border
  GameConfig.asset
```

## Архитектура

### Паттерны
- **Singletons:** GameManager (DontDestroyOnLoad), MapManager, EconomyManager, RegionManager, FogOfWar, BotController, SelectionManager, DivisionSpawner
- **Resources.Load:** все SO и префабы загружаются из Resources/ (не serialized refs в сцене — те ломаются при программном создании сцены)
- **EventBus:** статический класс с C# Action events (OnGameStateChanged, OnDivisionSpawned/Destroyed, OnGoldChanged, OnCityCaptured)
- **OnGUI:** все UI панели рисуются через IMGUI (не Canvas/UGUI)

### Namespaces
`DotWars.Core`, `DotWars.Map`, `DotWars.Units`, `DotWars.Economy`, `DotWars.UI`, `DotWars.AI`, `DotWars.CameraSystem`

### Ключевые системы

**MapManager** — Singleton, строит grid из Tilemap. TerrainConfig загружается из Resources по имени тайла (Voronoi не нужен — имя тайла = тип). Карта 100x60.

**Pathfinding** — A* с 8 направлениями. Стоимость = 1/speedModifier. Корабли ходят по воде + береговые тайлы (HasAdjacentWater). `FindPath(start, end, isInfantry, isShip)`.

**Division** — MonoBehaviour на каждом юните:
- Kinematic Rigidbody2D + MovePosition (не Dynamic, чтобы юниты не двигали друг друга)
- Steering: перед шагом OverlapCircle → при блокировке руление перпендикулярно
- Stuck detection: если не двигался 0.8с → nudge вбок
- Visual: дочерний контейнер (для shake при бое), HP bar и crack overlay отдельно
- Корабль: swap спрайта + outline, IsShip флаг, плавание по воде
- Высадка: корабль на суше → 10с таймер → ConvertToLand
- Бой: OverlapCircle 0.6 → урон * 0.125 (длинные бои ~80с). Корабль не бьёт наземных, наземные бьют корабль x5

**SelectionManager** — ЛКМ клик/рамка выделения, Shift мульти. ПКМ = движение с формацией (спираль). Ctrl+ПКМ drag = линейное построение (scroll = расстояние).

**RegionManager** — Voronoi регионы (каждый тайл → ближайший город). Overlay + Border tilemap. Захват города = захват всего региона с постройками. `GetFrontlineTiles(ownerIndex)` для AI.

**FogOfWar** — bool[100,60] видимость. Обновление каждые 0.3с. Источники: юниты (r=8), города (r=5), свои регионы (полностью). F2 = dev toggle. Вражеские юниты скрыты через SetFogVisible.

**EconomyManager** — float[] Gold для 2 игроков. Доход только когда юнит в городе. City.Update каждые 0.2с (1 OverlapCircle на income+capture).

**BotController** — Фронтовой Utility AI, думает каждые 3с:
1. Гарнизон (1 юнит/город, заблокирован)
2. 3 сектора фронта (верх/центр/низ) — подсчёт сил
3. Подкрепление слабых секторов
4. Атака при перевесе 2:1 (группа до 3)
5. Распределение по линии фронта
6. Отступление раненых (<20% HP)
7. Покупка юнитов с экономией

**CameraController** — WASD/стрелки, edge pan, MMB drag, scroll zoom (3-30). Bounds привязаны к карте (WorldMin/WorldMax + margin). BlockZoom при Ctrl+ПКМ.

**TimeControlHUD** — Скорости: Пауза/1x/2x/4x/8x. Space = toggle пауза, 1-4 клавиши. Игровое время Day X HH:MM (1 real sec = 1 game min at 1x).

### UI панели
- **ShopPanel** — клик по своему городу, левая панель 20% экрана, slide анимация. Покупка пехоты/танков.
- **PortPanel** — клик по своему порту, чекбоксы юнитов для погрузки на корабль.
- **EconomyHUD** — правый нижний: золото игрока + приблизительное вражеское.

## Ключевые параметры

| Параметр | Значение |
|---|---|
| Стартовое золото | 300$ |
| Доход города | 1$/сек (при гарнизоне) |
| Доход столицы | 2$/сек (при гарнизоне) |
| Пехота | HP 100, урон 1.25/сек*, скорость 0.75, $100 |
| Танк | HP 200, урон 2.5/сек*, скорость 0.75, $200 |
| Корабль | скорость x3, не бьёт наземных, получает x5 урон от наземных |
| Высадка | 10 сек на берегу |
| Модификаторы | Лес: скорость 70% (пехота 85%), танк урон 70% |
| Горы | Непроходимы |
| Победа | Захват столицы + ≥80% территории |

*урон после множителя 0.125x (базовый 10/20 в DivisionStats)

## Dev режим

- **F2** — toggle туман войны (все юниты видны)
