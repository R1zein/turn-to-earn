# Квесты и диалоги

> **Останки убраны 2026-10-08** по порядку из раздела
> [«Как убрать останки»](#как-убрать-останки): вызовы каналов, компоненты на
> сценах и префабах, скрипты, ассеты и пакет `com.unity.behavior`. Оставлены
> только `DialogData`, `CharacterSpeech` и `Data/Dialogs/ResourceDialog.asset` —
> как формат реплик для будущей системы. Ниже — описание того, что было, для
> истории. В `Tutorial` агент графа висел на `Main Camera`, а `BootStrap` — на
> `Directional Light`; больше в сцене ничего нет.

> **Система не работает.** Квестовый граф Unity Behavior
> (`Assets/Behaviour/Quest graph.asset`) и его blackboard (`GameVariables.asset`)
> удалены 2026-07-02 коммитом `3c6c891` («perenos»). Вместе с графом умерла вся
> квестовая система: квесты не выдаются, условия не проверяются, диалоги не
> показываются. Код и ассеты вокруг графа остались — этот документ описывает,
> что именно осталось и что из этого всё ещё исполняется.

## Как это было устроено

Логика жила в графе Unity Behavior, код давал ему кирпичи:

- **квест** — ScriptableObject с условием выполнения (`IsQuestComplited`) и
  ссылкой на реплики;
- **узлы графа** — действия (показать реплику, выдать награду, создать бота) и
  условия (квест взят, выполнен, награда выдана, реплики кончились);
- **event-каналы** — ScriptableObject-сигналы из игры в граф («бот создан», «NPC
  умер», «нажата кнопка диалога»);
- **`DialogManager`** — показ реплик в uGUI-окне.

## Что осталось

| Что | Где | Состояние |
|---|---|---|
| агент графа | `BehaviorGraphAgent` на [`Player.prefab`](../Assets/Prefabs/Player/Player.prefab) (сцена `Scene 5`) и на объекте в [`Tutorial`](../Assets/Scenes/Tutorial.unity) | оба ссылаются на удалённый граф — ничего не делают |
| переменные blackboard | переопределения в `Tutorial.unity` (3 × `Quest`, 1 × `GameObject`) | висят на мёртвом агенте |
| ассеты квестов | [`Data/Quests/`](../Assets/Data/Quests/): `FirstBotCreated.asset`, `ResourceQuest.asset` | на них никто не ссылается |
| классы квестов | [`Quest.cs`](../Assets/Scripts/Quest.cs), [`Quests/`](../Assets/Scripts/Quests/) | условия никто не проверяет |
| узлы графа | [`Behaviour Tree/Actions`](../Assets/Scripts/Behaviour%20Tree/Actions/) (3), [`Conditions`](../Assets/Scripts/Behaviour%20Tree/Conditions/) (5) | без графа не исполняются |
| окно диалога | [`DialogManager`](../Assets/Scripts/UI/DialogManager.cs) на `Canvas.prefab` | вызывался только узлом `PlayQuestAction` |
| реплики | [`DialogData`](../Assets/Scripts/UI/DialogData.cs), [`CharacterSpeech`](../Assets/Scripts/UI/CharacterSpeech.cs), ассет `Data/Dialogs/ResourceDialog.asset` | данные без потребителя |
| event-каналы | три ассета в [`Behaviour Tree/`](../Assets/Scripts/Behaviour%20Tree/) | **исполняются**, см. ниже |
| флаги прогресса | [`GameManager`](../Assets/Scripts/Player/GameManager.cs) (только в `Scene 5`) | читали только квесты |
| учебные заготовки | [`Behaviour Tree/Tutorial/`](../Assets/Scripts/Behaviour%20Tree/Tutorial/) | к игре не относились и раньше |

## Что всё ещё исполняется вхолостую

Часть кода по-прежнему срабатывает — но шлёт сигналы в пустоту: подписчиков на
события каналов в коде нет ни одного (ни одного `.Event +=`).

| Вызов | Кто и когда | Что происходит |
|---|---|---|
| `onNpcDeath.SendEventMessage()` | [`NPCFacade.Death`](../Assets/Scripts/AI/NPCFacade.cs) — смерть любого NPC | `countNPC++` в ассете, слушателей нет |
| `onBotCreated.SendEventMessage()` + `firstBotCrea6ted = true` | [`ShopController.ByeBot`](../Assets/Scripts/UI/ShopController.cs) — покупка бота | слушателей нет, флаг читал только квест |
| `onDialogButtonPressed.SendEventMessage()` | [`PlayerControll`](../Assets/Scripts/Player/PlayerControll.cs) — клавиша `X` | слушателей нет; больше этот скрипт ничего не делает |
| `GameManager.instance.firstBuildCreated = true` | [`Inventory`](../Assets/Scripts/Player/Inventory.cs) — первая постройка | флаг читал только квест |

Вреда это не приносит: каналы назначены на всех префабах, которые их вызывают
(4 NPC, `Canvas`, `Player` — проверено), так что исключений не будет. Но это
мёртвый код, который выглядит живым, и он держит в проекте пакет Unity Behavior.

> `GameManager` есть только в сцене `Scene 5`, а `Inventory` обращается к
> `GameManager.instance` без проверки. Сейчас игрок с `Inventory` тоже есть только
> в `Scene 5`, поэтому исключения нет. Но если перенести игрока в сцену без
> `GameManager`, первая же постройка упадёт с `NullReferenceException`.

## Как убрать останки

Начинать с удаления пакета `com.unity.behavior` **нельзя**. Event-каналы
наследуют `EventChannelBase` из Unity Behavior, и на их типы ссылаются игровые
скрипты `NPCFacade`, `ShopController`, `PlayerControll`. Проект перестанет
компилироваться, а агенты на префабе и в сцене превратятся в Missing Script.

Правильный порядок. Слушателей у каналов нет, так что замена не нужна — вызовы
просто удаляются:

1. Убрать вызовы каналов и поля под них из `NPCFacade` и `ShopController`,
   строку с флагом из `Inventory`.
2. **Снять компоненты со сцен и префабов**, прежде чем удалять их скрипты, иначе
   останется Missing Script:
   - `BehaviorGraphAgent` — с `Player.prefab` и из `Tutorial` (переопределения
     blackboard уйдут вместе с ним);
   - `PlayerControll` — с `Player.prefab`;
   - `DialogManager` — с `Canvas.prefab` (и его панель диалога);
   - `GameManager` — из `Scene 5`.
3. Удалить скрипты и ассеты: `Behaviour Tree/` целиком (узлы, каналы, их ассеты,
   учебные заготовки), `Quest.cs`, `Quests/`, `Data/Quests/`, `PlayerControll`,
   `DialogManager`, `GameManager`.
4. Удалить пакет `com.unity.behavior` из `Packages/manifest.json`.
5. **Проверка:** компиляция без ошибок, консоль чистая; в `Scene 5` — смерть NPC,
   покупка бота, первая постройка; в `Tutorial` — сцена открывается без Missing
   Script.

`DialogData` и `CharacterSpeech` — удачный формат реплик (имя, портрет, текст). Их
стоит оставить или удалить вместе с остальным — это решение для новой системы.

## Два источника правды о прогрессе

Пока квесты работали, одинаковые по смыслу флаги хранились в разных местах:

| Событие | Где флаг |
|---|---|
| первая постройка | `GameManager.instance.firstBuildCreated` |
| первое убийство | `GameManager.instance.firstKillMaded` (никто не выставляет) |
| первый бот | `OnBotCreated.firstBotCrea6ted` — в ScriptableObject |

Сейчас их никто не читает. В новой системе прогресс собирается в одном месте —
сервисе `GameProgress`, см. [Архитектуру](architecture.md#квесты).

## Новая система квестов

Не спроектирована. Когда до неё дойдёт очередь, её делают по
[правилам архитектуры](architecture.md#квесты): конфиг квеста — ScriptableObject
только для чтения, состояние — в сервисе сцены, условия — обычные C#-классы,
подписанные на события сервисов, окно диалога — контроллер на UI Toolkit. Каким
будет сам механизм выдачи и связки квестов, план не предрешает.
