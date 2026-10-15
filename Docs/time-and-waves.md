# Время суток и волны

Всё время живёт в контейнере сцены `Scene 5` ([`Scene5Installer`](../Assets/Scripts/Installers/Scene5Installer.cs)):

```
GameConfig (SO, ProjectContext)   длина часа, час старта, список периодов
TimeManager : ITickable           часы; стоят, пока GameStarter не вызовет Begin()
├── DayCycle                      какие периоды активны, сколько раз начинались, события
│   ├── WaveService               волна на каждом портале в начале ночи
│   └── ClockHudController        часы и фаза суток в HUD
└── DayLighting : ITickable       солнце, луна, туман, небо — непрерывно по часу
```

## Сутки

[`TimeManager`](../Assets/Scripts/Services/TimeManager.cs) — часы мира.

- `GameConfig.SecondsPerHour` задаёт длину игрового часа в реальных секундах
  (сейчас 20).
- [`GameStarter`](../Assets/Scripts/Services/GameStarter.cs) через кадр после
  инициализации зовёт `Begin()`: таймер встаёт на `StartHour` — **игра
  начинается в 7 утра**.
- По достижении 24 часов таймер сбрасывается в ноль.
- `Hour` — непрерывный час 0..24 (до `Begin()` — час старта); по нему живёт
  освещение.
- `Hours`/`Minutes` и событие `OnClockChanged` (раз в игровую минуту).
  [`ClockHudController`](../Assets/Scripts/UI/Controllers/ClockHudController.cs) пишет их в HUD как
  `{часы}:{минуты:00}`, а под часами — имя текущего периода (`DAWN`, `DAY`,
  `DUSK`, `NIGHT`).

Каждый тик `TimeManager` передаёт текущий час в `DayCycle.Advance(hour)`.

## Фазы суток: 300 с света, 180 с ночи

Сутки — 24 игровых часа по 20 с = 480 с. Четыре периода (`Data/*.asset`):

| Период | Часы | Реальных секунд | Что происходит |
|---|---|---|---|
| `Dawn` | 6:00 → 7:30 | 30 | солнце встаёт, тёплый свет, туман розовеет |
| `Day` | 7:30 → 19:30 | 240 | полный свет, солнце в зените в 13:30 |
| `Dusk` | 19:30 → 21:00 | 30 | солнце садится, красный свет |
| `Night` | 21:00 → 6:00 | 180 | лунный свет, ночной скайбокс, **волна** |

Светлая часть (рассвет + день + закат) — 300 с, ночь — 180 с. Игра начинается в
7:00, в конце рассвета. Периоды не перекрываются и покрывают сутки целиком.

[`TimePeriod`](../Assets/Scripts/TimePeriod.cs) — ScriptableObject **только для
чтения**: `periodStart`/`periodEnd` (часы, дробные) и `soundEffect` (сейчас
никто не проигрывает). `TryGetProgress(hour, out progress)` — чистая функция:
внутри ли час периода и насколько он пройден (0..1). Период, у которого начало
не раньше конца (например 21→6), идёт **через полночь**.

[`DayCycle`](../Assets/Scripts/Services/DayCycle.cs) — состояние:

1. На входе в период — `EnterCount(period)` растёт на 1, событие
   `OnPeriodEnter(period)`.
2. На выходе — `OnPeriodExit(period)`.
3. Внутри периода каждый тик — `OnPeriodProgress(period, progress)`.

Периоды — это **игровые события** (волна, надпись в HUD). Свет от них не зависит.

## Освещение

[`DayLighting`](../Assets/Scripts/Services/DayLighting.cs) каждый кадр считает
вид мира из одного непрерывного `TimeManager.Hour`, поэтому на границах фаз
ничего не прыгает:

- **Солнце** встаёт в начале рассвета (`GameConfig.SunriseHour` = начало
  `Dawn`) и садится в конце заката (`SunsetHour` = конец `Dusk`). Между ними —
  дуга: высота `NoonSunElevation · sin(πt)`, азимут проходит 180°.
- **Ночью** тот же directional light — луна под фиксированным углом
  `MoonElevation`. Направление меняется на горизонте, где интенсивность ≈ 0.
