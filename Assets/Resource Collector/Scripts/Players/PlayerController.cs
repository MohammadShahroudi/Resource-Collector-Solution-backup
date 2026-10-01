using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UIElements;

/*
 * PlayerController is the owner's local input loop: movement, target
 * selection, and the client-to-server interaction request. The server
 * still owns every world mutation.
 */

public class PlayerController : NetworkBehaviour
{
    static readonly int Speed = Animator.StringToHash("Speed");
    static readonly int ThrowHash = Animator.StringToHash("Throw");
    
    [Header("References")]
    public ThrownAxe axe;
    
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

    [Header("Axe")]
    public float throwImpulse = 25f;
    public float returnDuration = 1f;
    public float bowAmount = 0.5f;

    enum AxeState { Held, Throwing, Away, Returning }
    
    Interactable _closestTarget;
    Vector2 _smoothedInput;
    
    AxeState _axeState = AxeState.Held;
    LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }
    
    void Update()
    {
        if (!IsOwner) return;
        
        UpdateAxeInput();
        UpdateAimVisual();

        Vector2 movementInput = ReadMovementInput();
        _smoothedInput = Vector2.MoveTowards(_smoothedInput, movementInput, Time.deltaTime * 10f);

        transform.Rotate(Vector3.up, _smoothedInput.x * _rotationSpeed * Time.deltaTime);

        Vector3 motion = _characterController.transform.forward * _smoothedInput.y * _movementSpeed * Time.deltaTime;
        _characterController.Move(motion);

        _animator.SetFloat("Speed", _characterController.velocity.magnitude);

        UpdateInteractionTarget();

        if (Keyboard.current.eKey.wasPressedThisFrame /*|| Mouse.current.leftButton.wasPressedThisFrame*/)
            HandleInteractionPressed();
    }
    
    void UpdateAxeInput()
    {
        if (_axeState == AxeState.Held && Mouse.current.leftButton.wasPressedThisFrame)
        {
            _axeState = AxeState.Throwing;
            _animator.SetTrigger(ThrowHash);
        }
        
        if (_axeState == AxeState.Away && Mouse.current.rightButton.wasPressedThisFrame)
            StartCoroutine(ReturnAxe());
        
        // Debug.Log(_axeState);
    }
    
    public void LaunchAxe()
    {
        if (_axeState != AxeState.Throwing) return;

        Vector3 direction = transform.forward;
        direction.y = 0f;
        direction.Normalize();

        // NetworkObject axePrefab = NetworkObject.InstantiateAndSpawn(axe.gameObject, NetworkManager, position: transform.position);
        
        axe.Launch(direction, throwImpulse, _characterController);
        Debug.Log("Launching axe!");
        _axeState = AxeState.Away;
    }
    
    IEnumerator ReturnAxe()
    {
        _axeState = AxeState.Returning;
        axe.rigidbody.isKinematic = true;
        axe.axeCollider.enabled = false;
        
        Vector3 start = axe.transform.position;
        
        float elapsedTime = 0f;
        while (elapsedTime < returnDuration)
        {
            float t = elapsedTime / returnDuration;
            
            Vector3 p0 = start;
            Vector3 p2 = axe.CatchPosition;
            Vector3 p1 = (p0 + p2) * 0.5f + transform.right * bowAmount;
                
            axe.transform.position = QuadraticBezierMath.SamplePointBernstein(p0, p1, p2, t);
            axe.transform.Rotate(Vector3.forward, axe.spinSpeed * Time.deltaTime, Space.Self);
            
            yield return null;
            elapsedTime += Time.deltaTime;
        }

        axe.AttachToHand();
        _axeState = AxeState.Held;
        Debug.Log("Axe returned!");
    }
    
    void UpdateAimVisual()
    {
        switch (_axeState)
        {
            case AxeState.Held:
                DrawAimLine();
                break;
            case AxeState.Away:
                DrawReturnPath();
                break;
            default:
                _lineRenderer.positionCount = 0;
                break;
        }
    }
    
    void DrawAimLine()
    {
        if (Physics.Raycast(axe.transform.position, transform.forward, out RaycastHit hit))
        {
            _lineRenderer.positionCount = 2;
            _lineRenderer.SetPosition(0, axe.transform.position);
            _lineRenderer.SetPosition(1, hit.point);
        }
        else
        {
            _lineRenderer.positionCount = 0;
        }
    }

    void DrawReturnPath()
    {
        // TODO Slice 5.2: draw the curve from GetReturnControlPoints with ten samples,
        // evenly spaced in t, from the axe to the hand. Recall (5.4) reuses the same curve.
        // Check: throw. While Away, the preview bows from the axe to the hand.
        // Changing bowAmount changes the bow.
        // Next: Slice 5.3 in ReturnAxe.
        _lineRenderer.positionCount = 0;
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
