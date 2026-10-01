# ИИ и навигация

## Иерархия

```
NPCFacade (абстрактный, MonoBehaviour)
├── BotMiner      — добытчик, цель: ResourceController
├── BotDefender   — защитник, цель: Enemy
├── Enemy         — враг, цели: боты, игрок, турели, постройки
└── BomberBug     — враг-смертник, свои анимации
```

Все они — на NavMesh (`com.unity.ai.navigation`), ходят через `NavMeshAgent`.

## NPCFacade — общий каркас

[`NPCFacade`](../Assets/Scripts/AI/NPCFacade.cs) в `Awake` собирает компоненты
(`Animator`, `NavMeshAgent`, `Rigidbody`, `Collider`, `NPCNavigation`,
`StatsHandler`), подписывается на `OnDeath` и каждый кадр дёргает две вещи:

```csharp
protected void Update()
{
    Navigation();            // abstract — каждый тип ищет свои цели
    NPCAnimationControl();   // virtual — анимация по состоянию
}
```

Смерть: анимация `Death`, выключение коллайдера, заморозка физики, отключение
агента, событие `onNpcDeath`, `Destroy(gameObject, 3)`.

Поле `requiredResources` — цена этого NPC в магазине (см. [Ресурсы](resources.md#траты)).

> `Update` в базе объявлен как `protected void`, а `BotMiner` объявляет
> **свой** `Update`, скрывающий базовый, и вызывает `base.Update()` вручную.
> Работает, но хрупко: забудешь вызвать базовый — NPC молча перестанет
> соображать. Правильнее `protected virtual void Update()` + `override`.

## NPCNavigation — выбор цели

[`NPCNavigation`](../Assets/Scripts/AI/NPCNavigation.cs) — общий для всех механизм
приоритетов. Идея: разные типы целей имеют разный вес, а ближняя цель
предпочтительнее дальней.

```
score = priority / distance
```

Три метода:

| Метод | Что делает | Стоимость |
|---|---|---|
| `FirstLook<T>(priority)` | ищет цель по **всей сцене** через `FindObjectsByType` | дорого |
| `ChaseTarget<T>(priority)` | ищет в радиусе `sightDistance` через `OverlapSphere` | дёшево |
| `SetAndRefresh()` | выбирает лучшую из накопленных и очищает список | — |

Задумка: `FirstLook` один раз в `Start` (найти, куда идти вообще), дальше каждый
кадр `ChaseTarget` (реагировать на то, что рядом).

Приоритеты по типам (из `Enemy` и `BomberBug`):

| Цель | Вес у `Enemy` | Вес у `BomberBug` |
|---|---|---|
| Игрок | 75 | 75 |
| `BotDefender` | 70 | 50 |
| `GatlingGun` | 60 | 25 |
| `BotMiner` | 50 | 10 |
| `Building` | 30 | — |

### Три бага в этом механизме

**1. Общий таймер на все типы целей.** `ChaseTarget` имеет один на объект
счётчик `timer` и сканирует не чаще раза в секунду. Но `Enemy.Navigation()`
вызывает `ChaseTarget` **четыре раза за кадр** — каждый вызов добавляет
`deltaTime`. В итоге порог берётся вчетверо быстрее, а сканируется при этом
только **один тип целей** — тот, на чьём вызове счётчик случайно перевалил порог.
Остальные три типа в этом цикле не осматриваются вовсе.

**2. `Enemy` зовёт не тот метод.** В
[`Enemy.Navigation()`](../Assets/Scripts/AI/Enemy.cs) для построек вызывается
`FirstLook<Building>(30)`, а не `ChaseTarget` — то есть **каждый кадр идёт
полный поиск по сцене**. Похоже на копипасту из `Start()`.

**3. Падение при дубликате цели.** `priorityTargets.Add(...)` бросит
`ArgumentException`, если один и тот же `Transform` добавится дважды за цикл. Это
случится, когда на одном объекте окажутся два искомых компонента — например
`Building` и `GatlingGun`. Сейчас таких префабов нет (проверено), но правило
«турель — это тоже постройка» выглядит естественным, и первый же такой префаб
уронит всех врагов на сцене.

Подробности и приоритеты — в [Известных проблемах](known-issues.md).

## Боты-добытчики

[`BotMiner`](../Assets/Scripts/AI/BotMiner.cs): ищет `ResourceController`, подходит
на `mineDistance`, раз в `cooldown` бьёт — `TakeHit()` плюс триггер `Mine`.

### Дубль логики добычи

[`BotNavigation`](../Assets/Scripts/AI/BotNavigation.cs) — **вторая, независимая**
реализация того же: корутина ищет ближайший ресурс в `currentRadius`, и если не
нашла, **удваивает радиус**. Ограничения сверху нет, поэтому на пустой карте
радиус растёт экспоненциально, а `OverlapSphere` с огромным радиусом собирает все
коллайдеры сцены каждый тик.

Этот скрипт не наследует `NPCFacade` и не использует `NPCNavigation` — отдельная
ветка эволюции. **Нужно выбрать одну реализацию и удалить вторую.**

## Враги

[`Enemy`](../Assets/Scripts/AI/Enemy.cs) — пустой по содержанию класс: только
список приоритетов. Вся логика в базе и в `NPCNavigation`. Служит ещё и маркером
для оружия игрока (`DamageEnemy`/`DamageZombie` ищут именно компонент `Enemy`).

[`BomberBug`](../Assets/Scripts/Enemy/BomberBug.cs) отличается анимацией: вместо
булевых `Idle`/`Run` использует float-параметр `locomotion` и триггер `attack1`.
Триггер взводится **каждый кадр**, пока цель в радиусе атаки.

## Анимация NPC

Базовый `NPCAnimationControl` ставит булевы параметры (`Idle`, `Run`, `Attack`),
но **не сбрасывает** их. Сброс сделан снаружи:
[`EnemyEndParams`](../Assets/Scripts/Enemy/EnemyEndParams.cs) — это
`StateMachineBehaviour`, который по выходу из состояния выставляет заданный в
инспекторе набор параметров.

Решение рабочее, но хрупкое: поведение анимации размазано между кодом и
настройками Animator Controller, и при добавлении состояния легко получить
NPC, навсегда застрявшего в атаке.

## Производительность

Что дорого прямо сейчас:

| Место | Что происходит |
|---|---|
| `Enemy.Navigation` | `FindObjectsByType<Building>` каждый кадр у каждого врага |
| `NPCNavigation.FirstLook` | полный обход сцены; у `Enemy` — 5 типов в `Start` |
| `BotNavigation.FindResource` | `OverlapSphere` с неограниченно растущим радиусом |
| `GatlingGun.Update` | `OverlapSphere` раз в секунду на каждую турель |

При волне в несколько десятков врагов это главный источник просадок. Решение —
общий сервис поиска целей с одним реестром, см.
[Архитектурный план](architecture-plan.md#этап-5-ии-и-поиск-целей).
