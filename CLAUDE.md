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
  Core/       — GameManager (singleton), EventBus, GameConfig (SO), GameSetup (bootstrap)
  Map/        — MapManager, Pathfinding (A*), TerrainType, TerrainData
  Units/      — Division, DivisionSpawner, SelectionManager, DivisionType, DivisionStats
  Combat/     — (будет)
  Economy/    — (будет)
  AI/         — (будет)
  UI/         — (будет)
  Camera/     — CameraController (pan + zoom)
  Utils/      — (будет)
Prefabs/      — Units/, Map/, UI/
ScriptableObjects/ — Terrain/, Units/, AI/
Sprites/      — Units/, Map/, UI/, Effects/
Audio/        — SFX/, Music/
Tiles/
Scenes/
Resources/
```

## Архитектурные решения

- **Namespaces:** `DotWars.Core`, `DotWars.Map`, `DotWars.Units`, etc.
- **EventBus:** статический класс с C# Action events для слабого связывания систем
- **GameManager:** Singleton + DontDestroyOnLoad, управляет GameState и Time.timeScale
- **ScriptableObjects:** конфигурация (GameConfig, TerrainData, DivisionStats) — данные отделены от логики
- **TerrainData:** поддерживает override модификаторов для конкретных типов юнитов (infantrySpeedModifier, tankDamageModifier)
- **MapManager:** Singleton, строит grid из Tilemap, маппит тайлы → TerrainData через TerrainTileMapping[]
- **Pathfinding:** A* с 8 направлениями, стоимость = 1/speedModifier, учитывает тип юнита
- **Division:** MonoBehaviour на каждом юните, движение по пути, HP/Morale, цвет по владельцу
- **SelectionManager:** ЛКМ = выделение (Shift для мульти), ПКМ = приказ на движение
- **CameraController:** WASD/стрелки + edge pan + middle mouse drag + scroll zoom
- **SetupWizard:** Editor-скрипт (DotWars > Setup All) — создаёт тайлы, SO, префаб, сцену с картой 30x20
- **GameSetup:** Runtime bootstrap — StartGame() + спавн стартовых дивизий

## Setup (первый запуск)

1. Открыть проект в Unity
2. Меню **DotWars > Setup All** — создаст все ассеты и сцену Game
3. Открыть **Assets/Scenes/Game.unity**
4. Нажать Play

## Ключевые параметры (из GDD)

- Стартовое золото: 300$
- Доход города: 10$/сек, столицы: 20$/сек
- Пехота: HP 100, урон 10/сек, стоимость 100$
- Танк: HP 200, урон 20/сек, стоимость 200$
- Победа: захват столицы + ≥80% территории
