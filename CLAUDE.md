# Dot Wars

Solo PvE real-time стратегия (2D, вид сверху). Игрок (синий) vs 1 бот (красный).
Дивизии — цветные кружки, карта — tilemap с рельефом.

## Tech Stack

- **Движок:** Unity 6 (6000.3.10f1), Universal 2D (URP)
- **Язык:** C#
- **GitHub:** https://github.com/konyasharin/war-of-dots.git

## Структура Assets/

```
Scripts/
  Core/       — GameManager, EventBus, GameConfig (SO), GameSetup
  Map/        — MapManager, Pathfinding (A*), TerrainType, TerrainConfig, City, Port
  Units/      — Division, DivisionSpawner, SelectionManager, DivisionType, DivisionStats
  Economy/    — EconomyManager
  AI/         — (будет)
  UI/         — EconomyHUD
  Camera/     — CameraController
  Utils/      — (будет)
Resources/
  Terrain/    — TerrainConfig SO per type
  Units/      — InfantryStats, TankStats
  Prefabs/    — Division prefab
  Sprites/    — CrackLight, CrackHeavy
  GameConfig.asset
```

## Архитектурные решения

- **Namespaces:** `DotWars.Core`, `DotWars.Map`, `DotWars.Units`, `DotWars.Economy`, `DotWars.UI`, `DotWars.CameraSystem`
- **Resources.Load:** все SO и префабы загружаются из Resources (не serialized refs в сцене)
- **EventBus:** статический класс с C# Action events
- **GameManager:** Singleton + DontDestroyOnLoad, GameState + Time.timeScale
- **MapManager:** Singleton, grid из Tilemap, TerrainConfig загружается из Resources по имени тайла
- **Pathfinding:** A* с 8 направлениями, стоимость = 1/speedModifier
- **Division:** Visual контейнер (для shake), Dynamic Rigidbody2D, velocity-based movement, HP bar, crack overlay
- **SelectionManager:** ЛКМ клик + рамка выделения, ПКМ = движение с формацией
- **CameraController:** WASD + edge pan + MMB drag + scroll zoom, bounds привязаны к карте
- **EconomyManager:** золото для 2 игроков, города генерируют доход
- **City:** флаг + обводка, захват при контакте вражеского юнита
- **Port:** обводка, заготовка для конвертации в корабль
- **EconomyHUD:** OnGUI отображение валюты (правый нижний угол)

## Setup

1. Меню **DotWars > Setup All**
2. Открыть **Assets/Scenes/Game.unity**
3. Play

## Ключевые параметры

- Стартовое золото: 300$
- Доход города: 10$/сек, столицы: 20$/сек
- Пехота: HP 100, урон 10/сек, скорость 1.5, стоимость 100$
- Танк: HP 200, урон 20/сек, скорость 1.5, стоимость 200$ (чёрная точка в центре)
- Конвертация в корабль: 50$ + 10 сек
- Победа: захват столицы + ≥80% территории
