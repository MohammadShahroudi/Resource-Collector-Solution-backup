using Unity.Netcode;
using UnityEngine;

/*
 * ItemPickup is an item lying in the world. Picking it up copies its type onto
 * the player and despawns this object. In-scene pickups keep their GameObject
 * (Despawn(false)); catalog drops are destroyed. Late joiners do not see taken
 * items: dynamic ones are gone, and a taken scene pickup is hidden below.
 */

public class ItemPickup : Interactable
{
    [SerializeField] ObjectType _objectType;

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        // Despawn(false) leaves the GameObject active and visible. Netcode also
        // runs this callback on a late joiner's copy of a taken scene pickup.
        // A session ending despawns everything too; hiding then would leave
        // uncollected pickups inactive for the next join.
        if (NetworkObject.InScenePlaced && !NetworkManager.ShutdownInProgress)
            gameObject.SetActive(false);
    }

    public override bool CanInteract(ObjectType heldType)
    {
        return true;
    }

    protected override void Interact(PlayerHeldItem heldItem)
    {
        heldItem.DropHeldItem(transform.position);
        heldItem.SetHeldItem(_objectType);
        NetworkObject.Despawn(!NetworkObject.InScenePlaced);
    }
}
