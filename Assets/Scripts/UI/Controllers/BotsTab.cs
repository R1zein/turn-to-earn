using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

// BOTS at the shop table: buy helper bots. Bought bots appear at the table's spawn
// point; ShopService takes the price.
public class BotsTab : IStructureTab, IInitializable, IDisposable
{
    [Inject] private UIDocument document;
    [Inject] private ItemCards cards;
    [Inject] private GameConfig config;
    [Inject] private ShopService shop;

    private readonly List<Action> unsubscribe = new List<Action>();
    private readonly List<(Button card, NPCFacade bot)> botCards = new List<(Button, NPCFacade)>();
    private Vector3 spawnPosition;

    public string Name => "Bots";

    public void Initialize()
    {
        VisualElement list = document.rootVisualElement.Q("BotOptions");
        foreach (BotOffer offer in config.BotOffers)
        {
            NPCFacade bot = offer.Bot;
            Button card = cards.Add(list, offer.Title, offer.Icon, bot.requiredResources,
                () => Buy(bot), unsubscribe);
            botCards.Add((card, bot));
        }
    }

    public void Dispose()
    {
        foreach (Action action in unsubscribe)
            action();
    }

    public bool IsFor(Structure structure) => structure.TryGetComponent(out ShopTable _);

    public void Show(Structure structure)
    {
        spawnPosition = structure.GetComponent<ShopTable>().spawnPosition.position;
        RefreshPrices();
    }

    public void Hide()
    {
    }

    private void Buy(NPCFacade bot)
    {
        shop.TryBuyBot(bot, spawnPosition);
        RefreshPrices();
    }

    private void RefreshPrices()
    {
        foreach (var (card, bot) in botCards)
            card.SetEnabled(shop.CanAfford(bot));
    }
}
