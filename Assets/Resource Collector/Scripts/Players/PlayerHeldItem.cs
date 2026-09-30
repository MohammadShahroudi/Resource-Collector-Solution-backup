using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/*
 * PlayerHeldItem tracks which resource or tool a player is carrying and shows
 * the matching held model on every client.
 *
 * The server changes the held type. Every client subscribes in OnNetworkSpawn
 * and applies the current value immediately so a late joiner sees the right
 * model. That first application matters because a NetworkVariable may already
 * contain its synchronized value before the change callback is registered.
 */

public class PlayerHeldItem : NetworkBehaviour
{
    [Serializable]
    public struct ItemCatalogEntry
    {
        public ObjectType type;
        public GameObject model;
        public NetworkObject prefab;
    }

    [Header("Item Catalog")]
    [SerializeField] List<ItemCatalogEntry> _itemCatalog = new();

    public ObjectType ObjectType => _heldObjectType.Value;

    readonly NetworkVariable<ObjectType> _heldObjectType = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _heldObjectType.OnValueChanged += HandleObjectTypeChanged;
        HandleObjectTypeChanged(ObjectType.None, _heldObjectType.Value);
    }

    public override void OnNetworkPreDespawn()
    {
        base.OnNetworkPreDespawn();

        // Every peer runs this callback, but only the server may create the drop.
        // If a client left, the server is still running and returns that player's
        // held item to the world. If the host is shutting down, every player is
        // despawning and ShutdownInProgress prevents drops into the dying session.
        if (!IsServer) return;
        if (!NetworkManager.ShutdownInProgress)
            DropHeldItem(transform.position);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        _heldObjectType.OnValueChanged -= HandleObjectTypeChanged;
    }

    public void SetHeldItem(ObjectType objectType)
    {
        if (!IsServer) return;

        _heldObjectType.Value = objectType;
    }

    public void Clear()
    {
        if (!IsServer) return;

        _heldObjectType.Value = ObjectType.None;
    }

    // Drops the held item back into the world at the requested position, then
    // empties the hand. Used for swap and for drop-on-disconnect.
    public void DropHeldItem(Vector3 position)
    {
        if (!IsServer) return;

        if (_heldObjectType.Value == ObjectType.None) return;

        ItemCatalogEntry matchingEntry = _itemCatalog.Find((item) => item.type == _heldObjectType.Value);
        NetworkObject.InstantiateAndSpawn(matchingEntry.prefab.gameObject, NetworkManager, position: position);

        Clear();
    }

    void HandleObjectTypeChanged(ObjectType previousValue, ObjectType newValue)
    {
        foreach (var item in _itemCatalog)
            item.model.SetActive(item.type == newValue);
    }
}
