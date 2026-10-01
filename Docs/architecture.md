# Архитектура: Zenject + UI Toolkit

Целевая архитектура проекта. Источник — документ «Архитектура на Zenject + UI
Toolkit» (принципы из проекта NN), здесь он переложен на turn-to-earn: на его
сцены, подсистемы и классы.

> **Статус.** Текущий код этим правилам **не соответствует** — переход идёт по
> [Архитектурному плану](architecture-plan.md). Zenject в проект ещё не установлен
> (это [этап 2](architecture-plan.md#этап-2-каркас-zenject)). Код ниже —
> иллюстрации, компиляцией не проверены; имена классов (`ResourceWallet`,
> `LevelAnchors`, ...) — предложения, а не готовые типы.

## Принципы

1. **Один граф на сцену.** Сервисы, UI-контроллеры и игровая логика — обычные
   C#-классы, зарегистрированные в инсталлере. `MonoBehaviour` остаётся только
   там, где без него нельзя: тело в мире, коллайдер, `Rigidbody`, `Animator`,
   `NavMeshAgent`, точка спавна.
2. **Кто кого знает — решает инсталлер, а не код.** Класс объявляет, что ему
   нужно (`[Inject]`), и не ищет сам: никаких `FindAnyObjectByType`,
   `GameObject.Find`, `instance`, статических менеджеров. Сейчас в проекте поиск
   по сцене стоит в 8 файлах, синглтонов два — всё это уходит.
3. **Зависимости направлены вниз.** UI знает о сервисах, сервисы не знают об UI.
   Тело знает о сервисах, сервисы знают о телах только через реестр или фабрику.
4. **Порядок регистрации = порядок инициализации.** Это инструмент, а не
   случайность, см. [Порядок инициализации](#порядок-инициализации).

## Контексты и инсталлеры

Два уровня контейнера. `ProjectContext` живёт всю сессию и переживает смену сцен;
`SceneContext` рождается и умирает вместе со сценой. Сценовый контейнер —
дочерний: он видит всё проектное, а проектный сценовое — нет.

| Уровень | Где лежит | Что туда кладём |
|---|---|---|
| Project | `Resources/ProjectContext.prefab` + `ProjectInstaller` | то, что не должно теряться между сценами: конфиги-SO (`GameConfig`, `UITemplate`), `Controls` (Input System), настройки, звук, экран загрузки — **по мере появления** |
| Scene | объект со `SceneContext` + инсталлер сцены | состояние игры (`ResourceWallet`, `GameProgress`, `QuestProgress`), время, ввод, спавнеры и реестры, все UI-контроллеры, `GameStarter` |

Для наших сцен общая часть выносится в базовый инсталлер, конкретная сцена
наследует его и добавляет своё:

```
GameplayInstaller          — всё, что не знает, какая это карта
├── Scene5Installer        — основная карта FPS: игрок, ресурсы, магазин; её якоря
├── SampleSceneInstaller   — сцена дрона
└── TutorialInstaller      — обучение (сейчас в нём только мёртвый агент графа)
```

Роли сцен взяты из их содержимого: игрок, спавнеры ресурсов и `Canvas` с
магазином есть только в `Scene 5`, дрон — только в `SampleScene`. В Build
Settings сейчас не добавлена ни одна сцена.

```csharp
// Порядок строк важен:
//  1. Реестры и спавнеры — раньше всех, кто в Initialize регистрирует или спавнит.
//  2. TimeManager — раньше подписчиков на время (DayCycle, WaveService, TimeUIController).
//  3. Сервисы состояния — раньше UI-контроллеров, которые их рисуют.
//  4. GameStarter — последним.
public class GameplayInstaller : MonoInstaller
{
    [SerializeField] private GameConfig _config;

    public override void InstallBindings()
    {
        Container.Bind<GameConfig>().FromInstance(_config).AsSingle();
        Container.Bind<Camera>().FromComponentInHierarchy().AsSingle();
        Container.Bind<UIDocument>().FromComponentInHierarchy().AsSingle();
        Container.Bind<LevelAnchors>().FromComponentInHierarchy().AsSingle();

        Container.Bind<TargetRegistry>().AsSingle();
        Container.Bind<EnemySpawner>().AsSingle();

        Container.BindInterfacesAndSelfTo<TimeManager>().AsSingle();
        Container.BindInterfacesAndSelfTo<InputService>().AsSingle();
        Container.Bind<ResourceWallet>().AsSingle();
        Container.Bind<GameProgress>().AsSingle();

        Container.BindInterfacesAndSelfTo<ResourcesHudController>().AsSingle();
        Container.BindInterfacesAndSelfTo<TimeUIController>().AsSingle();

        Container.BindInterfacesTo<GameStarter>().AsSingle();
    }
}
```

**Сериализуемые поля инсталлера — единственное место, где ассеты из инспектора
попадают в граф.** Остальные классы получают конфиг через `[Inject]`, а не через
`[SerializeField]`. Исключения — карта уровня (`LevelAnchors`) и физические
настройки тела, которые по смыслу принадлежат префабу.

## Как заводятся зависимости

Правило одно: **`[Inject]` на полях (или на методе), никогда через
конструктор.** Одинаково для обычных классов и для `MonoBehaviour`. Все
зависимости видны в шапке класса столбиком; новая зависимость — одна строка.

```csharp
public class ResourcesHudController : IInitializable, IDisposable
{
    [Inject] private UIDocument     _document;
    [Inject] private ResourceWallet _wallet;

    private Label _ore, _tree, _iron, _gold;

    public void Initialize()
    {
        var root = _document.rootVisualElement;
        _ore  = root.Q<Label>("OreValue");
        _tree = root.Q<Label>("TreeValue");
        _iron = root.Q<Label>("IronValue");
        _gold = root.Q<Label>("GoldValue");

        _wallet.OnChanged += Redraw;
        Redraw(_wallet.Current);
    }

    public void Dispose() => _wallet.OnChanged -= Redraw;

    private void Redraw(AllResources r)
    {
        _ore.text = r.ore.ToString();
        _tree.text = r.tree.ToString();
        _iron.text = r.iron.ToString();
        _gold.text = r.gold.ToString();
    }
}
```

### Словарь привязок

| Привязка | Когда | Пример в turn-to-earn |
|---|---|---|
| `Bind<T>().AsSingle()` | класс без жизненного цикла, его только зовут | `ResourceWallet`, `GameProgress`, `TargetRegistry` |
| `BindInterfacesAndSelfTo<T>().AsSingle()` | класс реализует `IInitializable` / `ITickable` / `IDisposable` — без этого Zenject не узнает, что его надо инициализировать и тикать | `TimeManager`, `InputService`, все UI-контроллеры |
| `BindInterfacesTo<T>()` | класс никто не должен звать напрямую | `GameStarter` |
| `Bind<T>().FromInstance(x)` / `BindInstance(x)` | ScriptableObject или ассет из поля инсталлера | `GameConfig`, `UITemplate` |
| `Bind<T>().FromComponentInHierarchy()` | один известный `MonoBehaviour`, уже стоящий в сцене | `UIDocument`, `Camera`, `LevelAnchors` |
| `Bind<Base>().To<Impl>().FromResolve()` | сцена подставляет свою реализацию под абстрактный тип, не создавая второй экземпляр | `TutorialInstaller` подменяет правила волн |
| несколько `Bind<Base>()` → `[Inject] List<Base>` | коллекция реализаций без ручного реестра | все конфиги `TimePeriod` |
| `.WithId("...")` | два значения одного типа — редко, лучше завести свой тип | — |
| `BindFactory<TArg, T, T.Factory>()` | объект создаётся в рантайме много раз и сам хочет `[Inject]` | рантайм-объекты с зависимостями |

### Правила

- **`new` на классе с `[Inject]`-полями его ломает.** Такой объект создаётся только
  фабрикой или `DiContainer.Instantiate`.
- **Связь — по конкретному классу.** Интерфейс заводится, только когда реализаций
  две и больше (`IInteractable`: стол магазина, ворота) или когда сцены
  подставляют разные. Интерфейс «на будущее» не делаем.
- **Все сервисы — `AsSingle`.** Один экземпляр на контейнер, а не статический
  синглтон: перезагрузили сцену — получили чистое состояние. Это заодно лечит
  «залипающее» между сессиями состояние ([P1-3](known-issues.md#p1-3-состояние-игры-живёт-в-ассетах)).
- **Обратные связи — событиями.** Сервис публикует `event Action<...>`, подписчик
  инжектит сервис, подписывается в `Initialize` и отписывается в `Dispose`. Сервис
  своих подписчиков не знает.
- **Никакой `static` в сервисах, поведении и UI.** Статика допустима только для
  чистой математики и формул — например, операторы `AllResources`.

## Порядок инициализации

Объект проходит три фазы: **инъекция → `Initialize()` → старт игры**, и каждая
фаза заканчивается для всех объектов, прежде чем начнётся следующая.

| Фаза | Можно | Нельзя |
|---|---|---|
| `InstallBindings` | только `Container.Bind…` | трогать сервисы — их ещё нет |
| конструктор / инициализаторы полей | создать свои коллекции и вложенные объекты | обращаться к `[Inject]`-полям — они ещё `null` |
| `Initialize()` | подписаться на события, найти элементы UI, захватить ссылки на чужие данные | читать чужие **значения** и запускать геймплей — сосед мог ещё не загрузиться |
| `GameStarter` (через кадр) | расставить сцену, запустить сутки, отдать управление игроку | — |

### Порядок строк в инсталлере — это код

`Initialize()` вызывается в порядке биндинга (если не задан `BindExecutionOrder`).
Пользуемся этим сознательно, и **каждая такая зависимость записана комментарием
в шапке инсталлера** (см. пример выше):

- реестр и спавнер — раньше тех, кто в своём `Initialize` регистрирует и спавнит;
- `TimeManager` — раньше всех, кто подписан на время;
- `GameStarter` — в самом конце.

Всё, что не записано таким комментарием, считается негарантированным: в
`Initialize` захватываем ссылку на чужой объект, а значение читаем лениво — в
обработчике, в лямбде, в `Tick`.

### Стартер сцены

Один класс, последний в инсталлере, ждёт кадр и запускает игру:

```csharp
public class GameStarter : IInitializable
{
    [Inject] private PlayerSpawner _player;
    [Inject] private DayCycle      _day;

    public void Initialize() => _ = Start();

    private async Awaitable Start()
    {
        await Awaitable.NextFrameAsync();
        _player.SpawnAtStart();
        _day.Begin();
    }
}
```

## Сервисы

Сервис — обычный C#-класс без `MonoBehaviour`, живущий в контейнере. Его
состояние — поля и события об их изменении; подписчики (UI, другие сервисы)
**реагируют на событие, а не опрашивают сервис каждый кадр**.

```csharp
public class ResourceWallet
{
    public AllResources Current { get; private set; } = new AllResources();
    public event Action<AllResources> OnChanged;

    public void Add(AllResources amount)
    {
        Current += amount;
        OnChanged?.Invoke(Current);
    }

    public bool TrySpend(AllResources price)
    {
        if (!(Current >= price))
            return false;
        Current -= price;
        OnChanged?.Invoke(Current);
        return true;
    }
}
```

`TrySpend` заменяет нынешнюю пару «проверь `>=`, потом `DecreaseResources`»,
которая продублирована в `Inventory` и `ShopController` и не защищена от ухода в
минус.

**Сервис с циклом** реализует `ITickable` (`ILateTickable`, `IFixedTickable`)
вместо `Update` на GameObject'е: ввод, таймеры, слежение UI за объектами мира.
Порядок тика — тот же порядок биндинга.

**Сервис, которому нужен свой GameObject**, создаёт его сам в `Initialize`; если
сервис живёт в `ProjectContext` — вешает объект в `DontDestroyOnLoad`.

### Ввод

Input System — сгенерированный класс `Controls` в `ProjectContext`. Сцена читает
его через свой `InputService : ITickable` — **единственный**, кто знает про
клавиши:

- превращает нажатия в игровые события (`OnAttack`, `OnInteract`, `OnBuildConfirm`,
  `OnToolNext`, ...);
- пускает луч из центра экрана, ищет `GetComponentInParent<IInteractable>()` и
  публикует событие — сам не знает ни дверей, ни столов;
- спрашивает UI, не над ним ли курсор;
- **держит режим курсора**: открылась панель, держащая мышь, — `Cursor.lockState
  = None` и выключенная карта действий геймплея; закрылась — обратно.

Это заменяет `Input.GetKey` в десятке скриптов, `Interract`, `ObjectPicker` и трёх
владельцев курсора ([P1-4](known-issues.md#p1-4-три-владельца-курсора)).

> **Исключение — дрон.** Чтение мыши и клавиш для полёта живёт внутри
> `DroneControl`, и переносить его в `InputService` можно **только по явной
> просьбе**: это правка управления, см. [Дрон](drone.md#правило-не-переписывать-управление).

### Конфиги

ScriptableObject-ассеты, поданные в инсталлер и забинженные `FromInstance`. Числа
баланса живут там или в `private const` внутри класса, а не в магических числах
(`DamagePlayer` с уроном `10`, `DamageBot` с `5`) и не в параметрах методов «на
всякий случай». **SO — только для чтения**, состояние в них не пишем никогда.

## Тела и мозги

Игровой объект делится на **тело** (`MonoBehaviour` на префабе: трансформ,
физика, аниматор) и **мозг** (сервис в контейнере). Тело умеет двигаться и
показываться, решения принимает мозг.

| Сущность | Тело (на префабе) | Мозг (в контейнере) |
|---|---|---|
| Враг, бот | наследник `NPCFacade`: агент, аниматор, коллайдер | `TargetingService` выбирает цель по `TargetRegistry` |
| Турель | `GatlingGun`: поворот, вспышка | `TargetingService` |
| Узел ресурса | `ResourceController`: меш, коллайдер, запас | `ResourceWallet` начисляет, `ResourceNodeSpawner` ведёт точки |
| Постройка | `Building`: модель, звук урона | `BuildService`: правила и цена |
| Дрон | `DroneControl` — полёт **не трогать** | телеметрию читает UI-контроллер |

`StatsHandler` остаётся компонентом тела: это данные и событие конкретной
сущности, а не решение.

## Сцена и контейнер: четыре способа связи

Обычный `Object.Instantiate` префаба с `[Inject]`-полями — **ошибка**: поля
останутся `null`. При `InstantiatePrefab` Zenject заполняет поля до `Awake`, так
что в `Awake` и `Start` их уже можно читать.

| Ситуация | Как | Где в проекте |
|---|---|---|
| объект уже стоит в сцене, ему нужны сервисы | просто `[Inject]`-поля: `SceneContext` инжектит все объекты сцены при загрузке | ворота, стол магазина |
| сервису нужен один известный объект сцены | `Bind<T>().FromComponentInHierarchy()` | `Camera`, `UIDocument` |
| сервисам нужны точки и объекты уровня | один `MonoBehaviour`-«карта» `LevelAnchors` с `[SerializeField]`-ссылками и геттерами, без логики | спавн игрока, портал зомби, точки ресурсов, точка у стола, `BuildingPoint` |
| объект рождается из префаба | только `DiContainer.InstantiatePrefab…` внутри спавнера; спавнер же ставит его на учёт в реестре | `EnemySpawner`, `BotSpawner`, `ResourceNodeSpawner`, `BuildingPlacer` |

`LevelAnchors` заменяет `GameObject.FindGameObjectWithTag("BuildingPoint")`,
дочерние точки `ResourceSpawner` и поле `spawnPos` у `ZombieSpawn`.

```csharp
// Рождение через контейнер (иначе [Inject] на компонентах не отработает)
// и постановка на учёт. Кто именно и где — забота сцены.
public class EnemySpawner
{
    [Inject] private DiContainer    _container;
    [Inject] private TargetRegistry _registry;

    public Enemy Spawn(Enemy prefab, Vector3 position)
    {
        var enemy = _container.InstantiatePrefabForComponent<Enemy>(
            prefab, position, Quaternion.identity, null);
        _registry.Add(enemy);
        return enemy;
    }
}
```

Сервис, держащий список тел, проверяет их жизнь через Unity-null (`if (!body)`):
уничтоженный объект сам сообщает о себе, и телу не нужно помнить про отписку.

### Квесты

Дополнение для этого проекта, в исходном документе его нет. Прежняя система
квестов на графах Unity Behavior мертва — граф удалён, остались только останки
([что именно и как их убрать](quests-and-dialogs.md)). Новая система, когда до неё
дойдёт очередь, строится на общих правилах, без отдельного механизма:

- конфиг квеста — ScriptableObject только для чтения (реплики, условие, награда);
- состояние квестов — сервис сцены `QuestProgress` (`AsSingle`), а не поля ассета;
- условие — обычный C#-класс, подписанный на события сервисов
  (`ResourceWallet.OnChanged`, `BuildService.OnBuilt`, ...), а не опрос каждый кадр;
- прогресс («первая постройка», «первый бот») — в одном сервисе `GameProgress`;
- окно диалога — контроллер на UI Toolkit.

## UI Toolkit

### Слои

На сцену — **один `UIDocument`** с корневым `Main.uxml`. Каждая панель — отдельный
UXML-файл, включённый в корень как `<ui:Instance>`, растянутый на весь экран и
прозрачный для мыши (`picking-mode="Ignore"`). **Порядок `Instance` в файле и есть
порядок слоёв.**

```
Main.uxml
├── Hud           ресурсы, часы, прицел
├── DroneHud      телеметрия дрона       (уже есть: DroneStats.uxml)
├── WorldMarkers  полоски HP над NPC и постройками
├── BuildPanel    выбор постройки
├── ShopPanel     покупка ботов
├── Dialog        реплики квестов — когда появится новая система квестов
└── PauseMenu     системная, держит мышь
```

### Контроллер — обычный C#-класс

`UIDocument` биндится в сценовом инсталлере `FromComponentInHierarchy()`,
контроллеры — `BindInterfacesAndSelfTo<T>().AsSingle()`. **Ни один контроллер не
`MonoBehaviour`.** Жизнь одинаковая:

1. `Initialize()` — `Q<>()` своих элементов по `name`, подписка на кнопки и на
   события сервисов, первая отрисовка;
2. работа — только реакция на события; `Tick` — только там, где надо следить за
   миром (метки над объектами, телеметрия дрона);
3. `Dispose()` — отписка от всего, на что подписался.

Zenject вызывает `Initialize` из `Start` ядра сцены — к этому моменту `UIDocument`
уже построил дерево в своём `OnEnable`. Ленивый поиск элементов в первом `Update`,
как сейчас в `DroneStatsController`, становится не нужен.

```csharp
public class ShopPanelController : IInitializable, IDisposable
{
    [Inject] private UIDocument   _document;
    [Inject] private ShopService  _shop;
    [Inject] private InputService _input;

    private VisualElement _panel;
    private Action _unsubscribe;

    public void Initialize()
    {
        _panel = _document.rootVisualElement.Q("ShopPanel");
        var buyDefender = _panel.Q<Button>("BtnBuyDefender");
        var close       = _panel.Q<Button>("BtnClose");

        Action onBuy   = () => _shop.TryBuyDefender();   // одним вызовом — правил здесь нет
        Action onClose = Hide;
        buyDefender.clicked += onBuy;
        close.clicked       += onClose;
        _unsubscribe = () =>
        {
            buyDefender.clicked -= onBuy;
            close.clicked       -= onClose;
        };
    }

    public void Dispose() => _unsubscribe?.Invoke();

    public void Show()
    {
        _panel.style.display = DisplayStyle.Flex;
        _panel.pickingMode   = PickingMode.Position;   // теперь панель ловит мышь
        _input.PushCursorMode(CursorMode.Ui);
    }

    public void Hide()
    {
        _panel.style.display = DisplayStyle.None;
        _panel.pickingMode   = PickingMode.Ignore;
        _input.PopCursorMode();
    }
}
```

### Как делить UI на классы

- **По ответственности, а не по панели.** Навигация (кнопки, вкладки, видимость) —
  один класс; содержимое каждой вкладки — свой маленький `*Renderer`. Навигатор
  говорит рендереру «покажись», что рисовать — решает рендерер. Универсальный
  «движок панелей» не строим.
- **Панель не командует игрой.** Она показывает состояние сервисов и передаёт
  ввод игрока в сервис одним вызовом; игровых правил в контроллере нет. Нынешний
  `ShopController` смешивает правило (проверку цены и спавн) с панелью — в цели
  это `ShopService` + `ShopPanelController`.
- **Полноэкранные системные панели** (пауза, настройки, загрузка) накрывают всё и
  держат мышь; игровые панели — нет.

### Привязка UI к объектам мира

Метка над объектом — обычный экранный элемент, который каждый кадр встаёт в
спроецированную точку, а **не world-space панель**: текст читается одинаково на
любом расстоянии. Это заменяет мировые uGUI-канвасы `CanvasController` и
`PhpBarController`. Контроллер — `ITickable`:

```csharp
Vector3 point = target.MarkerPoint;
bool visible = _camera.WorldToViewportPoint(point).z > 0f;   // за спиной проекция зеркалится
marker.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
if (visible)
{
    Vector2 p = RuntimePanelUtils.CameraTransformWorldToPanel(_root.panel, point, _camera);
    marker.style.left = p.x;
    marker.style.top  = p.y;
}
```

Наоборот, `InputService` спрашивает UI, не над ним ли курсор:
`panel.Pick(RuntimePanelUtils.ScreenToPanel(...))` — не `null` и не корень, значит
«клик съел интерфейс».

### Стили и шаблоны

**Динамический контент — только из UXML-шаблонов.** Строка магазина, вариант
постройки, метка над объектом — это маленький `.uxml`, а не `new VisualElement()`
с проставленными в C# стилями. Вёрстка остаётся в UI Builder, код только
заполняет данные. Ссылки на все шаблоны собраны в один ScriptableObject
`UITemplate` в `ProjectContext`:

```csharp
[CreateAssetMenu(menuName = "Data/UI Template")]
public class UITemplate : ScriptableObject
{
    [SerializeField] private VisualTreeAsset _shopItemTemplate;
    [SerializeField] private VisualTreeAsset _markerTemplate;
    public VisualTreeAsset ShopItemTemplate => _shopItemTemplate;
    public VisualTreeAsset MarkerTemplate   => _markerTemplate;
}
```

**USS: утилитарные классы + inline.**

- Один общий `MainStyle.uss` с утилитарными классами: заливки, рамки, цвета
  текста, размеры шрифта (`.text-sm` / `.text-md` / `.text-lg`), `.bold`, состояния
  кнопок `:hover` / `:active` / `:disabled`.
- Всё, что относится к одному конкретному элементу (отступы, позиция, ширина
  рамки), пишется **inline в UXML**. Классов вида `.inventory-left-panel-header`
  нет.
- Стили из C# — только то, что реально меняется в игре: `display`, `opacity`,
  позиция метки, ширина полоски здоровья. Или `AddToClassList` /
  `RemoveFromClassList`.
- Стилевой лист достаёт только до своего документа: корневому `Main.uxml` нужен
  свой `<Style>`, иначе его прямые дети (тултипы) останутся без стилей вложенных
  шаблонов.

> Нынешний [`Cyberpunk.uss`](../Assets/UI%20Toolkit/Cyberpunk.uss) построен на
> BEM-классах (`.cyber-panel__header`, `.cyber-row__value--hot`) — это ровно тот
> стиль, который правило запрещает. Палитра сохраняется, а форма переводится в
> утилитарные классы на [этапе 7](architecture-plan.md#этап-7-интерфейс), раскладка
> — в [Интерфейсе](ui.md#киберпанк-стиль).

## Ловушки

**На обязательные данные null-проверок не ставим.** Элемент UI по имени, конфиг,
якорь, непустой список инструментов — если их нет, это ошибка сборки сцены.
Опечатка в `name` должна упасть в `Initialize` с понятным стеком, а не тихо
выключить панель. (Сейчас код делает наоборот, например `if (speedValue != null)`
в `DroneStatsController`.)

| Симптом | Причина | Что делать |
|---|---|---|
| `[Inject]`-поле `null` у объекта, созданного в рантайме | `new` или `Object.Instantiate` мимо контейнера | фабрика или `DiContainer.Instantiate` / `InstantiatePrefab` |
| `Initialize` / `Tick` молча не вызываются | класс забинжен `Bind<T>()`, а не `BindInterfacesAndSelfTo<T>()` | любой класс с `IInitializable` / `ITickable` / `IDisposable` — только через `BindInterfaces…` |
| после перезагрузки сцены ошибки от уничтоженных элементов | сценовый контроллер подписался на проектный сервис и не отписался | каждой подписке в `Initialize` — отписка в `Dispose` |
| сосед в `Initialize` отдаёт нули или пустые списки | сосед ещё не инициализирован | в `Initialize` только ссылки, значения — лениво или в стартере |
| клики не доходят до игры, «курсор всегда над UI» | полноэкранный слой ловит мышь | `picking-mode="Ignore"` на каждом слое; `Position` — только у открытой панели |
| анимация меню или загрузка застывает на паузе | `timeScale = 0`, а код считает `Time.deltaTime` | в UI и загрузке — `Time.unscaledDeltaTime` и ожидание кадрами |
| `FromComponentInHierarchy` вернул не тот объект | в сцене два компонента этого типа | так биндим только единственные; остальное — через `LevelAnchors` |
| картинки в UXML пропали после перенастройки текстуры | спрайт переключён Multiple ↔ Single, `fileID` сменился | режим спрайта выбирается при импорте и больше не меняется |

## Чеклист каркаса

- [ ] `Resources/ProjectContext.prefab` + `ProjectInstaller`: `GameConfig`,
      `UITemplate`, `Controls`, настройки, звук — по мере появления.
- [ ] В каждой игровой сцене `SceneContext`; базовый `GameplayInstaller` и
      наследники под сцены; отдельный инсталлер для главного меню, когда оно появится.
- [ ] В шапке каждого инсталлера — комментарий: какой порядок строк важен и почему.
- [ ] `LevelAnchors : MonoBehaviour` — точки спавна и ключевые объекты уровня,
      бинд `FromComponentInHierarchy`.
- [ ] `PlayerSpawner` рождает игрока через `DiContainer.InstantiatePrefab`; тело
      игрока получает сервисы через `[Inject]`.
- [ ] `InputService : ITickable` — единственный, кто читает `Controls`: движение,
      взгляд, луч взаимодействия из центра экрана → `IInteractable`, режим курсора.
- [ ] Один `UIDocument` и `Main.uxml` со слоями; на каждый слой — свои контроллеры
      и рендереры.
- [ ] `UITemplate` со всеми шаблонами динамических элементов; `MainStyle.uss` с
      утилитарными классами.
- [ ] `GameStarter` — последней строкой инсталлера.
