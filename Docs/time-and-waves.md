# Время суток и волны

Всё время живёт в контейнере сцены `Scene 5` ([`Scene5Installer`](../Assets/Scripts/Installers/Scene5Installer.cs)):

```
GameConfig (SO, ProjectContext)   длина часа, час старта, список периодов
TimeManager : ITickable           часы; стоят, пока GameStarter не вызовет Begin()
└── DayCycle                      какие периоды активны, сколько раз начинались, события
    ├── DayLighting               скайбокс и интенсивность света
    ├── WaveService               волна на каждом портале в начале ночи
    └── ClockView                 TMP-часы на Canvas (временно, до этапа 7)
```

## Сутки

[`TimeManager`](../Assets/Scripts/Services/TimeManager.cs) — часы мира.

- `GameConfig.SecondsPerHour` задаёт длину игрового часа в реальных секундах
  (сейчас 20).
- [`GameStarter`](../Assets/Scripts/Services/GameStarter.cs) через кадр после
  инициализации зовёт `Begin()`: таймер встаёт на `StartHour` — **игра
  начинается в 7 утра**.
- По достижении 24 часов таймер сбрасывается в ноль.
- `Hours`/`Minutes` и событие `OnClockChanged` (раз в игровую минуту).
  [`ClockView`](../Assets/Scripts/UI/ClockView.cs) пишет их в TMP-поле как
  `{часы}:{минуты}`.

> Минуты не дополняются нулём: в 7:05 на экране будет `7:5`.

Каждый тик `TimeManager` передаёт текущий час в `DayCycle.Advance(hour)`.

## Периоды суток

[`TimePeriod`](../Assets/Scripts/TimePeriod.cs) — ScriptableObject **только для
чтения**, описывающий отрезок суток. Сейчас их два: `Data/Day.asset` (7→23) и
`Data/Night.asset` (23→7).

| Поле | Смысл |
|---|---|
| `periodStart`, `periodEnd` | границы в часах (0–24) |
| `skyboxMaterial` | скайбокс, включаемый на входе в период |
| `curve` | кривая освещённости по ходу периода |
| `soundEffect` | звук периода (сейчас никто не проигрывает) |

`TryGetProgress(hour, out progress)` — чистая функция: внутри ли час периода и
насколько он пройден (0..1). Период, у которого начало не раньше конца
(например 23→7), идёт **через полночь**: часы после полуночи сдвигаются на +24.

[`DayCycle`](../Assets/Scripts/Services/DayCycle.cs) — состояние:

1. На входе в период — `EnterCount(period)` растёт на 1, событие
   `OnPeriodEnter(period)`.
2. На выходе — `OnPeriodExit(period)`.
3. Внутри периода каждый тик — `OnPeriodProgress(period, progress)`.

[`DayLighting`](../Assets/Scripts/Services/DayLighting.cs) на входе ставит
скайбокс и зовёт `DynamicGI.UpdateEnvironment()`, по ходу периода ведёт по
кривой интенсивность солнца и `ReflectionProbe` — оба берутся из
[`LevelAnchors`](../Assets/Scripts/Level/LevelAnchors.cs).

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

- [`Portal`](../Assets/Scripts/Level/Portal.cs) — тело: точка появления и эффект,
  который `WaveService` включает на время волны.
- Враги рождаются через [`EnemySpawner`](../Assets/Scripts/Services/Spawning/EnemySpawner.cs)
  (контейнер).
- Выгрузка сцены отменяет недоигранные волны (`Dispose` → токен).
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
