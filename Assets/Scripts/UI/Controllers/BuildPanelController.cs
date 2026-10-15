using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Zenject;

// Build menu: Tab opens it, a card picks a ghost, Esc/Tab closes. Rules are BuildService's.
public class BuildPanelController : IInitializable, IDisposable
{
    [Inject] private UIDocument document;
    [Inject] private ItemCards cards;
    [Inject] private GameConfig config;
    [Inject] private InputService input;
    [Inject] private BuildService buildService;

    private readonly List<Action> unsubscribe = new List<Action>();
    private readonly List<(Button card, Ghost ghost)> options = new List<(Button, Ghost)>();
    private VisualElement panel;
    private bool isOpen;

    public void Initialize()
    {
        panel = document.rootVisualElement.Q("BuildPanel");
        VisualElement list = panel.Q("BuildOptions");
        foreach (BuildOption option in config.BuildOptions)
        {
            Ghost ghost = option.Ghost;
            Button card = cards.Add(list, option.Title, option.Icon, ghost.requiredResources,
                () => Pick(ghost), unsubscribe);
            options.Add((card, ghost));
        }

        input.OnBuildMenu += Show;
        input.OnUiClose += Hide;
    }

    public void Dispose()
    {
        input.OnBuildMenu -= Show;
        input.OnUiClose -= Hide;
        foreach (Action action in unsubscribe)
            action();
    }

    private void Show()
    {
        if (isOpen)
            return;

        isOpen = true;
        foreach (var (card, ghost) in options)
            card.SetEnabled(buildService.CanAfford(ghost));
        panel.RemoveFromClassList("hidden");
        input.PushMode(InputMode.Ui);
    }

    private void Hide()
    {
        if (!isOpen)
            return;

        isOpen = false;
        panel.AddToClassList("hidden");
        input.PopMode();
    }

    private void Pick(Ghost ghost)
    {
        buildService.Pick(ghost);
        Hide();
    }
}
