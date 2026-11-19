using System;
using UnityEngine;
using UnityEngine.AI;

// A tower owns the weapon in its one socket. Destroying either body releases
// the right state: losing the weapon frees the socket, losing the tower takes both.
public class TowerMount : MonoBehaviour
{
    [SerializeField] private Transform socket;

    private Structure weapon;
    private AllResources installedCost = new AllResources();

    public Transform Socket => socket;
    public Structure Weapon => weapon;
    public bool HasWeapon => weapon != null && !weapon.Stats.IsDead;
    public AllResources InstalledCost => HasWeapon ? installedCost : new AllResources();
    public event Action Changed;

    public void Install(Structure placed, AllResources cost)
    {
        placed.transform.SetParent(socket, true);
        foreach (NavMeshObstacle obstacle in placed.GetComponentsInChildren<NavMeshObstacle>())
            obstacle.enabled = false;

        weapon = placed;
        installedCost = cost;
        weapon.Stats.OnDeath += OnWeaponDeath;
        Changed?.Invoke();
    }

    public void RemoveWeapon()
    {
        Structure removed = weapon;
        Detach();
        removed.gameObject.SetActive(false);
        Destroy(removed.gameObject);
        Changed?.Invoke();
    }

    private void OnWeaponDeath()
    {
        Detach();
        Changed?.Invoke();
    }

    private void OnDestroy() => Detach();

    private void Detach()
    {
        if (weapon != null)
            weapon.Stats.OnDeath -= OnWeaponDeath;
        weapon = null;
        installedCost = new AllResources();
    }
}
