using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

// Build menu: Tab opens it, a tab on the left picks a category, a card picks a ghost,
// Esc/Tab closes. The card under the mouse is described at the bottom: price, size in
// cells, health. A card the wallet cannot pay for is faded but still answers the mouse,
// so hovering it shows what is missing. Rules are BuildService's.
// DISMANTLE in the header starts dismantling; while that is on, Tab stops it instead of opening.
public class BuildPanelController : IInitializable, IDisposable
{
    [Inject] private UIDocument document;
    [Inject] private UITemplate templates;
    [Inject] private GameConfig config;
    [Inject] private InputService input;
    [Inject] private BuildService buildService;
    [Inject] private Dismantler dismantler;
    [Inject] private ResourceWallet wallet;

    private readonly List<Action> unsubscribe = new List<Action>();
    private readonly List<(Button card, BuildOption option)> options = new List<(Button, BuildOption)>();
    private readonly Dictionary<BuildCategory, Button> tabs = new Dictionary<BuildCategory, Button>();
    private VisualElement panel, walletMarks, detail, detailIcon, detailPrice;
    private Label detailTitle, detailSize, detailHealth, detailHint;
    private Button lit;
    private BuildOption described;
    private bool isOpen;

    public void Initialize()
    {
        panel = document.rootVisualElement.Q("BuildPanel");
        walletMarks = panel.Q("Wallet");
        detail = panel.Q("BuildDetail");
        detailIcon = panel.Q("DetailIcon");
        detailPrice = panel.Q("DetailPrice");
        detailTitle = panel.Q<Label>("DetailTitle");
        detailSize = panel.Q<Label>("DetailSize");
        detailHealth = panel.Q<Label>("DetailHealth");
        detailHint = panel.Q<Label>("DetailHint");

        VisualElement list = panel.Q("BuildOptions");
        foreach (BuildOption option in config.BuildOptions)
            options.Add((AddCard(list, option), option));

        foreach (BuildCategory category in Enum.GetValues(typeof(BuildCategory)))
        {
            Button tab = panel.Q<Button>(category + "Tab");
            tab.text += "  " + options.FindAll(o => o.option.Category == category).Count;
            BuildCategory picked = category;
            Subscribe(tab, () => ShowCategory(picked));
            tabs.Add(category, tab);
        }

        panel.Q("DismantleIcon").style.backgroundImage = new StyleBackground(config.DismantleIcon);
        Subscribe(panel.Q<Button>("DismantleButton"), StartDismantling);

        input.OnBuildMenu += Show;
        input.OnUiClose += Hide;
        wallet.OnChanged += OnWalletChanged;
        ShowCategory(BuildCategory.Fort);
    }

    public void Dispose()
    {
        input.OnBuildMenu -= Show;
        input.OnUiClose -= Hide;
        wallet.OnChanged -= OnWalletChanged;
        foreach (Action action in unsubscribe)
            action();
    }

    private Button AddCard(VisualElement list, BuildOption option)
    {
        var card = templates.BuildCard.Instantiate().Q<Button>("Card");
        card.Q<Label>("Title").text = option.Title;
        card.Q("Icon").style.backgroundImage = new StyleBackground(option.Icon);
        // Add the card itself, not the template's wrapper, so the grid lays it out.
        list.Add(card);

        Subscribe(card, () => Pick(option.Ghost));
        EventCallback<PointerEnterEvent> onEnter = _ => Describe(card, option);
        card.RegisterCallback(onEnter);
        unsubscribe.Add(() => card.UnregisterCallback(onEnter));
        return card;
    }

