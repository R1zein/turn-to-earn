# Квесты и диалоги

Квестовая логика собрана не в коде, а в графах **Unity Behavior**. C#-классы здесь
— это кирпичи (условия и действия), из которых граф складывается в редакторе.

## Квест

[`Quest`](../Assets/Scripts/Quest.cs) — абстрактный ScriptableObject:

```csharp
public bool rewardGained;     // награда выдана
public DialogData dialogData; // реплики
public bool IsTaken;          // квест взят
public int currentIndex;      // на какой реплике остановились
public abstract bool IsQuestComplited();
```

Наследники (каждый — свой ассет через `CreateAssetMenu`):

| Квест | Условие выполнения |
|---|---|
| [`FirstBotQuest`](../Assets/Scripts/Quests/FirstBotQuest.cs) | `onBotCreated.firstBotCrea6ted` |
| [`FirstBuildQuest`](../Assets/Scripts/Quests/FirstBuildQuest.cs) | `GameManager.instance.firstBuildCreated` |
| [`FirstKillQst`](../Assets/Scripts/Quests/FirstKillQst.cs) | `GameManager.instance.firstKillMaded` |
| [`ResourcesQuest`](../Assets/Scripts/Quests/ResourcesQuest.cs) | набрано `stone`/`tree`/`iron` |

[`KillingQuest.cs`](../Assets/Scripts/Quests/KillingQuest.cs) — пустой файл с одной
закомментированной строкой, можно удалять.

> **Главная проблема квестов:** `IsTaken`, `rewardGained` и `currentIndex` —
> поля ScriptableObject, то есть **сохраняются в файл ассета**. Пройдя квест один
> раз в редакторе, ты оставляешь его «взятым и завершённым» навсегда: при
> следующем запуске он уже выполнен. Это надо чинить до того, как квестов станет
> много — см. [Известные проблемы](known-issues.md#p1-3-состояние-игры-живёт-в-ассетах).

## Диалоги

[`DialogData`](../Assets/Scripts/UI/DialogData.cs) — ScriptableObject со списком
[`CharacterSpeech`](../Assets/Scripts/UI/CharacterSpeech.cs):

```csharp
public string CharacterName;
public Sprite CharacterSprite;
[TextArea] public string Speech;
```

[`DialogManager`](../Assets/Scripts/UI/DialogManager.cs) показывает их по одной.
`StartDialog(quest)` увеличивает `quest.currentIndex` и выводит соответствующую
реплику; когда индекс перевалил за длину массива — панель закрывается.

Перелистывание: [`PlayerControll`](../Assets/Scripts/Player/PlayerControll.cs)
ловит клавишу `X` и шлёт событие `onDialogButtonPressed`, на которое реагирует
граф.

Диалоговое окно — на старом uGUI (TMP + `Image`), см. [Интерфейс](ui.md).

## Event-каналы

Мост между игровым кодом и графами Unity Behavior. Три канала, все —
ScriptableObject-ассеты:

| Канал | Кто шлёт | Что несёт |
|---|---|---|
| [`OnBotCreated`](../Assets/Scripts/Behaviour%20Tree/EventChannel/OnBotCreated.cs) | `ShopController` | + флаг `firstBotCrea6ted` |
| [`OnNpcDeath`](../Assets/Scripts/Behaviour%20Tree/EventChannel/OnNpcDeath.cs) | `NPCFacade.Death` | + счётчик `countNPC` |
| [`OnDialogButtonPressed`](../Assets/Scripts/Behaviour%20Tree/EventChannel/OnDialogButtonPressed.cs) | `PlayerControll` | — |

Каналы задуманы как сигналы, но в два из них **дописали состояние** — булев флаг
и счётчик. Это то же самое сохраняемое-в-ассет состояние, что и у квестов:
`countNPC` накапливается между сессиями.

## Узлы графа

### Действия (`Action`)

| Узел | Что делает |
|---|---|
| [`PlayQuestAction`](../Assets/Scripts/Behaviour%20Tree/Actions/PlayQuestAction.cs) | помечает квест взятым, показывает реплику через `DialogManager` |
| [`RewardGainedAction`](../Assets/Scripts/Behaviour%20Tree/Actions/RewardGainedAction.cs) | ставит `rewardGained = true` |
| [`CreateBotAction`](../Assets/Scripts/Behaviour%20Tree/Actions/CreateBotAction.cs) | спавнит `BotDefender` у стола |

### Условия (`Condition`)

| Узел | Проверяет |
|---|---|
| [`CheckQuestCompletionCondition`](../Assets/Scripts/Behaviour%20Tree/Conditions/CheckQuestCompletionCondition.cs) | `IsQuestComplited()` |
| [`CheckQuestRewardCondition`](../Assets/Scripts/Behaviour%20Tree/Conditions/CheckQuestRewardCondition.cs) | `rewardGained` |
| [`QuestTakenCondition`](../Assets/Scripts/Behaviour%20Tree/Conditions/QuestTakenCondition.cs) | `IsTaken` |
| [`HasLastDialogCondition`](../Assets/Scripts/Behaviour%20Tree/Conditions/HasLastDialogCondition.cs) | дошли ли до конца реплик |
| [`KeyPressedCondition`](../Assets/Scripts/Behaviour%20Tree/Conditions/KeyPressedCondition.cs) | **сломан** |

> `KeyPressedCondition` всегда возвращает `true` независимо от клавиши — он лишь
> пишет в консоль «pressed»/«not pressed». Это отладочная заготовка; если граф на
> него опирается, ветка срабатывает всегда.

`PlayQuestAction` и `CreateBotAction` ищут `DialogManager` и `TableInterract`
через `FindAnyObjectByType` при каждом запуске узла — дорого и ломается молча,
если объекта нет на сцене.

## Два источника правды о прогрессе

Одинаковые по смыслу флаги хранятся в разных местах:

| Событие | Где флаг |
|---|---|
| Первая постройка | `GameManager.instance.firstBuildCreated` |
| Первое убийство | `GameManager.instance.firstKillMaded` |
| Первый бот | `OnBotCreated.firstBotCrea6ted` (ScriptableObject!) |

При этом в `GameManager` есть и неиспользуемое поле `firstBotCreated` — то есть
задумывалось единообразно, но бот поехал по другому пути.

Чинить вместе с выносом состояния из ассетов, см.
[Архитектурный план](architecture-plan.md#этап-2-состояние-игры-из-ассетов-в-рантайм).

## Учебные заготовки

Папка [`Behaviour Tree/Tutorial/`](../Assets/Scripts/Behaviour%20Tree/Tutorial/) —
упражнения по паттернам (`Node`, `Leaf`, `IStrategy`, `Animal`/`Cat`/`Dog`/`Mouse`,
`BootStrap`). К игре отношения не имеют. Удалять можно целиком, когда перестанут
быть нужны как справочник.
