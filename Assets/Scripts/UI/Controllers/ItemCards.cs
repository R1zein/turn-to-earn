using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

// Makes ItemCard elements from the template: icon, title, price and a click handler.
// Each click subscription goes into the caller's list so its panel can drop them in Dispose.
public class ItemCards
{
    [Inject] private UITemplate templates;

    public Button Add(VisualElement parent, string title, Sprite icon, AllResources price,
        Action onClick, List<Action> unsubscribe)
    {
        var card = templates.ItemCard.Instantiate().Q<Button>("Card");
        card.Q<Label>("Title").text = title;
        card.Q<Label>("Price").text = PriceText(price);
        card.Q("Icon").style.backgroundImage = new StyleBackground(icon);

        // Add the card itself, not the template's wrapper, so the row lays it out.
        parent.Add(card);
        card.clicked += onClick;
        unsubscribe.Add(() => card.clicked -= onClick);
        return card;
    }

    private static string PriceText(AllResources price)
    {
        var parts = new List<string>();
        if (price.tree > 0) parts.Add($"WOOD {price.tree}");
        if (price.ore > 0) parts.Add($"STONE {price.ore}");
        if (price.iron > 0) parts.Add($"IRON {price.iron}");
        if (price.copper > 0) parts.Add($"COPPER {price.copper}");
        if (price.gold > 0) parts.Add($"GOLD {price.gold}");
        return parts.Count == 0 ? "FREE" : string.Join("  ", parts);
    }
}