    private void Show()
    {
        if (isOpen)
            return;

        if (dismantler.IsActive)
        {
            dismantler.End();
            return;
        }

        isOpen = true;
        Refresh();
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

    // Bots keep mining while the menu is open, so what can be paid for changes under it.
    private void OnWalletChanged(AllResources _)
    {
        if (isOpen)
            Refresh();
    }

    private void Refresh()
    {
        AllResources have = wallet.Current;
        ShowMarks(walletMarks, have, have, isPrice: false);
        foreach (var (card, option) in options)
        {
            ShowMarks(card.Q("Price"), option.Ghost.requiredResources, have, isPrice: true);
            card.EnableInClassList("btn-faded", !buildService.CanAfford(option.Ghost));
        }
        Describe(lit, described);
    }

    private void ShowCategory(BuildCategory category)
    {
        foreach (var pair in tabs)
            pair.Value.EnableInClassList("btn-on", pair.Key == category);

        Button first = null;
        BuildOption firstOption = null;
        foreach (var (card, option) in options)
        {
            bool isShown = option.Category == category;
            card.EnableInClassList("hidden", !isShown);
            if (isShown && first == null)
            {
                first = card;
                firstOption = option;
            }
        }
        Describe(first, firstOption);
    }

    // A category with no buildings leaves nothing to describe.
    private void Describe(Button card, BuildOption option)
    {
        lit?.RemoveFromClassList("btn-lit");
        lit = card;
        described = option;
        lit?.AddToClassList("btn-lit");

        detail.EnableInClassList("hidden", option == null);
        if (option == null)
            return;

        Ghost ghost = option.Ghost;
        detailIcon.style.backgroundImage = new StyleBackground(option.Icon);
        detailTitle.text = option.Title;

        // Size and health are read off what the ghost places. The shop table stands
        // off the grid, so it has no size in cells.
        GridBuilding onGrid = ghost.prefab.GetComponent<GridBuilding>();
        detailSize.EnableInClassList("hidden", onGrid == null);
        if (onGrid != null)
        {
            Vector2Int size = onGrid.SizeTurned(0);
            detailSize.text = $"SIZE {size.x} x {size.y}";
        }
        Structure structure = ghost.prefab.GetComponent<Structure>();
        detailHealth.EnableInClassList("hidden", structure == null);
        if (structure != null)
            detailHealth.text = $"HP {Mathf.CeilToInt(structure.GetComponent<StatsHandler>().maxHP)}";

        bool canAfford = buildService.CanAfford(ghost);
        detailHint.text = canAfford ? "CLICK - PICK   F - PLACE AND PAY" : "NOT ENOUGH";
        // .heavy is only for muted captions: it outlines in the muted colour.
        detailHint.EnableInClassList("text-muted", canAfford);
        detailHint.EnableInClassList("heavy", canAfford);
        detailHint.EnableInClassList("text-hot", !canAfford);
        ShowMarks(detailPrice, ghost.requiredResources, wallet.Current, isPrice: true);
    }

    // A price shows only what it asks for (FREE if nothing) and turns hot what the
    // wallet is short of; the wallet itself shows every resource.
    private void ShowMarks(VisualElement marks, AllResources amount, AllResources have, bool isPrice)
    {
        bool asksAnything = false;
        asksAnything |= ShowMark(marks.Q("Wood"), amount.tree, have.tree, isPrice);
        asksAnything |= ShowMark(marks.Q("Stone"), amount.ore, have.ore, isPrice);
        asksAnything |= ShowMark(marks.Q("Iron"), amount.iron, have.iron, isPrice);
        asksAnything |= ShowMark(marks.Q("Copper"), amount.copper, have.copper, isPrice);
        asksAnything |= ShowMark(marks.Q("Gold"), amount.gold, have.gold, isPrice);
        marks.Q("Free").EnableInClassList("hidden", !isPrice || asksAnything);
    }

    private bool ShowMark(VisualElement mark, int value, int have, bool isPrice)
    {
        bool isShown = !isPrice || value > 0;
        mark.EnableInClassList("hidden", !isShown);
        Label number = mark.Q<Label>("Value");
        number.text = value.ToString();
        bool isShort = isPrice && value > have;
        number.EnableInClassList("text-hot", isShort);
        number.EnableInClassList("text-muted", !isShort);
        return isPrice && isShown;
    }

    private void StartDismantling()
    {
        dismantler.Begin();
        Hide();
    }

    // A faded card still takes the click; picking needs the full price, so it stays open.
    private void Pick(Ghost ghost)
    {
        if (!buildService.CanAfford(ghost))
            return;

        buildService.Pick(ghost);
        Hide();
    }

    private void Subscribe(Button button, Action onClick)
    {
        button.clicked += onClick;
        unsubscribe.Add(() => button.clicked -= onClick);
    }
}
