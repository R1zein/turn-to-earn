# Известные проблемы

Результат разбора кодовой базы (83 скрипта, ~3000 строк). Приоритеты:

- **P0** — ломает игру или считает неверно прямо сейчас
- **P1** — ломает при определённых условиях либо копит долг, который дорожает
- **P2** — работает, но неправильно по форме; чинить при случае
- **P3** — мусор и косметика

Нашёл новое — допиши сюда, даже если не чинишь.

---

## P0 — чинить первым

### P0-1. `operator !=` в `AllResources` даёт неверный результат

> **Исправлено** 2026-10-08.

[`AllResources.cs:58`](../Assets/Scripts/AllResources.cs)

```csharp
if (left.iron != right.iron | left.tree != right.tree | left.ore != right.ore & left.gold != right.gold)
```

В C# `&` приоритетнее `|`, поэтому выражение считается как
`a | b | (c & d)` — а не как «хоть что-то отличается». Оператор не является
отрицанием `==`: есть наборы значений, где и `==`, и `!=` вернут `true`.

**Чинить:** `public static bool operator !=(AllResources l, AllResources r) => !(l == r);`

Заодно: класс перегружает `==`/`!=`, но не переопределяет `Equals` и
`GetHashCode` (предупреждения компилятора CS0660/CS0661), и `==` упадёт с
`NullReferenceException` при сравнении с `null`.

### P0-2. `OnDeath` срабатывает многократно

> **Исправлено** 2026-10-08.

[`StatsHandler.cs:14`](../Assets/Scripts/UI/StatsHandler.cs)

```csharp
public void TakeDamage(float damage)
{
    currHP -= damage;
    OnDamage?.Invoke();
    if (currHP <= 0) OnDeath?.Invoke();
}
```

Флага «уже мёртв» нет. Каждый следующий удар по трупу (а он прилетит: турель и
зоны урона продолжают бить, пока объект не уничтожен) снова поднимает `OnDeath`.
Последствия: повторный `Destroy`, двойной спавн камеры смерти у игрока, двойной
инкремент `countNPC`, повторное событие `onNpcDeath`.

**Чинить:** флаг `isDead`, ранний выход в `TakeDamage`.

### P0-3. Враги каждый кадр обыскивают всю сцену

> **Исправлено** 2026-10-08.

[`Enemy.cs:19`](../Assets/Scripts/AI/Enemy.cs)

В `Navigation()` (вызывается каждый кадр) стоит `FirstLook<Building>(30)` вместо
`ChaseTarget<Building>(30)`. `FirstLook` делает `FindObjectsByType` по всей сцене.
При волне из N врагов — N полных обходов сцены за кадр.

**Чинить:** заменить на `ChaseTarget<Building>(30)`.

---

## P1 — ломается при условиях

### P1-1. Общий таймер ломает выбор целей

[`NPCNavigation.cs:54`](../Assets/Scripts/AI/NPCNavigation.cs)

`ChaseTarget` ограничивает сканирование одним разом в секунду через поле `timer`.
Но `Enemy` вызывает `ChaseTarget` четыре раза за кадр, и каждый вызов добавляет
`deltaTime` к **одному и тому же** счётчику. Следствия:

1. Порог в 1 секунду берётся примерно вчетверо быстрее.
2. В момент срабатывания сканируется **только один** тип целей — тот, на чьём
   вызове счётчик перевалил порог. Остальные три типа в этот цикл не смотрятся.

То есть система приоритетов работает не так, как задумана: враг видит не «лучшую
цель из всех», а «лучшую цель одного случайного типа».

