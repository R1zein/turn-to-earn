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

Цикл `TakeHit()`: списать `oneHitResource` из запаса → начислить игроку → если
запас кончился, `Death()`.

`Death()` → `DeathEffect()`: выключает коллайдер и меш, спавнит эффект, ждёт 2
секунды, освобождает свою точку у `ResourceNodeSpawner`, уничтожает объект. Флаг
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

На карте три области узлов — объекты `gsp`, `msp`, `bsp` с компонентом
[`ResourceArea`](../Assets/Scripts/Level/ResourceArea.cs): дети объекта — точки,
список `stonePrefabs` — что здесь растёт, `timer` — секунды между попытками
(сейчас 0, то есть каждый кадр). Области перечислены в `LevelAnchors`.

| Область | Точек | Префабы |
|---|---|---|
| `gsp` | 70 | `OreStone1-2`, `Oak_Tree`, `Poplar_Tree` |
| `msp` | 15 | `Fir_Tree`, `IronOre1-3`, `OreStone1-2` |
| `bsp` | 20 | `Palm_Tree`, `GoldOre1-3` |

[`ResourceNodeSpawner`](../Assets/Scripts/Services/Spawning/ResourceNodeSpawner.cs)
— один сервис на все области:

- держит для каждой области список **свободных** точек и раз в `timer` ставит
  случайный префаб области в случайную свободную точку (через контейнер — у узла
  `[Inject]`-поля);
- узел, выработанный до конца, через 2 секунды зовёт `Release(this)` — точка
  возвращается в список свободных своей области.

На старте все 105 точек заполняются за первые кадры.

## Траты

| Что покупаем | Где | Цена |
|---|---|---|
| Постройка | [`BuildService`](../Assets/Scripts/Services/BuildService.cs) | `Ghost.requiredResources` |
| Бот | [`ShopService`](../Assets/Scripts/Services/ShopService.cs) | `NPCFacade.requiredResources` |

Обе списывают через `ResourceWallet.TrySpend`. Постройка: при выборе призрака
цена только **проверяется**, списывается — при установке (F); не хватает —
призрак остаётся. Цены сейчас: стена и стол — 0, турель — 80 железа, 10 дерева,
20 камня; оба бота — 0.
Цену показывает [`PriceTextBuilder`](../Assets/Scripts/Player/PriceTextBuilder.cs)
— он выводит только дерево, железо и камень, **золото в ценнике не отображается**.
