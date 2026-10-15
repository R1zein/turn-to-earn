using Zenject;

// Base building: the player picks one of the level's base sites, then builds its
// parts. Only one site can ever be chosen (known issue P1-5, kept as it was).
public class FortificationService
{
    [Inject] private LevelAnchors anchors;

    public Fortification Current { get; private set; }

    public bool TryBuild(string id)
    {
        if (Current != null)
            return false;

        foreach (Fortification fortification in anchors.Fortifications)
        {
            if (fortification.ID == id)
            {
                Current = fortification;
                fortification.gameObject.SetActive(true);
                return true;
            }
        }
        return false;
    }

    // Each part is built once; repeated requests do nothing.
    public bool TryBuildPart(string partName) => Current != null && Current.TryBuildPart(partName);

    public bool IsPartBuilt(string partName) => Current != null && Current.IsPartBuilt(partName);
}
