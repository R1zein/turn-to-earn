using System;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

public class PlayerHealthHudController : IInitializable, IDisposable
{
    [Inject] private UIDocument document;
    [Inject] private Player player;

    private StatsHandler stats;
    private Label value;
    private VisualElement fill;

    public void Initialize()
    {
        VisualElement root = document.rootVisualElement;
        value = root.Q<Label>("HealthValue");
        fill = root.Q("HealthFill");

        stats = player.GetComponent<StatsHandler>();
        stats.OnDamage += Redraw;
        // StatsHandler fills currHP in its own Start; until the first hit it is full.
        Draw(stats.maxHP);
    }

    public void Dispose() => stats.OnDamage -= Redraw;

    private void Redraw() => Draw(stats.currHP);

    private void Draw(float hp)
    {
        float share = Mathf.Clamp01(hp / stats.maxHP);
        value.text = Mathf.CeilToInt(Mathf.Max(hp, 0f)).ToString();
        fill.style.width = Length.Percent(share * 100f);
    }
}
