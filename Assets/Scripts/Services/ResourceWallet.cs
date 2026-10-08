using System;

// The player's resources for the current scene. Lives in the scene container,
// so reloading the scene starts from an empty wallet.
public class ResourceWallet
{
    public AllResources Current { get; private set; } = new AllResources();
    public event Action<AllResources> OnChanged;

    public void Add(AllResources amount)
    {
        Current += amount;
        OnChanged?.Invoke(Current);
    }

    // Check and spend in one call, so the balance can never go negative.
    public bool TrySpend(AllResources price)
    {
        if (!(Current >= price))
            return false;

        Current -= price;
        OnChanged?.Invoke(Current);
        return true;
    }
}
