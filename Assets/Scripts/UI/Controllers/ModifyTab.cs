using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Zenject;

public class ModifyTab : IStructureTab, IInitializable, IDisposable
{
    [Inject] private UIDocument document;
    [Inject] private StructureUpgrades upgrades;
    [Inject] private ResourceWallet wallet;
    [Inject] private ResourceMarks marks;

    private readonly List<(Button action, VisualElement price, Label status, StructureUpgrade upgrade)> rows =
        new List<(Button, VisualElement, Label, StructureUpgrade)>();
    private VisualElement options;
    private VisualTreeAsset rowTemplate;
    private Structure structure;
    private TowerMount mount;

    public string Name => "Modify";

    public void Initialize()
    {
        options = document.rootVisualElement.Q("UpgradeOptions");
        rowTemplate = document.rootVisualElement.Q<TemplateContainer>("UpgradeRowTemplate").templateSource;
        wallet.OnChanged += OnWalletChanged;
    }

    public void Dispose()
    {
        Hide();
        wallet.OnChanged -= OnWalletChanged;
    }

    public bool IsFor(Structure structure) => structure.Upgrades.Count > 0;

    public void Show(Structure shownStructure)
    {
        structure = shownStructure;
        if (structure.TryGetComponent(out mount))
            mount.Changed += Rebuild;
        Rebuild();
    }

    public void Hide()
    {
        if (mount != null)
            mount.Changed -= Rebuild;
        mount = null;
        structure = null;
    }

    private void Rebuild()
    {
        options.Clear();
        rows.Clear();
        foreach (StructureUpgrade upgrade in structure.Upgrades)
        {
            if (!upgrades.IsOffered(structure, upgrade))
                continue;

            TemplateContainer row = rowTemplate.Instantiate();
            Structure preview = upgrade.Action == StructureUpgradeAction.RemoveMount ? mount.Weapon : upgrade.Result;
            row.Q("UpgradeIcon").style.backgroundImage = new StyleBackground(preview.Icon);
            row.Q<Label>("UpgradeTitle").text = preview.Title;
            row.Q<Label>("UpgradeDescription").text = upgrade.Description;

            Button action = row.Q<Button>("UpgradeAction");
            action.text = upgrade.ActionLabel;
            action.clicked += () => upgrades.TryUpgrade(structure, upgrade);
            rows.Add((action, row.Q("UpgradePrice"), row.Q<Label>("UpgradeStatus"), upgrade));
            options.Add(row);
        }
        Refresh();
    }

    private void OnWalletChanged(AllResources _)
    {
        if (structure != null)
            Refresh();
    }

    private void Refresh()
    {
        foreach (var (action, price, status, upgrade) in rows)
        {
            bool fits = upgrades.Fitting(structure, upgrade) != null;
            bool canAfford = upgrades.CanAfford(upgrade);
            action.SetEnabled(fits && canAfford);
            status.text = !fits ? "NO ROOM OR INVALID POSITION" : !canAfford ? "NOT ENOUGH RESOURCES" : string.Empty;
            status.EnableInClassList("hidden", fits && canAfford);
            marks.Show(price, upgrade.Price, wallet.Current, isPrice: true);
        }
    }
}
