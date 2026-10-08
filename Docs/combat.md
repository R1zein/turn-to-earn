# Бой и здоровье

## StatsHandler — единая модель здоровья

[`StatsHandler`](../Assets/Scripts/UI/StatsHandler.cs) висит на всём, что можно
убить: игрок, боты, враги, постройки, турели.

```csharp
public float maxHP, currHP;
public event Action OnDamage;   // после каждого получения урона
public event Action OnDeath;    // один раз, когда currHP упало до нуля
public bool IsDead { get; }
public Fraction fraction;
```

`currHP = maxHP` выставляется в `Start()`. После смерти `TakeDamage` ничего не
делает — турели и оружие продолжают бить труп, пока его не уничтожат, но
`OnDeath` поднимается ровно один раз.

Всё поведение смерти построено на подписке на `OnDeath`: `Player` спавнит камеру
смерти, `Building` уничтожает себя, `NPCFacade` уходит из реестра целей,
проигрывает анимацию и убирает агента, `GatlingGun` уничтожается.

## Фракции

```csharp
public enum Fraction { Player, Friendly, Enemy, Neutral }
```

Задаётся в инспекторе на `StatsHandler`. **«Свой/чужой» в бою решается только
фракцией** — ни одно оружие не смотрит на тип компонента.

| Фракция | У кого |
|---|---|
| `Player` | игрок |
| `Friendly` | боты, стена, турель |
| `Enemy` | `Zombie1`, `spider` (у паука до 2026-10-08 по ошибке стояла `Player`) |

## Нанесение урона

### Оружие ближнего боя — `DamageDealer`

[`DamageDealer`](../Assets/Scripts/DamageDealer.cs) — триггер на оружии или
конечности: при входе в триггер бьёт `damage`, если у задетого `StatsHandler` с
фракцией `targetFraction`.

| Где | Цель | Урон |
|---|---|---|
| руки `Zombie1`, лапы `spider` | `Player` | 10 |
| руки `Zombie1`, лапы `spider` | `Friendly` | 5 |
| меч игрока (`MagicSword_Ice`) | `Enemy` | 50 |
| меч защитника (`MagicSword_Iron`) | `Enemy` | 50 |

Урон — поле на префабе: это настройка конкретного оружия.

> Урон идёт по факту **входа** в триггер, без учёта анимации удара и без
> перезарядки — зажатый в цели враг наносит урон один раз и больше никогда,
> пока не выйдет и не войдёт снова.

До этапа 6 это были четыре скрипта (`DamagePlayer`, `DamageBot`, `DamageEnemy`,
`DamageZombie`), половина из которых решала по фракции, половина — по компоненту
`Enemy`.

### Лазер дрона

Описан в [Дроне](drone.md#стрельба): луч (`Physics.Raycast`) по траектории лазера,
тик `interval`, `StatsHandler` ищется вверх по иерархии от попадания; бьёт тоже
только фракцию `Enemy`.

## Турель

[`GatlingGun`](../Assets/Scripts/AI/GatlingGun.cs) — автоматическая защита базы,
сама стоит в реестре целей как `Turret`.

1. В `Awake` подгоняет радиус `SphereCollider` под `firingRange`.
2. Раз в секунду просит `TargetingService.FindNearest<Enemy>` — ближайшего врага
   в радиусе `firingRange` из реестра.
3. `AimAndFire()` каждый кадр: доворачивает основание и ствол на цель, крутит
   стволы, запускает `muzzelFlash`, и раз в `fireRate` секунд наносит `damage`.
4. На смерть цели подписывается `CheckTarget`, чтобы сбросить её и прекратить
   огонь.

Особенности:

- Стрельба **без луча и без препятствий** — урон наносится напрямую по цели, стены
  не защищают.
- Поле `stats` публичное, но перезаписывается в `Awake` через `GetComponent` —
  значение из инспектора не имеет смысла.
- Подписка/отписка от `OnDeath` целей сделана вручную и разъедется, если цель
  уничтожат не через смерть (например, при выгрузке сцены).

## Отображение здоровья

| Скрипт | Чем показывает | Где |
|---|---|---|
| [`CanvasController`](../Assets/Scripts/UI/CanvasController.cs) | `Image.fillAmount` | мировой Canvas над NPC |
| [`PhpBarController`](../Assets/Scripts/UI/PhpBarController.cs) | `Slider.value` | тоже uGUI |

Оба подписаны на `OnDamage` и пересчитывают долю `currHP / maxHP`. Оба — на старом
uGUI; заменяются `WorldMarkersController` на [этапе 7](architecture-plan.md#этап-7-интерфейс).
`CanvasController` дополнительно разворачивает Canvas к камере и прячет его
дальше `visableDistance`.

> Имя `PhpBarController` — опечатка, имелось в виду «HP Bar».
