using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Zenject;

// MODIFY: what the structure can turn into in its own place, a card per upgrade.
// The card under the mouse is described below the cards. A card is faded when the
// wallet is short or no result fits on the grid, and the description says which;
// it still answers the mouse, so the reason can be read. Rules are StructureUpgrades'.
public class ModifyTab : IStructureTab, IInitializable, IDisposable
{
    [Inject] private UIDocument document;
    [Inject] private UITemplate templates;
    [Inject] private StructureUpgrades upgrades;
    [Inject] private ResourceWallet wallet;
    [Inject] private ResourceMarks marks;

    private readonly List<(Button card, StructureUpgrade upgrade)> cards = new List<(Button, StructureUpgrade)>();
    private VisualElement list, detailIcon, detailPrice;
    private Label detailTitle, detailDescription, detailHint;
    private Structure structure;
    private Button lit;
    private StructureUpgrade described;

    public string Name => "Modify";

    public void Initialize()
    {
        VisualElement page = document.rootVisualElement.Q("ModifyPage");
        list = page.Q("UpgradeOptions");
        detailIcon = page.Q("UpgradeIcon");
        detailPrice = page.Q("UpgradePrice");
        detailTitle = page.Q<Label>("UpgradeTitle");
        detailDescription = page.Q<Label>("UpgradeDescription");
        detailHint = page.Q<Label>("UpgradeHint");

        wallet.OnChanged += OnWalletChanged;
    }

    public void Dispose() => wallet.OnChanged -= OnWalletChanged;

    public bool IsFor(Structure structure) => structure.Upgrades.Count > 0;

    // The cards are the structure's own, so they are made anew for each one shown.
    public void Show(Structure shownStructure)
    {
        structure = shownStructure;
        list.Clear();
        cards.Clear();
        foreach (StructureUpgrade upgrade in structure.Upgrades)
            cards.Add((AddCard(upgrade), upgrade));

        lit = null;
        Refresh();
        Describe(cards[0].card, cards[0].upgrade);
    }

    public void Hide() => structure = null;

    private Button AddCard(StructureUpgrade upgrade)
    {
        var card = templates.BuildCard.Instantiate().Q<Button>("Card");
        card.Q<Label>("Title").text = upgrade.Result.Title;
        card.Q("Icon").style.backgroundImage = new StyleBackground(upgrade.Result.Icon);
        // The card itself, not the template's wrapper, so the grid lays it out.
        list.Add(card);

        card.clicked += () => upgrades.TryUpgrade(structure, upgrade);
        card.RegisterCallback<PointerEnterEvent>(_ => Describe(card, upgrade));
        return card;
    }

    // Bots keep mining while the window is open, so what can be paid for changes under it.
    private void OnWalletChanged(AllResources _)
    {
        if (structure != null)
            Refresh();
    }

    private void Refresh()
    {
        foreach (var (card, upgrade) in cards)
        {
            marks.Show(card.Q("Price"), upgrade.Price, wallet.Current, isPrice: true);
            card.EnableInClassList("btn-faded", !CanUpgrade(upgrade));
        }
        if (lit != null)
            Describe(lit, described);
    }

    private bool CanUpgrade(StructureUpgrade upgrade) =>
        upgrades.CanAfford(upgrade) && upgrades.Fitting(structure, upgrade) != null;

    private void Describe(Button card, StructureUpgrade upgrade)
    {
        lit?.RemoveFromClassList("btn-lit");
        lit = card;
        described = upgrade;
        lit.AddToClassList("btn-lit");

        detailIcon.style.backgroundImage = new StyleBackground(upgrade.Result.Icon);
        detailTitle.text = upgrade.Result.Title;
        detailDescription.text = upgrade.Description;

        bool fits = upgrades.Fitting(structure, upgrade) != null;
        bool canAfford = upgrades.CanAfford(upgrade);
        detailHint.text = !fits ? "NO ROOM - SOMETHING STANDS IN THE WAY"
            : !canAfford ? "NOT ENOUGH"
            : "CLICK - UPGRADE AND PAY";
        bool isOk = fits && canAfford;
        // .heavy is only for muted captions: it outlines in the muted colour.
        detailHint.EnableInClassList("text-muted", isOk);
        detailHint.EnableInClassList("heavy", isOk);
        detailHint.EnableInClassList("text-hot", !isOk);
        marks.Show(detailPrice, upgrade.Price, wallet.Current, isPrice: true);
    }
}
