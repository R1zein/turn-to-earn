# ИИ и навигация

## Иерархия

```
NPCFacade (абстрактный, MonoBehaviour)
├── BotMiner      — добытчик, цель: узлы ресурсов
├── BotDefender   — защитник, цель: враги
├── Enemy         — враг, цели: игрок, боты, турели, постройки
└── BomberBug     — враг-смертник, свои анимации (ни на одном префабе)
```

Все они — на NavMesh (`com.unity.ai.navigation`), ходят через `NavMeshAgent`.
Рождаются через спавнеры (`EnemySpawner`, `BotSpawner`) — то есть через контейнер.

## Тело и мозг

Тело — `MonoBehaviour` на префабе: агент, аниматор, коллайдер, здоровье.
Решение «куда идти» принимает сервис
[`TargetingService`](../Assets/Scripts/Services/Targeting/TargetingService.cs)
поверх [`TargetRegistry`](../Assets/Scripts/Services/Targeting/TargetRegistry.cs).

### Реестр целей

`TargetKind`: `Player`, `BotMiner`, `BotDefender`, `Turret`, `Building`, `Enemy`,
`ResourceNode`. Каждое тело, которое может быть целью, **само** добавляется в
реестр в `OnEnable` и убирается в `OnDisable`:

| Тело | Вид |
|---|---|
| `Player` | `Player` |
| `BotMiner`, `BotDefender`, `Enemy` (через `NPCFacade.Kind`) | одноимённый |
| `GatlingGun` | `Turret` |
| `Building` | `Building` |
| `ResourceController` | `ResourceNode` |

Умирающий NPC и выработанный узел убираются из реестра сразу, ещё до
уничтожения, — на труп и на пустой узел больше никто не идёт. Уничтоженные
объекты реестр отсеивает при чтении (Unity-null), так что мёртвую цель он не
отдаст, даже если удаление где-то пропустили.

`BomberBug` в реестр не попадает (`Kind = null`): раньше защитники и турели
искали компонент `Enemy`, которого у него нет, — поведение сохранено.

### Выбор цели

```
score = weight / distance        — побеждает наибольший
```

[`NPCNavigation`](../Assets/Scripts/AI/NPCNavigation.cs) — телесная часть: держит
`target` и каждый кадр ведёт агента к нему. Сервис она спрашивает в двух случаях:

| Метод | Когда | Где ищет |
|---|---|---|
| `LookEverywhere(weights)` | один раз в `Start` | вся карта |
| `Rescan(weights)` | раз в секунду, из `Navigation()` | радиус `sightDistance` |

Оба проходят по реестру **один раз по всем видам сразу**. Если в обзоре никого
нет, цель остаётся прежней.

Веса — в `GameConfig` (секция Targeting):

| Цель | `enemyTargets` | `bomberTargets` | `defenderTargets` | `minerTargets` |
|---|---|---|---|---|
| Игрок | 75 | 75 | — | — |
| `BotDefender` | 70 | 50 | — | — |
| Турель | 60 | 25 | — | — |
| `BotMiner` | 50 | 10 | — | — |
| Постройка | 30 | — | — | — |
| Враг | — | — | 50 | — |
| Узел ресурса | — | — | — | 10 |

> Расстояние считается до позиции объекта, а не до ближайшей точки коллайдера,
> как было с `OverlapSphere`. Для больших построек цель «видна» чуть позже.

## NPCFacade — общий каркас

[`NPCFacade`](../Assets/Scripts/AI/NPCFacade.cs) в `Awake` собирает компоненты
(`Animator`, `NavMeshAgent`, `Rigidbody`, `Collider`, `NPCNavigation`,
`StatsHandler`), в `OnEnable` подписывается на `OnDeath` и встаёт в реестр, и
каждый кадр дёргает две вещи:

```csharp
protected void Update()
{
    Navigation();            // abstract — у каждого типа: navigation.Rescan(свои веса)
    NPCAnimationControl();   // virtual — анимация по состоянию
}
```

Смерть: уход из реестра, анимация `Death`, выключение коллайдера, заморозка
физики, отключение агента, `Destroy(gameObject, 3)`.

Поле `requiredResources` — цена этого NPC в магазине (см. [Ресурсы](resources.md#траты)).

> `Update` в базе объявлен как `protected void`, а `BotMiner` объявляет
> **свой** `Update`, скрывающий базовый, и вызывает `base.Update()` вручную.
> Работает, но хрупко: забудешь вызвать базовый — NPC молча перестанет
> соображать. Правильнее `protected virtual void Update()` + `override`.

## Боты-добытчики

[`BotMiner`](../Assets/Scripts/AI/BotMiner.cs): цель — узел ресурса, подходит на
`mineDistance`, раз в `cooldown` бьёт — `TakeHit()` плюс триггер `Mine`.
Вторая реализация добычи (`BotNavigation`) удалена на этапе 6.

## Враги

[`Enemy`](../Assets/Scripts/AI/Enemy.cs) — только веса целей; вся логика в базе,
`NPCNavigation` и сервисе.

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

Поиска по сцене и `OverlapSphere` в выборе целей больше нет: каждый NPC раз в
секунду проходит по реестру (порядка сотни записей, из них 105 — узлы ресурсов,
которые смотрят только добытчики). Замер 2026-10-08: волна из 35 врагов — в
среднем 8 мс на кадр, p99 11 мс.
