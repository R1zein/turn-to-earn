using System;
using UnityEngine;

// What the shop and build panels offer. Each option is one ItemCard in a panel.

[Serializable]
public class BotOffer
{
    public NPCFacade Bot;
    public string Title;
    public Sprite Icon;
}

[Serializable]
public class BuildOption
{
    public Ghost Ghost;
    public string Title;
    public Sprite Icon;
}

// Fortifications are scene objects, so the option names one by its Fortification.ID.
[Serializable]
public class FortificationOption
{
    public string Id;
    public string Title;
    public Sprite Icon;
}

// A part is enabled on the chosen fortification by the part object's name.
[Serializable]
public class FortificationPartOption
{
    public string PartName;
    public string Title;
    public Sprite Icon;
}