**Чинить:** быстро — таймер на каждый тип цели; по плану — один проход по реестру целей
в `TargetingService`, все типы сразу (см. [план](architecture-plan.md#этап-6-бой-и-ии)).

### P1-2. Падение при дубликате цели

[`NPCNavigation.cs:46,77`](../Assets/Scripts/AI/NPCNavigation.cs)

`priorityTargets.Add(newTarget, score)` бросит `ArgumentException`, если один и
тот же `Transform` придёт дважды за цикл — а это случится, когда на объекте
окажутся два искомых компонента (например `Building` и `GatlingGun`).

Проверено: сейчас таких префабов нет, **баг латентный**. Но «турель — это тоже
постройка» звучит естественно, и первый же такой префаб положит весь ИИ.

**Чинить:** `priorityTargets[newTarget] = score;` вместо `Add`.

### P1-3. Состояние игры живёт в ассетах

> **Исправлено** 2026-10-08: квесты и каналы удалены на этапе 1, состояние суток
> переехало в сервис `DayCycle` на этапе 3. Описание ниже — как было.

Самая дорогая проблема: изменяемое состояние хранится в ScriptableObject, то есть
**пишется в файлы проекта** и переживает перезапуск.

| Где | Что хранится |
|---|---|
| [`Quest`](../Assets/Scripts/Quest.cs) | `IsTaken`, `rewardGained`, `currentIndex` — квесты мертвы |
| [`OnBotCreated`](../Assets/Scripts/Behaviour%20Tree/EventChannel/OnBotCreated.cs) | `firstBotCrea6ted` |
| [`OnNpcDeath`](../Assets/Scripts/Behaviour%20Tree/EventChannel/OnNpcDeath.cs) | `countNPC` |
| [`TimePeriod`](../Assets/Scripts/TimePeriod.cs) | `dayNumber`, `currentProgress`, `wasInPeriod` |

Проявление: счётчик убитых NPC растёт от сессии к сессии; пока квесты работали,
пройденный в редакторе квест оставался «взятым и завершённым» навсегда. Квесты и
каналы удаляются на [этапе 1](architecture-plan.md#этап-1-страховка) как останки —
после этого проблема остаётся только у `TimePeriod`.

`TimePeriod` спасает только `InitSettings`, сбрасывающий поля в `Awake`.

**Чинить:** разделить «конфиг» (SO, только чтение) и «состояние» (обычные классы,
живущие в рантайме): сервисы сцены `AsSingle`. Это [этап 3](architecture-plan.md#этап-3-состояние-из-ассетов-в-сервисы) плана.

### P1-4. Три владельца курсора

> **Исправлено** 2026-10-08: курсором владеет только `InputService` (стек режимов).

`Cursor.lockState` переключают независимо
[`ShopController`](../Assets/Scripts/UI/ShopController.cs),
[`Inventory`](../Assets/Scripts/Player/Inventory.cs) и
[`DroneControl`](../Assets/Scripts/Drone/DroneControl.cs).

Открыл магазин → открыл панель строительства → закрыл одну: итоговое состояние
курсора зависит от того, кто отработал последним. Пересесть в дрон с открытой
панелью — и курсор останется запертым.

**Чинить:** режимом курсора владеет только `InputService`, [этап 4](architecture-plan.md#этап-4-ввод).

### P1-5. Укрепление можно начать только одно

[`BsseManager.cs`](../Assets/Scripts/UI/BsseManager.cs)

`BuildFortification` работает только при `currentFortification == null`, а в
`null` это поле не возвращается никогда. После первой секции кнопки выбора других
секций молча перестают работать.

### P1-6. Ресурсы списываются за призрак, а не за постройку

[`Inventory.Build`](../Assets/Scripts/Player/Inventory.cs)

Цена снимается в момент создания призрака. Отменить режим постройки нельзя —
способа выбросить призрак в коде нет. Передумал — ресурсы потеряны.

### P1-7. Свет периодов берётся через поиск по имени

> **Исправлено** 2026-10-08: свет берётся из `LevelAnchors.Sun`.

`TimeManager.cs:22` (до этапа 3)

```csharp
public Light directionLight;                                        // не используется
Light directionlLight = GameObject.Find("Directional Light")...;    // используется
```

Опечатка в имени локальной переменной затенила поле из инспектора. Переименование
объекта на сцене тихо ломает смену освещения; публичное поле вводит в заблуждение.

---

## P2 — неправильно по форме

### P2-1. Асинхронные методы без отмены

| Где | Что |
|---|---|
| [`ZombieSpawn.OpenPortal`](../Assets/Scripts/Enemy/ZombieSpawn.cs) | `async void`, цикл спавна |
| [`ResourceController.DeathEffect`](../Assets/Scripts/ResourceController.cs) | `async Awaitable`, вызван без `await` |

Ни один не принимает токен отмены. Уничтожение объекта или выгрузка сцены в
процессе — и продолжение метода обращается к мёртвым объектам. У `async void`
исключение ещё и теряется.

**Чинить:** `async Awaitable` + `destroyCancellationToken`.

### P2-2. Четыре разных способа нанести урон

`DamagePlayer`, `DamageBot`, `DamageEnemy`, `DamageZombie` — почти одинаковые
скрипты, но два выбирают цель по фракции, а два по наличию компонента `Enemy`.
Урон у половины зашит в код (10 и 5). Подробности — в [Бою](combat.md#нанесение-урона--четыре-разных-способа).

### P2-3. Дублирующая логика добычи

[`BotNavigation`](../Assets/Scripts/AI/BotNavigation.cs) и
[`BotMiner`](../Assets/Scripts/AI/BotMiner.cs) решают одну задачу двумя способами.
У `BotNavigation` вдобавок радиус поиска растёт вдвое без ограничения сверху —
на пустой карте `OverlapSphere` начнёт собирать всю сцену.

**Чинить:** выбрать одну реализацию, вторую удалить.

### P2-4. `Update` скрыт, а не переопределён

[`NPCFacade`](../Assets/Scripts/AI/NPCFacade.cs) объявляет `protected void Update()`,
`BotMiner` объявляет свой и вручную зовёт `base.Update()`. Забыл вызвать — NPC
молча перестал думать.

**Чинить:** `protected virtual void Update()` + `override`.

### P2-5. Обязательные данные не проверяются при старте

Пустой массив или отсутствующий компонент обнаруживается не при сборке сцены, а
в случайный момент игры — непонятным `IndexOutOfRange` или `NullReference`
посреди геймплея:

| Где | Что упадёт и когда |
|---|---|
| [`ToolsSwap.cs:46`](../Assets/Scripts/Player/ToolsSwap.cs) | `tools[toolIndex]` при пустом массиве — на старте |
| [`SoundController`](../Assets/Scripts/Player/SoundController.cs) | `miningSounds[random]` при пустом списке — на первом ударе |
| [`Building.TakeDamage`](../Assets/Scripts/Building.cs) | `audioClips[randomIndex]` при пустом массиве — при первом уроне |
| [`Selectable.Start`](../Assets/Scripts/Selectable.cs) | `outline.enabled`, если нет `Outline` — на старте |

**Чинить не тихой защитой** (`if (tools.Length == 0) return;` спрячет ошибку
сборки сцены), а проверкой при инициализации, которая падает **сразу и с
понятным сообщением** — по [правилу архитектуры](architecture.md#ловушки) про
обязательные данные.

### P2-6. Точки спавна ресурсов дублируются

[`ResourceSpawner.Awake`](../Assets/Scripts/ResourceSpawner.cs) добавляет всех
детей в `spawnPoints`, который уже мог быть заполнен в инспекторе. Плюс выбор
точки случайный без повторной попытки: выпала занятая — тик спавна пропал зря.

### P2-7. Останки квестовой системы работают вхолостую

Квестовый граф удалён 2026-07-02, но вокруг него остались два агента
`BehaviorGraphAgent` с висячей ссылкой на граф (`Player.prefab` и `Tutorial`), три
event-канала, которые игра продолжает вызывать без единого слушателя, и пакет
Unity Behavior, который из-за этих каналов нельзя просто удалить. Исключений нет,
но код выглядит живым и вводит в заблуждение.

**Чинить:** снять останки в правильном порядке — пакет последним, см.
[Квесты](quests-and-dialogs.md#как-убрать-останки).

### P2-8. Устаревший Input Manager

> **Исправлено** 2026-10-08: свой код читает ввод через Input System
> (`Controls` → `InputService`). Active Input Handling — Both из-за демо-скриптов
> сторонних ассетов.

Unity сообщает об этом в консоли при каждом запуске:

> This project uses Input Manager, which is marked for deprecation.

Весь ввод — через `Input.GetKey`/`GetMouseButton`, разбросанный по десятку
скриптов. Переназначить клавиши нельзя, геймпад не поддержан.

### P2-9. Мелкие просчёты производительности

- `Camera.main` в 8 местах, часть — каждый кадр (`ObjectPicker.LateUpdate`,
  `CanvasController.LateUpdate` — дважды за вызов).
- `ObjectPicker` пускает луч **без ограничения дальности** каждый кадр.
- `FindAnyObjectByType` в `Start` у `ResourceController` — у каждого узла
  ресурсов свой полный поиск спавнера.

---

## P3 — мусор и косметика

### P3-1. Мёртвые файлы

| Файл | Что в нём |
|---|---|
| [`TimeSystem.cs`](../Assets/Scripts/TimeSystem.cs) | пустой шаблон Unity |
| [`DroneMain.cs`](../Assets/Scripts/Drone/DroneMain.cs) | пустой шаблон Unity |
| [`KillingQuest.cs`](../Assets/Scripts/Quests/KillingQuest.cs) | одна закомментированная строка |
| [`Behaviour Tree/Tutorial/`](../Assets/Scripts/Behaviour%20Tree/Tutorial/) | 9 учебных файлов, к игре не относятся |
| [`PlayerControll.cs`](../Assets/Scripts/Player/PlayerControll.cs) | шлёт event-канал, у которого нет слушателей |
| квестовая система целиком | граф удалён; состав и порядок удаления — в [Квестах](quests-and-dialogs.md#что-осталось) |

### P3-2. Опечатки в именах

`Interract`, `PlayerControll`, `BsseManager` (Base), `PhpBarController` (HP),
`MineableResourses`, `firstBotCrea6ted`, `firstKillMaded`, `ByeBot` (Buy),
`FirstKillQst`, `GatelingGun_Yellow_L3` (префаб).

Переименование типов рвёт связи в сценах и префабах — делать отдельной задачей,
по одному, с проверкой в редакторе.

### P3-3. Мусорные `using`

`using static UnityEngine.GraphicsBuffer;` в
[`NPCFacade`](../Assets/Scripts/AI/NPCFacade.cs) и
[`GatlingGun`](../Assets/Scripts/AI/GatlingGun.cs) — автодобавление IDE. Тянет в
область видимости тип `Target`, что может запутать при чтении.

Плюс `using System.Collections/Generic` в десятках файлов, которые их не
используют (наследие шаблона Unity).

### P3-4. Прочее

- `ShopController.ByeBot` — цикл `for (int i = 0; i < 1; i++)` на одну итерацию.
- `PriceTextBuilder` не показывает золото в ценнике.
- `TimeManager` выводит время без ведущего нуля: `7:5` вместо `07:05`.
- `GatlingGun.stats` публичное, но перезаписывается в `Awake`.
- `GameManager` целиком: `firstBotCreated` не используется, `firstKillMaded` никто не
  выставляет, `firstBuildCreated` никто не читает с тех пор, как умерли квесты.
- `NPCNavigation.isDead` объявлено, но нигде не используется.
- `ResourceSpawner.length` объявлено, но нигде не используется.
- `BomberBug` взводит триггер `attack1` каждый кадр, пока цель в радиусе.