- **Кривые и градиенты суток** — `GameConfig` → Sky (у кривых ось — часы 0..24,
  у градиентов — доля суток): цвет и интенсивность солнца, ambient (он же
  множитель отражений и `ReflectionProbe`), цвет тумана, экспозиция неба.
  Касательные кривых — `ClampedAuto`, без выбросов между ключами.
- **Скайбокс** дневной/ночной меняется на восходе и закате, когда экспозиция
  по кривой минимальна. Используются копии материалов — ассеты не меняются.

`Sun` и `ReflectionProbe` берутся из
[`LevelAnchors`](../Assets/Scripts/Level/LevelAnchors.cs). Чтобы поменять длину
фаз, двигать границы `Dawn`/`Dusk` **вместе с ключами кривых**: кривые заданы в
часах, а не в долях периода.

Состояние живёт в сервисе сцены: перезагрузка сцены начинает счёт ночей с нуля,
а в ассеты ничего не пишется.

## Волны врагов

[`WaveService`](../Assets/Scripts/Services/WaveService.cs) подписан на
`DayCycle.OnPeriodEnter`. Когда начинается `GameConfig.WavePeriod` (ночь), на
каждом из пяти порталов (`LevelAnchors.Portals`) запускается волна:

```csharp
int count = config.WaveBaseCount + dayCycle.EnterCount(period) * 2;
// на каждом портале:
for (int i = 0; i < count; i++)
{
    spawner.Spawn(config.WaveEnemy, portal.SpawnPosition);
    await Awaitable.WaitForSecondsAsync(config.WaveSpawnInterval, lifetime.Token);
}
```

Формула сложности: **базовое число + 2 за каждую наступившую ночь**. Ночь N даёт
`5 + 2N` врагов (`Zombie1`) на портал с интервалом 1 с. Рост линейный и ничем не
ограничен сверху.

**Пауки** (`GameConfig.SpiderEnemy`, префаб `Prefabs/Spider/spider`) — тяжёлый
враг: 250 HP, скорость 10 против 5 у зомби. Появляются с ночи
`SpiderFirstWave` (3) и выходят из портала **после** его зомби:

```
пауков за ночь N = (N − SpiderFirstWave + 1) · SpidersPerWave    // при N ≥ 3
```

| Ночь | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
|---|---|---|---|---|---|---|---|---|
| зомби на портал | 7 | 9 | 11 | 13 | 15 | 17 | 19 | 21 |
| пауков всего | 0 | 0 | 1 | 2 | 3 | 4 | 5 | 6 |

Пауки раздаются по одному на портал по кругу, начиная с портала `N mod 5`, — каждую
ночь первый паук выходит из другого места. Пустой `SpiderEnemy` отключает пауков.

- [`Portal`](../Assets/Scripts/Level/Portal.cs) — тело: точка появления и эффект,
  который `WaveService` включает на время волны.
- Враги рождаются через [`EnemySpawner`](../Assets/Scripts/Services/Spawning/EnemySpawner.cs)
  (контейнер).
- Выгрузка сцены останавливает недоигранные волны: после `Dispose` цикл выходит на следующем шаге.
- Портал, ещё выпускающий прошлую волну, вторую поверх не начинает.

### Проблемы

- Спавн всегда в одной точке портала, пачкой — враги выходят стопкой друг в
  друге, пока NavMesh их не растолкает.

## Как связать новую механику со временем

Подписываться на события `DayCycle`, а не опрашивать часы. Подписчик — обычный
класс в контейнере (`BindInterfacesTo<T>()` в инсталлере сцены):

```csharp
public class NightLights : IInitializable, IDisposable
{
    [Inject] private DayCycle dayCycle;
    [Inject] private GameConfig config;

    public void Initialize() => dayCycle.OnPeriodEnter += OnPeriodEnter;
    public void Dispose()    => dayCycle.OnPeriodEnter -= OnPeriodEnter; // то же событие!

    private void OnPeriodEnter(TimePeriod period) { /* ... */ }
}
```

Отписываться **от того же события**, на которое подписался: перепутать
`OnPeriodEnter` и `OnPeriodExit` — частая ошибка, которая молча оставляет висячую
подписку. Сервис живёт ровно столько, сколько сцена, поэтому подписка не
переживёт её выгрузку, даже если `Dispose` забыли.

Подписчик должен стоять в инсталлере **после** `DayCycle`/`TimeManager` —
порядок записан в шапке `Scene5Installer`.
