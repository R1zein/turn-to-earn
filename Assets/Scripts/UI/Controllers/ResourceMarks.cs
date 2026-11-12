using UnityEngine.UIElements;

// Fills an element made from the ResourceMarks template. A price shows only what it
// asks for (FREE if nothing) and turns hot what the wallet is short of; the wallet
// itself shows every resource.
public class ResourceMarks
{
    public void Show(VisualElement marks, AllResources amount, AllResources have, bool isPrice)
    {
        bool asksAnything = false;
        asksAnything |= ShowMark(marks.Q("Wood"), amount.tree, have.tree, isPrice);
        asksAnything |= ShowMark(marks.Q("Stone"), amount.ore, have.ore, isPrice);
        asksAnything |= ShowMark(marks.Q("Iron"), amount.iron, have.iron, isPrice);
        asksAnything |= ShowMark(marks.Q("Copper"), amount.copper, have.copper, isPrice);
        asksAnything |= ShowMark(marks.Q("Gold"), amount.gold, have.gold, isPrice);
        marks.Q("Free").EnableInClassList("hidden", !isPrice || asksAnything);
    }

    private static bool ShowMark(VisualElement mark, int value, int have, bool isPrice)
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
}
