using Unity.Netcode;
using UnityEngine;

/*
 * Interactable is the shared server-owned entry point for every world object a
 * player can use. Only the server mutates state. ItemPickup, ResourceNode, and
 * Receptacle each replicate their own state.
 */

public abstract class Interactable : NetworkBehaviour
{
    public abstract bool CanInteract(ObjectType heldType);

    public void ServerInteract(PlayerHeldItem heldItem)
    {
        if (!IsServer) return;

        if (CanInteract(heldItem.ObjectType))
            Interact(heldItem);
    }

    protected abstract void Interact(PlayerHeldItem heldItem);
}
