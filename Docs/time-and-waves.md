# Время суток и волны

## Сутки

[`TimeManager`](../Assets/Scripts/TimeManager.cs) — часы мира.

- `secondsPerHour` задаёт длину игрового часа в реальных секундах.
- Таймер стартует с `7 * secondsPerHour`, то есть **игра начинается в 7 утра**.
- По достижении 24 часов таймер сбрасывается в ноль.
- Текущее время пишется в TMP-поле как `{часы}:{минуты}`.

> Минуты не дополняются нулём: в 7:05 на экране будет `7:5`.

Каждый кадр `TimeManager` прогоняет все `TimePeriod` через `ProgressTime(timer)`.

## Периоды суток

[`TimePeriod`](../Assets/Scripts/TimePeriod.cs) — ScriptableObject, описывающий
отрезок суток (утро, день, вечер, ночь).

| Поле | Смысл |
|---|---|
| `periodStart`, `periodEnd` | границы в часах (0–24) |
| `skyboxMaterial` | скайбокс, включаемый на входе в период |
| `curve` | кривая освещённости по ходу периода |
| `soundEffect` | звук периода |
| `dayNumber` | счётчик наступлений периода |

Логика `ProgressTime`:

1. Определяет, находимся ли мы внутри периода. Периоды **через полночь**
   (например 22→5) обрабатываются отдельной веткой с переносом на +24 часа.
2. На входе — `PeriodEnter()`: `dayNumber++`, смена скайбокса,
   `DynamicGI.UpdateEnvironment()`, событие `OnPeriodEnter`.
3. На выходе — `PeriodExit()` и событие `OnPeriodExit`.
4. Внутри периода — считает `currentProgress` (0..1) и ведёт по кривой
   интенсивность солнца и `ReflectionProbe`.

### Подводные камни

- **Состояние в ассете.** `dayNumber`, `currentProgress`, `wasInPeriod` — поля
  SO, то есть сохраняются в файл. Спасает только то, что `InitSettings` сбрасывает
  их в `Awake` у `TimeManager`. Если период окажется не подключён к менеджеру,
  он унесёт значения из прошлой сессии.
- **Свет берётся не из того поля.** В `Awake` объявлена локальная переменная
  `directionlLight` (опечатка), которая ищет объект
  `GameObject.Find("Directional Light")`. Публичное поле `directionLight`,
  выставляемое в инспекторе, **не используется никогда**. Переименуй объект на
  сцене — и освещение перестанет меняться.
- События `OnPeriodEnter`/`OnPeriodExit` живут в ассете и переживают выгрузку
  сцены. Подписчики обязаны отписываться в `OnDisable`, иначе события накопятся
  и при следующем запуске выстрелят в уничтоженные объекты.

## Волны врагов

[`ZombieSpawn`](../Assets/Scripts/Enemy/ZombieSpawn.cs) подписан на `OnPeriodEnter`
своего `TimePeriod` — обычно это ночь.

```csharp
for (int i = 0; i < spawnCount + timePeriod.dayNumber * 2; i++)
{
    Instantiate(zombie, spawnPos.position, Quaternion.identity);
    await Awaitable.WaitForSecondsAsync(spawnTime);
}
```

Формула сложности: **базовое число + 2 за каждый прошедший день**. Ночь N даёт
`spawnCount + 2N` врагов. Рост линейный и ничем не ограничен сверху.

На время спавна включается `portalEffect`, после — выключается.

### Проблемы

- `OpenPortal` объявлен `async void` без токена отмены. Если объект уничтожат или
  выгрузят сцену в процессе спавна, цикл продолжит работать и упадёт на обращении
  к уничтоженному `portalEffect`. Нужен `async Awaitable` +
  `destroyCancellationToken`.
- Спавн всегда в одной точке `spawnPos`, пачкой — враги выходят стопкой друг в
  друге, пока NavMesh их не растолкает.
- Если период наступит повторно до окончания прошлого спавна, запустится второй
  параллельный цикл: счётчик не защищён.

## Как связать новую механику со временем

Подписываться на события, а не опрашивать часы. Отписываться **от того же
события**, на которое подписался: перепутать `OnPeriodEnter` и `OnPeriodExit` —
частая ошибка, которая молча оставляет висячую подписку.

**Сейчас** события живут в ассете `TimePeriod`:

```csharp
[SerializeField] private TimePeriod night;

private void OnEnable()  => night.OnPeriodEnter += StartNightBehaviour;
private void OnDisable() => night.OnPeriodEnter -= StartNightBehaviour; // то же событие!
```

Отписка обязательна: события в SO переживают сцену.

**Цель** ([этап 3](architecture-plan.md#этап-3-состояние-из-ассетов-в-сервисы)):
состояние суток держит сервис сцены `DayCycle`, `TimePeriod` остаётся только
конфигом, а подписчик — обычный класс в контейнере:

```csharp
public class NightLights : IInitializable, IDisposable
{
    [Inject] private DayCycle _day;

    public void Initialize() => _day.OnNightStarted += TurnOn;
    public void Dispose()    => _day.OnNightStarted -= TurnOn;

    private void TurnOn() { /* ... */ }
}
```

Сервис живёт ровно столько, сколько сцена, поэтому подписка не переживёт её
выгрузку, даже если `Dispose` забыли.
