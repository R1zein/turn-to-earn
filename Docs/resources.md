# Ресурсы и добыча

## Четыре ресурса

Валюта игры — четвёрка чисел в [`AllResources`](../Assets/Scripts/AllResources.cs):

| Поле | Смысл |
|---|---|
| `ore` | камень |
| `tree` | дерево |
| `iron` | железо |
| `gold` | золото |

`AllResources` — обычный `[Serializable]` класс (не SO, не struct), с
перегруженными операторами `+ - > < >= <= == !=`.

> **Ловушка именования.** Enum [`MineableResourses`](../Assets/Scripts/Player/MineableResourses.cs)
> называет те же сущности иначе: `Stones`, `Wood`, `Gold`, `Iron`. То есть
> `Stones` ↔ `ore`, `Wood` ↔ `tree`. Не перепутай при маппинге.

### Как работают сравнения

Все операторы сравнения требуют выполнения условия **по всем четырём** полям
одновременно (`&`). То есть `a >= b` означает «хватает каждого из четырёх
ресурсов» — именно это и нужно для проверки цены.

> `operator !=` был сломан (перепутаны `|` и `&`) — исправлено 2026-10-08,
> теперь это отрицание `==`; равенство сравнивает значения и безопасно к `null`.
> См. [Известные проблемы](known-issues.md#p0-1-operator--в-allresources-даёт-неверный-результат).

## Хранилище игрока

[`ResourceWallet`](../Assets/Scripts/Services/ResourceWallet.cs) — сервис сцены
(`AsSingle` в `GameplayInstaller`). Держит текущие запасы и сообщает об изменении
событием `OnChanged`. Получают его через `[Inject]`.

```csharp
[Inject] private ResourceWallet wallet;

wallet.Add(new AllResources(0, oneHitResource, 0, 0));
if (wallet.TrySpend(price)) { /* купили */ }
```

- `TrySpend` проверяет и списывает одним вызовом — уйти в минус нельзя.
- Состояние живёт в контейнере сцены: перезагрузка сцены начинает с пустого
  кошелька.
- Отображение — временный uGUI-компонент
  [`ResourcesView`](../Assets/Scripts/UI/ResourcesView.cs) на `Canvas`
  (четыре TMP-поля, подписан на `OnChanged`). На этапе 7 его заменит
  `ResourcesHudController` на UI Toolkit (см. [Интерфейс](ui.md)).

## Добыча

### Игроком

1. [`ToolController`](../Assets/Scripts/Player/ToolController.cs) на ЛКМ запускает
   триггер аниматора `"Hit"`.
2. Анимация в нужном кадре вызывает `Hit()` (Animation Event).
3. `Hit()` пускает луч из центра экрана на **1.5 м**, ищет `ResourceController`
   вверх по иерархии.
4. Если тип ресурса есть в списке `mineableResourses` этого инструмента —
   `TakeHit()` и звук удара.

То есть **инструмент определяет, что им можно добывать**: у кирки в списке камень
и железо, у топора — дерево. Список задаётся в инспекторе на префабе инструмента.

Смена инструмента — [`ToolsSwap`](../Assets/Scripts/Player/ToolsSwap.cs), клавиши
`Q`/`E`, просто включает/выключает объекты из массива `tools`.

### Ботами

[`BotMiner`](../Assets/Scripts/AI/BotMiner.cs) ищет ближайший `ResourceController`
через систему навигации, подходит на `mineDistance` и бьёт с периодом `cooldown`.
Добытое ботом уходит **в тот же общий склад игрока** — `ResourceController` сам
начисляет в `ResourceWallet`.

> В проекте есть второй, несвязанный добытчик —
> [`BotNavigation`](../Assets/Scripts/AI/BotNavigation.cs), дублирующий ту же
> механику другим способом. Какой из них реально используется — зависит от
> префаба. См. [ИИ](ai.md#дубль-логики-добычи).

## Узлы ресурсов

Базовый класс [`ResourceController`](../Assets/Scripts/ResourceController.cs)
(абстрактный) + четыре наследника: `StoneController`, `TreeController`,
`IronController`, `GoldController`. Наследники отличаются **одной строкой** — в
какое поле `AllResources` начислять.

Поля узла:

| Поле | Смысл |
|---|---|
| `resourceStore` | сколько всего осталось в узле |
| `oneHitResource` | сколько даёт один удар |
| `resourse` | тип (`MineableResourses`) — с ним сверяется инструмент |
| `positionID` | индекс точки спавна, чтобы освободить её после смерти |

Цикл `TakeHit()`: списать `oneHitResource` из запаса → начислить игроку → если
запас кончился, `Death()`.

`Death()` → `DeathEffect()`: выключает коллайдер и меш, спавнит эффект, ждёт 2
секунды, сообщает спавнеру об освобождении точки, уничтожает объект. Флаг
`isDying` защищает от повторного запуска.

> `DeathEffect()` — `async Awaitable`, вызываемый без `await` и без токена отмены.
> См. [Известные проблемы](known-issues.md#p2-1-асинхронные-методы-без-отмены).

### Префабы залежей

`Assets/Prefabs/Ores/`: `IronOre1-3`, `GoldOre1-3`, `CopperOre1-3` — у каждого
свой меш. Железо — меши пака `Low_Poly_ResourceRocks` (октаэдры и кубы);
золото (жилы + самородки) и медь (друзы шестигранных кристаллов) сделаны в
Blender в том же стиле и лежат в `Assets/Meshes/Ores/`. У всех мешей два
сабмеша: `[0]` камень (`Stone.mat`), `[1]` руда.

> **Медь пока только визуальная.** На `CopperOre1-3` нет `ResourceController`:
> ресурса «медь» нет ни в `AllResources`, ни в `MineableResourses`. В спавнеры
> их не добавлять, пока ресурс не заведён.

## Респавн

[`ResourceSpawner`](../Assets/Scripts/ResourceSpawner.cs) — один объект, дети
которого служат точками спавна.

- В `Awake` собирает точки из `GetComponentsInChildren<Transform>()`.
- Каждые `timer` секунд выбирает **случайную** точку; если она свободна
  (`positions[point] == 0`), ставит случайный префаб из `stonePrefabs` и помечает
  занятой.
- `FindDestroyed(positionID)` освобождает точку, когда узел умирает.

Два следствия, важных для баланса:

1. Выбор точки случайный **без повторной попытки** — если выпала занятая,
   этот тик пропадает впустую. Чем плотнее заселена карта, тем реже спавн.
2. `spawnPoints` заполняется и из инспектора, и из `GetComponentsInChildren` —
   заданные вручную точки **продублируются**.

## Траты

| Что покупаем | Где | Цена |
|---|---|---|
| Постройка | [`Inventory.Build`](../Assets/Scripts/Player/Inventory.cs) | `Ghost.requiredResources` |
| Бот | [`ShopController.ByeBot`](../Assets/Scripts/UI/ShopController.cs) | `NPCFacade.requiredResources` |

Обе проверки одинаковы: `CurrentResources >= цена`, затем `DecreaseResources`.
Цену показывает [`PriceTextBuilder`](../Assets/Scripts/Player/PriceTextBuilder.cs)
— он выводит только дерево, железо и камень, **золото в ценнике не отображается**.
