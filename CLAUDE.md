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
  Core/       — GameManager (singleton), EventBus, GameConfig (SO)
  Map/        — TerrainType (enum), TerrainData (SO)
  Units/      — DivisionType (enum), DivisionStats (SO)
  Combat/     — (будет)
  Economy/    — (будет)
  AI/         — (будет)
  UI/         — (будет)
  Camera/     — (будет)
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

## Ключевые параметры (из GDD)

- Стартовое золото: 300$
- Доход города: 10$/сек, столицы: 20$/сек
- Пехота: HP 100, урон 10/сек, стоимость 100$
- Танк: HP 200, урон 20/сек, стоимость 200$
- Победа: захват столицы + ≥80% территории
