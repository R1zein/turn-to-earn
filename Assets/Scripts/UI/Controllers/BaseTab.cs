using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Zenject;

// BASE at the shop table: pick a base site, then build its parts. Only forwards the
// choice to FortificationService.
public class BaseTab : IStructureTab, IInitializable, IDisposable
{
    [Inject] private UIDocument document;
    [Inject] private ItemCards cards;
    [Inject] private GameConfig config;
    [Inject] private FortificationService fortifications;

    private readonly List<Action> unsubscribe = new List<Action>();
    private readonly List<(Button card, string partName)> partCards = new List<(Button, string)>();
    private VisualElement fortificationList, partList;
    private Label hint;

    public string Name => "Base";

    public void Initialize()
    {
        VisualElement root = document.rootVisualElement;
        fortificationList = root.Q("FortificationOptions");
        partList = root.Q("PartOptions");
        hint = root.Q<Label>("BaseHint");

        foreach (FortificationOption option in config.FortificationOptions)
        {
            string id = option.Id;
            cards.Add(fortificationList, option.Title, option.Icon, new AllResources(),
                () => BuildFortification(id), unsubscribe);
        }
        foreach (FortificationPartOption option in config.FortificationParts)
        {
            string partName = option.PartName;
            Button card = cards.Add(partList, option.Title, option.Icon, new AllResources(),
                () => BuildPart(partName), unsubscribe);
            partCards.Add((card, partName));
        }
    }

    public void Dispose()
    {
        foreach (Action action in unsubscribe)
            action();
    }

    public bool IsFor(Structure structure) => structure.TryGetComponent(out ShopTable _);

    public void Show(Structure structure) => Redraw();

    public void Hide()
    {
    }

    private void Redraw()
    {
        bool isChosen = fortifications.Current != null;
        fortificationList.EnableInClassList("hidden", isChosen);
        partList.EnableInClassList("hidden", !isChosen);
        hint.text = isChosen ? "BUILD PARTS OF THE BASE" : "CHOOSE WHERE TO BUILD THE BASE";
        RefreshParts();
    }

    private void BuildFortification(string id)
    {
        fortifications.TryBuild(id);
        Redraw();
    }

    private void BuildPart(string partName)
    {
        fortifications.TryBuildPart(partName);
        RefreshParts();
    }

    // A built part's card goes dark and says so: each part is built once.
    private void RefreshParts()
    {
        foreach (var (card, partName) in partCards)
        {
            bool isBuilt = fortifications.IsPartBuilt(partName);
            card.SetEnabled(!isBuilt);
            if (isBuilt)
                card.Q<Label>("Price").text = "BUILT";
        }
    }
}
