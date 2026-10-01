# Интерфейс

Проект **в середине перехода** с uGUI на UI Toolkit. Новый HUD дрона уже на UI
Toolkit, остальной интерфейс — старый uGUI + TextMeshPro.

Целевые правила (слои, контроллеры, стили) — в разделе
[UI Toolkit архитектуры](architecture.md#ui-toolkit). Этот документ описывает,
**что есть сейчас** и как оно ложится на цель.

## UI Toolkit (новое)

### Файлы

| Файл | Назначение |
|---|---|
| [`Main.uxml`](../Assets/UI%20Toolkit/Main.uxml) | корневой документ HUD |
| [`DroneStats.uxml`](../Assets/UI%20Toolkit/DroneStats.uxml) | панель телеметрии (шаблон) |
| [`Cyberpunk.uss`](../Assets/UI%20Toolkit/Cyberpunk.uss) | стиль панелей |
| `PanelSettings.asset` | настройки панели |

`PanelSettings` настроен на эталонное разрешение **1920×1080** со
`ScreenMatchMode` по ширине — это повторяет старый `CanvasScaler`, чтобы элементы
не поменяли размер при переносе.

### Структура Main.uxml

```
Main.uxml
├── <Template DroneStats>        подключённый шаблон
├── DroneStatsPanel (Instance)   растянут на весь экран, picking-mode Ignore
├── Scope          (100×100)     прицел, едет за курсором
└── ScopeOverlay   (150×150)     второй прицел, статичный
```

Это уже начало [слоистой схемы](architecture.md#слои): панель подключена как
`<ui:Instance>`, растянута и прозрачна для мыши. Чего не хватает до цели:

- у самого `Main.uxml` **нет своего `<Style>`** — стили из шаблона не доходят до
  прямых детей корня (сейчас это прицелы; они стилизованы inline, поэтому пока не
  ломается);
- прицелы лежат прямо в корне, а не в своём слое `Hud`.

> Обёртка шаблона (`TemplateContainer`) **обязана быть растянута** на весь экран
> (`position: absolute` + нули по краям). По умолчанию она схлопывается в нулевой
> размер, и панель внутри позиционируется относительно точки, а не экрана.

### Киберпанк-стиль

Палитра, которая остаётся при любом переходе:

```css
--cyber-accent:      rgb(0, 255, 213);        /* неоновая бирюза */
--cyber-accent-dim:  rgba(0, 255, 213, 0.3);  /* тонкая рамка */
--cyber-accent-line: rgba(0, 255, 213, 0.25); /* разделители */
--cyber-hot:         rgb(255, 46, 136);       /* розовый акцент */
--cyber-caption:     rgba(126, 196, 214, 0.75);
--cyber-surface:     rgba(4, 10, 16, 0.82);   /* тёмная подложка */
```

**Сейчас** [`Cyberpunk.uss`](../Assets/UI%20Toolkit/Cyberpunk.uss) построен на
BEM-классах — `.cyber-panel`, `.cyber-panel__header`, `.cyber-row__value--hot` и
т.д. Это **расходится с правилом** «утилитарные классы + inline, классов под
конкретную панель нет» ([стили](architecture.md#стили-и-шаблоны)).

**Цель** (этап 7 плана): палитра переезжает в `:root` общего `MainStyle.uss`,
BEM-классы раскладываются на утилитарные, а всё, что принадлежит конкретному
элементу, уходит inline в UXML:

| Сейчас (BEM) | Станет утилитами | Уходит inline в UXML |
|---|---|---|
| `.cyber-panel` | `.bg-surface .frame .frame-lit-left` | `width: 300px`, `padding` |
| `.cyber-panel--anchor-bottom-left` | — | `position: absolute; left: 40px; bottom: 40px` |
| `.cyber-panel__header` | `.row .divider-bottom` | `margin-bottom`, `padding-bottom` |
| `.cyber-panel__title` | `.text-accent .text-md .bold .tracking-wide` | — |
| `.cyber-panel__dot` | `.bg-hot` | `width: 8px; height: 8px` |
| `.cyber-row` | `.row` | `align-items: flex-end; margin-bottom` |
| `.cyber-row__caption` | `.text-muted .text-sm .tracking` | — |
| `.cyber-row__value` | `.text-accent .text-lg .bold` | — |
| `.cyber-row__value--hot` | `.text-hot .text-lg .bold` | — |

**До этапа 7** в новых панелях не заводим новых BEM-классов: переиспользуем
существующие или пишем inline. Тогда перевод останется механическим.

### Работа из кода

Элементы ищутся **по имени** через `rootVisualElement.Q<T>("Имя")`. Имена
задаются в UXML атрибутом `name`.

> В UI Builder имена показываются с решёткой (`#SpeedValue`) — это нотация
> ID-селектора, **частью имени решётка не является**. В UXML и в коде пишется
> просто `SpeedValue`.

**Цель:** контроллер — обычный C#-класс, элементы ищутся в `Initialize`, **без
null-проверок** — опечатка в имени должна упасть сразу ([почему](architecture.md#ловушки)).
Zenject вызывает `Initialize` после того, как `UIDocument` построил дерево.

**Сейчас** `DroneControl` и `DroneStatsController` — `MonoBehaviour`, которые
ищут элементы лениво в первом `Update` и проверяют их на `null`. Это обходной путь
доконтейнерной эпохи: `UIDocument` строит дерево в своём `OnEnable`, который мог
отработать позже `OnEnable` скрипта. С переходом на Zenject оба приёма убираются.

Правила, которые остаются в силе при любой архитектуре:

1. **Экранные координаты ≠ координаты панели.** У экрана Y растёт снизу, у панели
   — сверху, плюс панель имеет своё масштабирование:
   ```csharp
   RuntimePanelUtils.ScreenToPanel(panel, new Vector2(p.x, Screen.height - p.y));
   ```
   Для точки мира — `RuntimePanelUtils.CameraTransformWorldToPanel`.
2. **`using UnityEngine.UIElements` конфликтует с `Cursor`.** В UI Toolkit есть
   свой тип `Cursor`, и `Cursor.lockState` перестаёт компилироваться. Лечится
   алиасом: `using Cursor = UnityEngine.Cursor;`
3. **В UI — `Time.unscaledDeltaTime`.** Иначе анимации меню застынут на паузе.

### Редактирование в UI Builder

UI Builder по умолчанию пишет стили **инлайном в UXML** — для правок, относящихся
к одному элементу, это и есть правильное место. Если же правка — повторяющийся
стиль, её место в утилитарном классе общей таблицы: сделай таблицу активной
(StyleSheets → сделать активной) и навешивай класс, а не заполняй поля
инспектора.

## uGUI (старое)

Что ещё на старом стеке и во что превращается:

| Скрипт | Что показывает | Станет |
|---|---|---|
| [`StoredResources`](../Assets/Scripts/Player/StoredResources.cs) | четыре TMP-поля с запасами | `ResourcesHudController` |
| [`TimeManager`](../Assets/Scripts/TimeManager.cs) | TMP-поле с часами | `TimeUIController` |
| [`CanvasController`](../Assets/Scripts/UI/CanvasController.cs) | мировые полоски HP (`Image.fillAmount`) | `WorldMarkersController` |
| [`PhpBarController`](../Assets/Scripts/UI/PhpBarController.cs) | полоска HP через `Slider` | `WorldMarkersController` |
| [`DialogManager`](../Assets/Scripts/UI/DialogManager.cs) | окно диалога | `DialogPanelController` |
| [`ShopController`](../Assets/Scripts/UI/ShopController.cs) | панели магазина | `ShopService` + `ShopPanelController` |
| [`Inventory`](../Assets/Scripts/Player/Inventory.cs) | панель строительства | `BuildService` + `BuildPanelController` |
| [`PriceTextBuilder`](../Assets/Scripts/Player/PriceTextBuilder.cs) | ценник постройки | рендерер варианта постройки из шаблона |

Порядок перевода — в [плане](architecture-plan.md#этап-7-интерфейс).

### Мировые полоски здоровья

Раньше здесь было записано исключение «оставить мировые полоски на uGUI». Целевая
архитектура решает это иначе, и **исключение снято**: метка над объектом — обычный
экранный элемент, который каждый кадр встаёт в спроецированную точку. Текст
читается одинаково на любом расстоянии, а интерфейс остаётся на одном стеке.
Механика — в [привязке UI к миру](architecture.md#привязка-ui-к-объектам-мира).

### Курсор

Старые панели управляют курсором через `Cursor.lockState` из трёх мест (см.
[Строительство](building.md#курсор-и-блокировка-ввода)). В цели режимом курсора
владеет только `InputService`: панель, держащая мышь, просит режим, а не ставит
его сама. Переносить панели на UI Toolkit, сохраняя старые вызовы `Cursor`, не
нужно — это та же каша на новом стеке.

## EventSystem

В сцене есть `EventSystem` со `StandaloneInputModule` — через него UI Toolkit
получает ввод в рантайме. **Не удалять**, даже когда исчезнет последний
uGUI-канвас. При переходе на Input System ([этап 4](architecture-plan.md#этап-4-ввод))
модуль меняется на `InputSystemUIInputModule`.
