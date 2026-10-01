# Обзор проекта

## Что это за игра

Выживание от первого лица в открытом мире. Игрок добывает ресурсы, строит базу и
укрепления, покупает ботов-помощников (добытчиков и защитников), отбивается от волн
врагов, приходящих по ночам, и может пересесть в дрон с лазерами. Квесты были на
графах Unity Behavior; граф удалён 2026-07-02, и квестовой системы сейчас нет —
см. [Квесты и диалоги](quests-and-dialogs.md).

Название отсылает к экономическому циклу: **ход → ресурс → постройка → защита**.

## Технический стек

| Что | Чем |
|---|---|
| Unity | 6000.3.19f1 |
| Рендер | URP 17.3.0 |
| Ввод | **Input Manager (legacy)** — Unity помечает как устаревший |
| ИИ | NavMesh (`com.unity.ai.navigation`) + свой код в `AI/` |
| Unity Behavior | 1.0.16 — пакет установлен, **графов нет**; держится только на останках квестов |
| Интерфейс | UI Toolkit (новый HUD) + uGUI/TextMeshPro (старое, мигрирует) |
| Сборки | одна `Assembly-CSharp` — **asmdef в проекте нет** |
| Тесты | **отсутствуют** |
| MCP | `com.coplaydev.unity-mcp` v10.0.0 для управления редактором из агента |
| DI | **Zenject** — целевая архитектура, ещё не установлен ([этап 2](architecture-plan.md#этап-2-каркас-zenject)) |

## Структура

```
Assets/
  Scripts/            83 файла, ~3000 строк — весь игровой код
    AI/               боты, враги, навигация, турель
    Behaviour Tree/   мёртвое: узлы удалённого графа, event-каналы, учебные заготовки
    Drone/            дрон: полёт, камера, стрельба, телеметрия
    Enemy/            спавн волн, урон, параметры анимаций
    Player/           игрок, инвентарь, инструменты, ресурсы на руках
    Quests/           мёртвое: квесты удалённой системы
    UI/               HUD, магазин, диалоги, здоровье
  UI Toolkit/         Main.uxml, DroneStats.uxml, Cyberpunk.uss, PanelSettings
  Scenes/             Scene 5 (основная FPS-карта), SampleScene (дрон), Tutorial
                      в Build Settings не добавлена ни одна
  Prefabs/            98 префабов
  <пакеты ассетов>    Bitgem, Hovl Studio, Gentleland, NamuFX, ... — не трогать
```

Сторонние пакеты ассетов лежат прямо в `Assets/` вперемешку с проектными папками.
Правило: **всё своё — в `Assets/Scripts`, `Assets/UI Toolkit`, `Assets/Prefabs`,
`Assets/Scenes`**; остальные папки верхнего уровня считать внешними и не править.

## Точки входа

| Подсистема | Главный скрипт |
|---|---|
| Игрок | [`Player.cs`](../Assets/Scripts/Player/Player.cs) + `FirstPersonMovement` (сторонний) |
| Добыча | [`ToolController.cs`](../Assets/Scripts/Player/ToolController.cs) → [`ResourceController`](../Assets/Scripts/ResourceController.cs) |
| Ресурсы | [`StoredResources.cs`](../Assets/Scripts/Player/StoredResources.cs) (синглтон) |
| Строительство | [`Inventory.cs`](../Assets/Scripts/Player/Inventory.cs) |
| Магазин ботов | [`ShopController.cs`](../Assets/Scripts/UI/ShopController.cs) |
| ИИ | [`NPCFacade.cs`](../Assets/Scripts/AI/NPCFacade.cs) + [`NPCNavigation.cs`](../Assets/Scripts/AI/NPCNavigation.cs) |
| Здоровье/урон | [`StatsHandler.cs`](../Assets/Scripts/UI/StatsHandler.cs) |
| Время и волны | [`TimeManager.cs`](../Assets/Scripts/TimeManager.cs) + [`TimePeriod.cs`](../Assets/Scripts/TimePeriod.cs) |
| Дрон | [`DroneControl.cs`](../Assets/Scripts/Drone/DroneControl.cs) |
| Глобальные флаги | [`GameManager.cs`](../Assets/Scripts/Player/GameManager.cs) (синглтон) |

## Как подсистемы связаны

Явной композиции нет — связи собираются тремя способами:

1. **Синглтоны** — `StoredResources.instance`, `GameManager.instance`
2. **Поиск по сцене** — `FindAnyObjectByType` / `GameObject.Find` в 8 файлах
3. **События** — `StatsHandler.OnDeath`/`OnDamage`; ещё event-каналы Unity
   Behavior (ScriptableObject), но у них не осталось ни одного слушателя

Это главный архитектурный долг: зависимости не видны по типам и ломаются молча.
Цель — один граф объектов на сцену, который собирает контейнер Zenject
([Архитектура](architecture.md)); как к ней прийти — [Архитектурный план](architecture-plan.md).

## Текущая стадия

Активная разработка, играбельный прототип. Приоритет — механики, а не чистота;
многое написано «на один раз» и дублируется. Часть файлов — учебные заготовки
(`Behaviour Tree/Tutorial/`) и пустые болванки (`TimeSystem.cs`, `DroneMain.cs`),
их можно удалять без оглядки. Останки квестовой системы — тоже, но в определённом
порядке ([какой](quests-and-dialogs.md#как-убрать-останки)).
