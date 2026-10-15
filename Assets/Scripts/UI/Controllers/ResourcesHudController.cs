using System;
using UnityEngine.UIElements;
using Zenject;

public class ResourcesHudController : IInitializable, IDisposable
{
    [Inject] private UIDocument document;
    [Inject] private ResourceWallet wallet;

    private Label stone, wood, iron, copper, gold;

    public void Initialize()
    {
        VisualElement root = document.rootVisualElement;
        stone = root.Q<Label>("StoneValue");
        wood = root.Q<Label>("WoodValue");
        iron = root.Q<Label>("IronValue");
        copper = root.Q<Label>("CopperValue");
        gold = root.Q<Label>("GoldValue");

        wallet.OnChanged += Redraw;
        Redraw(wallet.Current);
    }

    public void Dispose() => wallet.OnChanged -= Redraw;

    private void Redraw(AllResources resources)
    {
        stone.text = resources.ore.ToString();
        wood.text = resources.tree.ToString();
        iron.text = resources.iron.ToString();
        copper.text = resources.copper.ToString();
        gold.text = resources.gold.ToString();
    }
}
