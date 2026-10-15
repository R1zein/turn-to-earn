# Интерфейс

Весь интерфейс — на **UI Toolkit**; uGUI-канвасов в сценах и префабах нет
(переход закончен на [этапе 7](architecture-plan.md#этап-7-интерфейс)). Правила
слоёв, контроллеров и стилей — в [архитектуре](architecture.md#ui-toolkit); здесь —
как это устроено в проекте.

## Файлы

`Assets/UI Toolkit/`:

| Файл | Что это |
|---|---|
| `MainStyle.uss` | палитра в `:root` и утилитарные классы — единственная таблица стилей |
| `GameMain.uxml` | корень `Scene 5`: слои `WorldMarkers`, `Hud`, `BuildPanel`, `ShopPanel` |
| `DroneMain.uxml` | корень `SampleScene`: слои `WorldMarkers`, `DroneStats`, ромб центра, прицел `Scope` |
| `Hud.uxml` | прицел, ресурсы, часы, здоровье игрока |
| `BuildPanel.uxml` | меню строительства |
| `ShopPanel.uxml` | магазин: вкладки BOTS и BASE |
| `DroneStats.uxml` | телеметрия дрона |
| `WorldMarkers.uxml` | пустой слой для полосок здоровья над объектами |
| `Templates/ItemCard.uxml` | карточка выбора: иконка, название, цена (бот, постройка, база, часть базы) |
| `Templates/HealthMarker.uxml` | полоска здоровья над объектом |
| `PanelSettings.asset` | масштаб **ScaleWithScreenSize 1920×1080**, по ширине — как был `CanvasScaler` |

Шаблоны динамических элементов собраны в ассете
[`Data/UITemplate.asset`](../Assets/Scripts/Config/UITemplate.cs), он в
`ProjectContext`. Что предлагают панели (боты, постройки, места под базу, части
базы — с иконками и названиями) — в `GameConfig`, секция Shop and building.

## Слои

На сцену — один `UIDocument` с корневым UXML. Каждый слой — `<ui:Instance>` с
классом `.fill` и `picking-mode="Ignore"`; порядок в файле — порядок отрисовки.
Панели (`BuildPanel`, `ShopPanel`) скрыты классом `.hidden`; открытая панель
снимает его и просит у `InputService` режим `Ui` — курсор свободен, игровые
клавиши выключены. Закрываются панели своей кнопкой, `Esc` или `Tab`.

Стиль подключён и в корневом UXML, и в каждом слое: таблица слоя нужна UI
Builder для предпросмотра, а корневая — элементам, созданным из шаблонов в
рантайме (карточку кладут в панель без обёртки шаблона).

## Контроллеры

Обычные C#-классы в контейнере; элементы ищут в `Initialize` по `name`, без
null-проверок.

| Контроллер | Сцена | Что делает |
|---|---|---|
| `ResourcesHudController` | Scene 5 | запасы из `ResourceWallet.OnChanged` |
| `ClockHudController` | Scene 5 | часы из `TimeManager.OnClockChanged` (минуты без нуля: `7:5`) |
| `PlayerHealthHudController` | Scene 5 | здоровье игрока по `OnDamage` |
| `BuildPanelController` | Scene 5 | `Tab` → меню; карточка → `BuildService.Pick`; недоступные по цене приглушены |
| `ShopPanelController` | Scene 5 | открывает стол магазина; боты → `ShopService`, база → `FortificationService` |
| `WorldMarkersController` (`ILateTickable`) | обе | полоска над каждым `HealthBarAnchor`, см. ниже |
| `DroneHudController` (`ITickable`) | SampleScene | прицел за курсором, телеметрия дрона |

`ItemCards` — общий помощник панелей: делает карточку из шаблона, заполняет
иконку, название и цену (`WOOD 10  STONE 20  IRON 80` или `FREE`).

## Метки над объектами

Полоска здоровья — экранный элемент, а не world-space канвас: каждый кадр
`WorldMarkersController` проецирует точку `HealthBarAnchor` через
`RuntimePanelUtils.CameraTransformWorldToPanel`, прячет полоску за спиной камеры и
дальше `GameConfig.HealthBarDistance` (30 м) и выставляет ширину заливки по
`currHP / maxHP`.

`HealthBarAnchor` стоит на префабе `HPbar/Centre`, вложенном в зомби, паука, обоих
ботов и турель; сам добавляется в сервис `HealthBars` в `OnEnable`.

## Стиль

Палитра (`:root` в `MainStyle.uss`):

```css
--accent:      rgb(0, 255, 213);        /* неоновая бирюза */
--accent-dim:  rgba(0, 255, 213, 0.3);  /* тонкая рамка */
--accent-line: rgba(0, 255, 213, 0.25); /* разделители */
--hot:         rgb(255, 46, 136);       /* розовый акцент */
--muted:       rgb(170, 225, 240);      /* подписи */
--surface:     rgba(4, 10, 16, 0.82);   /* тёмная подложка */
```

Классы — только утилитарные: `.fill .row .center .hidden`, `.bg-surface .bg-accent
.bg-hot`, `.frame .frame-lit-left .divider-bottom .outline-accent .outline-hot
.round`, `.text-accent .text-hot .text-muted`, `.text-sm/md/lg/xl .bold .heavy .tracking
.tracking-wide`, `.btn` (с `:hover :active :disabled`) и `.btn-on` — выбранная
вкладка. Размеры, позиции и отступы конкретного элемента — inline в UXML.
Классов под одну панель (`.shop-panel__title`) не заводим.

> В inline-стилях UXML не пишем `var(...)` — цвета берутся классами.

### Шрифт и читаемость

Шрифт — `Fonts/LiberationSans SDF.asset` (SDF-ассет TextCore из
`LiberationSans.ttf`, лицензия OFL рядом), задан в `:root`. Там же для **всего**
текста: жирное начертание, тёмная обводка каждой буквы
(`-unity-text-outline-width: 2px`) и мягкая тень — текст читается на ярком небе
и песке.

- Обводку и тень UI Toolkit рисует **только у SDF-шрифта**. Шрифт темы по
  умолчанию их молча игнорирует — поэтому свой ассет обязателен.
- Подписи (`STONE`, `SPEED`, `HEALTH`...) — класс `.heavy`: обводка в цвет самих
  букв утолщает штрих, жёсткая тень отделяет от фона. Цифры его не получают.
  Синтетический жирный (`m_BoldStyleWeight` ассета) почти не утолщает буквы —
  проверено, не помогает.
- Узкие (condensed) шрифты вроде BebasNeue не берём — их трудно читать.
- Размеры: `.text-sm` 24, `.text-md` 30, `.text-lg` 38, `.text-xl` 56, `.btn` 30 —
  под масштаб панели 1920×1080.

## Работа из кода

1. **Экранные координаты ≠ координаты панели.** У экрана Y растёт снизу, у панели
   — сверху, плюс панель масштабируется:
   ```csharp
   RuntimePanelUtils.ScreenToPanel(panel, new Vector2(p.x, Screen.height - p.y));
   ```
   Для точки мира — `RuntimePanelUtils.CameraTransformWorldToPanel`.
2. **`using UnityEngine.UIElements` конфликтует с `Cursor`.** Курсором владеет
   только `InputService`, где стоит алиас `using Cursor = UnityEngine.Cursor;`.
3. **В UI — `Time.unscaledDeltaTime`.** Иначе анимации меню застынут на паузе.
4. **Live Reload в редакторе.** Если поменять UXML/USS во время play mode,
   `UIDocument` пересоберёт дерево, а контроллеры останутся со ссылками на старые
   элементы — HUD перестанет обновляться до перезапуска play mode. В билде этого нет.

## Редактирование в UI Builder

UI Builder по умолчанию пишет стили **инлайном в UXML** — для правок, относящихся
к одному элементу, это и есть правильное место. Повторяющийся стиль — утилитарный
класс в `MainStyle.uss`: сделай таблицу активной (StyleSheets → сделать активной)
и навешивай класс, а не заполняй поля инспектора.

## EventSystem

В каждой сцене есть `EventSystem` с `InputSystemUIInputModule` — через него UI
Toolkit получает клики. **Не удалять**, хотя uGUI-канвасов больше нет.
