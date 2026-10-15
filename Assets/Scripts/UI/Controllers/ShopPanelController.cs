using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

// Shop at the table: a Bots page (buy helper bots) and a Base page (pick a base
// site, then build its parts). Only forwards choices to ShopService and
// FortificationService; opened by ShopTable, closed by its button or Esc/Tab.
public class ShopPanelController : IInitializable, IDisposable
{
    [Inject] private UIDocument document;
    [Inject] private ItemCards cards;
    [Inject] private GameConfig config;
    [Inject] private InputService input;
    [Inject] private ShopService shop;
    [Inject] private FortificationService fortifications;

    private readonly List<Action> unsubscribe = new List<Action>();
    private readonly List<(Button card, NPCFacade bot)> botCards = new List<(Button, NPCFacade)>();
    private readonly List<(Button card, string partName)> partCards = new List<(Button, string)>();
    private VisualElement panel, botsPage, basePage, fortificationList, partList;
    private Label baseHint;
    private Button botsTab, baseTab;
    private Vector3 botSpawnPosition;
    private bool isOpen;

    public void Initialize()
    {
        panel = document.rootVisualElement.Q("ShopPanel");
        botsPage = panel.Q("BotsPage");
        basePage = panel.Q("BasePage");
        fortificationList = panel.Q("FortificationOptions");
        partList = panel.Q("PartOptions");
        baseHint = panel.Q<Label>("BaseHint");
        botsTab = panel.Q<Button>("BotsTab");
        baseTab = panel.Q<Button>("BaseTab");

        VisualElement botList = panel.Q("BotOptions");
        foreach (BotOffer offer in config.BotOffers)
        {
            NPCFacade bot = offer.Bot;
            Button card = cards.Add(botList, offer.Title, offer.Icon, bot.requiredResources,
                () => BuyBot(bot), unsubscribe);
            botCards.Add((card, bot));
        }
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

        Subscribe(botsTab, ShowBots);
        Subscribe(baseTab, ShowBase);
        Subscribe(panel.Q<Button>("CloseButton"), Hide);
        input.OnUiClose += Hide;
    }

    public void Dispose()
    {
        input.OnUiClose -= Hide;
        foreach (Action action in unsubscribe)
            action();
    }

    public void Show(Vector3 spawnPosition)
    {
        if (isOpen)
            return;

        isOpen = true;
        botSpawnPosition = spawnPosition;
        ShowBots();
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

    private void ShowBots()
    {
        SetPage(isBots: true);
        RefreshBotPrices();
    }

    private void ShowBase()
    {
        SetPage(isBots: false);
        bool isChosen = fortifications.Current != null;
        fortificationList.EnableInClassList("hidden", isChosen);
        partList.EnableInClassList("hidden", !isChosen);
        baseHint.text = isChosen ? "BUILD PARTS OF THE BASE" : "CHOOSE WHERE TO BUILD THE BASE";
        RefreshParts();
    }

    private void SetPage(bool isBots)
    {
        botsPage.EnableInClassList("hidden", !isBots);
        basePage.EnableInClassList("hidden", isBots);
        botsTab.EnableInClassList("btn-on", isBots);
        baseTab.EnableInClassList("btn-on", !isBots);
    }

    private void BuyBot(NPCFacade bot)
    {
        shop.TryBuyBot(bot, botSpawnPosition);
        RefreshBotPrices();
    }

    private void BuildFortification(string id)
    {
        fortifications.TryBuild(id);
        ShowBase();
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

    private void RefreshBotPrices()
    {
        foreach (var (card, bot) in botCards)
            card.SetEnabled(shop.CanAfford(bot));
    }

    private void Subscribe(Button button, Action onClick)
    {
        button.clicked += onClick;
        unsubscribe.Add(() => button.clicked -= onClick);
    }
}
