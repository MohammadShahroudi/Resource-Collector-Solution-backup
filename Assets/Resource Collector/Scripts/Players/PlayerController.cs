using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * PlayerController is the owner's local input loop: movement, target
 * selection, and the client-to-server interaction request. The server
 * still owns every world mutation.
 */

public class PlayerController : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] CharacterController _characterController;
    [SerializeField] Animator _animator;
    [SerializeField] PlayerHeldItem _heldItem;

    [Header("Detection")]
    [SerializeField] float _detectionRadius = 3f;
    [SerializeField] float _detectionAngle = 60f;
    [SerializeField] LayerMask _pickupLayer;

    [Header("Movement")]
    [SerializeField] float _movementSpeed = 4f;
    [SerializeField] float _rotationSpeed = 200f;

    Interactable _closestTarget;
    Vector2 _smoothedInput;

    void Update()
    {
        if (!IsOwner) return;

        Vector2 movementInput = ReadMovementInput();
        _smoothedInput = Vector2.MoveTowards(_smoothedInput, movementInput, Time.deltaTime * 10f);

        transform.Rotate(Vector3.up, _smoothedInput.x * _rotationSpeed * Time.deltaTime);

        Vector3 motion = _characterController.transform.forward * _smoothedInput.y * _movementSpeed * Time.deltaTime;
        _characterController.Move(motion);

        _animator.SetFloat("Speed", _characterController.velocity.magnitude);

        UpdateInteractionTarget();

        if (Keyboard.current.eKey.wasPressedThisFrame || Mouse.current.leftButton.wasPressedThisFrame)
            HandleInteractionPressed();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
            Camera.main.GetComponent<FollowCamera>().Target = transform;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
            ClearSelection();

        base.OnNetworkDespawn();
    }

    void HandleInteractionPressed()
    {
        if (!IsOwner) return;

        if (_closestTarget == null) return;
        _animator.SetTrigger("Interact");
        RequestInteractRpc(_closestTarget.NetworkObjectId);
    }

    static Vector2 ReadMovementInput()
    {
        Vector2 movementInput = Vector2.zero;
        movementInput.x += Keyboard.current.aKey.isPressed ? -1f : 0f;
        movementInput.x += Keyboard.current.dKey.isPressed ? 1f : 0f;
        movementInput.y += Keyboard.current.wKey.isPressed ? 1f : 0f;
        movementInput.y += Keyboard.current.sKey.isPressed ? -1f : 0f;

        return movementInput;
    }

    void UpdateInteractionTarget()
    {
        Interactable candidate = FindClosestValidInteractable();
        if (candidate == _closestTarget) return;

        ClearSelection();

        if (candidate != null)
        {
            candidate.GetComponent<Highlightable>().SetHighlighted(true);
            _closestTarget = candidate;
        }
    }

    Interactable FindClosestValidInteractable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _detectionRadius, _pickupLayer);
        float closestDistance = float.MaxValue;
        Interactable candidate = null;

        foreach (Collider hit in hits)
        {
            if (!hit.TryGetComponent(out Interactable target)) continue;
            if (!target.CanInteract(_heldItem.ObjectType)) continue;

            Vector3 directionToTarget = (hit.transform.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, directionToTarget) > _detectionAngle) continue;

            float distance = Vector3.Distance(transform.position, hit.transform.position);
            if (distance > closestDistance) continue;

            closestDistance = distance;
            candidate = target;
        }

        return candidate;
    }

    void ClearSelection()
    {
        if (_closestTarget != null)
            _closestTarget.GetComponent<Highlightable>().SetHighlighted(false);

        _closestTarget = null;
    }

    [Rpc(SendTo.Server)]
    void RequestInteractRpc(ulong networkObjectId)
    {
        Dictionary<ulong, NetworkObject> spawnedObjectMap = NetworkManager.SpawnManager.SpawnedObjects;
        if (!spawnedObjectMap.TryGetValue(networkObjectId, out NetworkObject spawnedObject))
        {
            Debug.LogError($"Couldn't find id: {networkObjectId}");
            return;
        }

        if (!spawnedObject.TryGetComponent(out Interactable interactable))
        {
            Debug.LogError("Object doesn't have interactable");
            return;
        }

        interactable.ServerInteract(_heldItem);
    }
}
