# Интерфейс

Проект **в середине перехода** с uGUI на UI Toolkit. Новый HUD дрона уже на UI
Toolkit, весь остальной интерфейс — старый uGUI + TextMeshPro.

## UI Toolkit (новое)

### Файлы

| Файл | Назначение |
|---|---|
| [`Main.uxml`](../Assets/UI%20Toolkit/Main.uxml) | корневой документ HUD |
| [`DroneStats.uxml`](../Assets/UI%20Toolkit/DroneStats.uxml) | панель телеметрии (шаблон) |
| [`Cyberpunk.uss`](../Assets/UI%20Toolkit/Cyberpunk.uss) | стиль панелей |
| `PanelSettings.asset` | настройки панели |

`PanelSettings` настроен на эталонное разрешение **1920×1080** со
`ScreenMatchMode` по ширине — это повторяет настройки старого `CanvasScaler`,
чтобы элементы не поменяли размер при переносе.

### Структура Main.uxml

```
Main.uxml
├── <Template DroneStats>        подключённый шаблон
├── DroneStatsPanel (Instance)   растянут на весь экран
├── Scope          (100×100)     прицел, едет за курсором
└── ScopeOverlay   (150×150)     второй прицел, статичный
```

> Обёртка шаблона (`TemplateContainer`) **обязана быть растянута** на весь экран
> (`position: absolute` + нули по краям). По умолчанию она схлопывается в нулевой
> размер, и панель внутри позиционируется относительно точки, а не экрана.

### Киберпанк-стиль

[`Cyberpunk.uss`](../Assets/UI%20Toolkit/Cyberpunk.uss) — переиспользуемый набор
классов. Палитра вынесена в переменные на `.cyber-panel` и наследуется вниз:

```css
--cyber-accent: rgb(0, 255, 213);    /* неоновая бирюза */
--cyber-hot: rgb(255, 46, 136);      /* розовый акцент */
--cyber-surface: rgba(4, 10, 16, 0.82);
```

| Класс | Роль |
|---|---|
| `.cyber-panel` | сама панель: фон, неоновая грань слева, скругление |
| `.cyber-panel--anchor-bottom-left` | позиционирование (вынесено отдельно) |
| `.cyber-panel__header` / `__title` / `__dot` | шапка |
| `.cyber-row` + `__caption` / `__value` | строка данных |
| `.cyber-row__value--hot` | значение розовым |

Позиционирование намеренно отделено от внешнего вида — чтобы вторую панель можно
было поставить в другой угол, не переопределяя стиль.

**Чтобы сделать новую панель в этом же стиле:** возьми разметку `DroneStats.uxml`
как образец, навесь те же классы, подключи `Cyberpunk.uss` через `<Style src=...>`
и добавь свой модификатор-якорь в USS.

### Работа из кода

Элементы ищутся **по имени** через `UIDocument.rootVisualElement.Q<T>("Имя")`.
Имена задаются в UXML атрибутом `name`.

> В UI Builder имена показываются с решёткой (`#SpeedValue`) — это нотация
> ID-селектора, **частью имени решётка не является**. В UXML и в коде пишется
> просто `SpeedValue`.

Два правила, выведенных на практике:

1. **Искать элементы лениво, а не в `OnEnable`.** `UIDocument` строит дерево в
   своём `OnEnable`, который может отработать позже твоего. Оба контроллера
   (`DroneControl`, `DroneStatsController`) резолвят элементы в первом `Update`,
   где `rootVisualElement` уже не `null`.
2. **Экранные координаты ≠ координаты панели.** У экрана Y растёт снизу, у панели
   — сверху, плюс панель имеет собственное масштабирование. Перевод:
   ```csharp
   RuntimePanelUtils.ScreenToPanel(panel, new Vector2(p.x, Screen.height - p.y));
   ```
3. **`using UnityEngine.UIElements` конфликтует с `Cursor`.** В UI Toolkit есть
   свой тип `Cursor`, и `Cursor.lockState` перестаёт компилироваться. Лечится
   алиасом: `using Cursor = UnityEngine.Cursor;`

### Редактирование в UI Builder

UI Builder по умолчанию пишет стили **инлайном в UXML**. Чтобы правки попадали в
`Cyberpunk.uss`, в UI Builder нужно сделать эту таблицу активной (StyleSheets →
сделать активной) и работать через классы, а не через поля инспектора.

## uGUI (старое)

Что ещё на старом стеке:

| Скрипт | Что показывает |
|---|---|
| [`StoredResources`](../Assets/Scripts/Player/StoredResources.cs) | четыре TMP-поля с запасами |
| [`TimeManager`](../Assets/Scripts/TimeManager.cs) | TMP-поле с часами |
| [`CanvasController`](../Assets/Scripts/UI/CanvasController.cs) | мировые полоски HP (`Image.fillAmount`) |
| [`PhpBarController`](../Assets/Scripts/UI/PhpBarController.cs) | полоска HP через `Slider` |
| [`DialogManager`](../Assets/Scripts/UI/DialogManager.cs) | окно диалога |
| [`ShopController`](../Assets/Scripts/UI/ShopController.cs) | панели магазина (`GameObject.SetActive`) |
| [`Inventory`](../Assets/Scripts/Player/Inventory.cs) | панель строительства |
| [`PriceTextBuilder`](../Assets/Scripts/Player/PriceTextBuilder.cs) | ценник постройки |

### Что переносить, а что нет

**Переносить на UI Toolkit** стоит экранный интерфейс: запасы ресурсов, часы,
магазин, панель строительства, диалоги. Порядок — в
[Архитектурном плане](architecture-plan.md#этап-6-интерфейс).

**Оставить на uGUI** имеет смысл мировые полоски здоровья над NPC
(`CanvasController`): UI Toolkit плохо приспособлен к world-space элементам,
привязанным к объектам сцены. Это осознанное исключение, а не недоделка.

### Проблема смешанного ввода

Старые панели управляют курсором через `Cursor.lockState` из трёх разных мест
(см. [Строительство](building.md#курсор-и-блокировка-ввода)). При переносе на UI
Toolkit это надо решить **один раз**, а не перенести как есть.

## EventSystem

В сцене остаётся объект `EventSystem` со `StandaloneInputModule` — он нужен
UI Toolkit для маршрутизации ввода в рантайме. **Не удалять**, даже когда
последний uGUI-канвас исчезнет.
