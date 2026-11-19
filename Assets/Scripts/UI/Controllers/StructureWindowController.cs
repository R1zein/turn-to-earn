using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

// The one window of every structure the player interacts with (E, PlayerInteraction).
// The header is the structure: icon, name, health. Below are the tabs it has - what
// it does (BOTS and BASE at the shop table) and what it can turn into (MODIFY); each
// tab is an IStructureTab, and the window only lays them out, opens and closes. A
// structure with no tabs (a gate, for now) does not open it.
// Upgraded while open, the window moves to what took its place; destroyed, it closes.
// Closed by its button, Esc, Tab or E.
public class StructureWindowController : IInitializable, IDisposable
{
    [Inject] private UIDocument document;
    [Inject] private InputService input;
    [Inject] private StructureUpgrades upgrades;
    [Inject] private List<IStructureTab> tabs;

    private readonly List<Action> unsubscribe = new List<Action>();
    private readonly Dictionary<IStructureTab, Button> buttons = new Dictionary<IStructureTab, Button>();
    private readonly Dictionary<IStructureTab, VisualElement> pages = new Dictionary<IStructureTab, VisualElement>();
    private VisualElement panel, frame, tabBar, icon, healthFill;
    private Label title, health;
    private Structure current;
    private IStructureTab shown;
    private bool isOpen;

    public void Initialize()
    {
        panel = document.rootVisualElement.Q("StructureWindow");
        frame = panel.Q("WindowFrame");
        tabBar = panel.Q("WindowTabs");
        icon = panel.Q("WindowIcon");
        title = panel.Q<Label>("WindowTitle");
        health = panel.Q<Label>("WindowHealth");
        healthFill = panel.Q("WindowHealthFill");

        foreach (IStructureTab tab in tabs)
        {
            Button button = panel.Q<Button>(tab.Name + "Tab");
            buttons.Add(tab, button);
            pages.Add(tab, panel.Q(tab.Name + "Page"));
            IStructureTab picked = tab;
            Subscribe(button, () => Select(picked));
        }

        Subscribe(panel.Q<Button>("CloseButton"), Hide);
        input.OnUiClose += Hide;
        upgrades.OnUpgraded += OnUpgraded;
    }

    public void Dispose()
    {
        input.OnUiClose -= Hide;
        upgrades.OnUpgraded -= OnUpgraded;
        Release();
        foreach (Action action in unsubscribe)
            action();
    }

    public bool HasTabs(Structure structure) => tabs.Exists(tab => tab.IsFor(structure));

    // Captions of the tabs the structure has, for the hint under the aimed structure.
    public string Captions(Structure structure)
    {
        var captions = new List<string>();
        foreach (IStructureTab tab in tabs)
            if (tab.IsFor(structure))
                captions.Add(buttons[tab].text);
        return string.Join("  ·  ", captions);
    }

    public void Open(Structure structure)
    {
        if (isOpen || !HasTabs(structure))
            return;

        isOpen = true;
        panel.RemoveFromClassList("hidden");
        input.PushMode(InputMode.Ui);
        Bind(structure, null);
    }

    private void Hide()
    {
        if (!isOpen)
            return;

        isOpen = false;
        shown?.Hide();
        shown = null;
        Release();
        panel.AddToClassList("hidden");
        input.PopMode();
    }

    // Draws a structure and its tabs, on `preferred` if it has that tab, else on its first.
    private void Bind(Structure structure, IStructureTab preferred)
    {
        Release();
        current = structure;
        current.Stats.OnDamage += DrawHealth;
        current.Stats.OnDeath += Hide;

        icon.style.backgroundImage = new StyleBackground(structure.Icon);
        title.text = structure.Title;
        DrawHealth();

        IStructureTab first = null;
        int tabCount = 0;
        foreach (IStructureTab tab in tabs)
        {
            bool has = tab.IsFor(structure);
            buttons[tab].EnableInClassList("hidden", !has);
            if (has && first == null)
                first = tab;
            if (has)
                tabCount++;
        }
        tabBar.EnableInClassList("hidden", tabCount == 1);
        frame.style.width = tabCount == 1 ? 870f : 1240f;
        Select(preferred != null && preferred.IsFor(structure) ? preferred : first);
    }

    private void Release()
    {
        if (current == null)
            return;

        current.Stats.OnDamage -= DrawHealth;
        current.Stats.OnDeath -= Hide;
        current = null;
    }

    private void Select(IStructureTab tab)
    {
        shown?.Hide();
        shown = tab;
        foreach (IStructureTab other in tabs)
        {
            buttons[other].EnableInClassList("btn-on", other == tab);
            pages[other].EnableInClassList("hidden", other != tab);
        }
        tab.Show(current);
    }

    private void DrawHealth()
    {
        StatsHandler stats = current.Stats;
        float hp = Mathf.Clamp(stats.currHP, 0f, stats.maxHP);
        health.text = Mathf.CeilToInt(hp) + " / " + Mathf.CeilToInt(stats.maxHP);
        healthFill.style.width = Length.Percent(hp / stats.maxHP * 100f);
    }

    private void OnUpgraded(Structure old, Structure result)
    {
        if (!isOpen || old != current)
            return;

        if (HasTabs(result))
            Bind(result, shown);
        else
            Hide();
    }

    private void Subscribe(Button button, Action onClick)
    {
        button.clicked += onClick;
        unsubscribe.Add(() => button.clicked -= onClick);
    }
}
