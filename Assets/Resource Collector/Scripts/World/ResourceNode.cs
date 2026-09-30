using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/*
 * ResourceNode is a harvestable object like a tree or stone. Replicated health
 * counts down as players hit it with the right tool; at zero the server spawns
 * resource pickups and every client hides the depleted node.
 */

public class ResourceNode : Interactable
{
    [SerializeField] List<ObjectType> _toolTypeRequired = new();
    [SerializeField] NetworkObject _producedPrefab;
    [SerializeField] int _amountToSpawn = 3;
    [SerializeField] int _startingHealth = 1;
    [SerializeField] AudioClip _audioClip;

    readonly NetworkVariable<int> _health = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
            _health.Value = _startingHealth;

        _health.OnValueChanged += HandleHealthChanged;
        HandleHealthChanged(_health.Value, _health.Value);
    }

    public override void OnNetworkDespawn()
    {
        _health.OnValueChanged -= HandleHealthChanged;
        base.OnNetworkDespawn();
    }

    public override bool CanInteract(ObjectType heldType)
    {
        return _health.Value > 0 && _toolTypeRequired.Contains(heldType);
    }

    protected override void Interact(PlayerHeldItem heldItem)
    {
        _health.Value = Mathf.Max(0, _health.Value - 1);
        HitFeedbackRpc();

        if (_health.Value > 0) return;

        for (int i = 0; i < _amountToSpawn; i++)
        {
            Vector3 dropPosition = transform.position;
            dropPosition.y = 0f;
            Vector2 offset = Random.insideUnitCircle;
            dropPosition.x += offset.x;
            dropPosition.z += offset.y;

            NetworkObject.InstantiateAndSpawn(_producedPrefab.gameObject, NetworkManager,
                position: dropPosition, rotation: Quaternion.Euler(0f, Random.Range(0, 360), 0f));
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    void HitFeedbackRpc()
    {
        if (_audioClip != null)
            AudioSource.PlayClipAtPoint(_audioClip, transform.position);
    }

    void HandleHealthChanged(int previousValue, int newValue)
    {
        bool isVisible = newValue > 0;
        GetComponent<Renderer>().enabled = isVisible;
        GetComponent<Collider>().enabled = isVisible;
    }
}
