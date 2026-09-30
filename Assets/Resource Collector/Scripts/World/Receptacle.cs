using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/*
 * Receptacle collects one resource type, like a pallet accepting wood. A
 * replicated stack count drives which stacked visuals are shown, so current
 * and late-joining clients render the same pile.
 */

public class Receptacle : Interactable
{
    [SerializeField] ObjectType _acceptedObjectType;
    [SerializeField] List<GameObject> _stackedResourceVisuals = new();
    [SerializeField] AudioClip _audioClip;

    readonly NetworkVariable<int> _stackedCount = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
            _stackedCount.Value = 0;

        _stackedCount.OnValueChanged += HandleStackedCountChanged;
        HandleStackedCountChanged(_stackedCount.Value, _stackedCount.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _stackedCount.OnValueChanged -= HandleStackedCountChanged;
    }

    public override bool CanInteract(ObjectType heldType)
    {
        return heldType == _acceptedObjectType && _stackedCount.Value < _stackedResourceVisuals.Count;
    }

    protected override void Interact(PlayerHeldItem heldItem)
    {
        _stackedCount.Value++;
        heldItem.Clear();
    }

    void HandleStackedCountChanged(int previousValue, int newValue)
    {
        for (int i = 0; i < _stackedResourceVisuals.Count; i++)
            _stackedResourceVisuals[i].SetActive(i < newValue);

        if (newValue > previousValue && _audioClip != null)
            AudioSource.PlayClipAtPoint(_audioClip, transform.position);
    }
}
