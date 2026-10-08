using TMPro;
using UnityEngine;
using Zenject;

// Interim uGUI display of the wallet; replaced by a UI Toolkit
// ResourcesHudController at stage 7 of the architecture plan.
public class ResourcesView : MonoBehaviour
{
    [SerializeField] TMP_Text playerOreStoreText;
    [SerializeField] TMP_Text playerTreeStoreText;
    [SerializeField] TMP_Text playerIronStoreText;
    [SerializeField] TMP_Text playerGoldStoreText;

    [Inject] private ResourceWallet wallet;

    private void OnEnable()
    {
        wallet.OnChanged += Redraw;
        Redraw(wallet.Current);
    }

    private void OnDisable()
    {
        wallet.OnChanged -= Redraw;
    }

    private void Redraw(AllResources resources)
    {
        playerOreStoreText.text = $"Добыто камня: {resources.ore}";
        playerTreeStoreText.text = $"Добыто дерева: {resources.tree}";
        playerIronStoreText.text = $"Добыто железа: {resources.iron}";
        playerGoldStoreText.text = $"Добыто золота: {resources.gold}";
    }
}
