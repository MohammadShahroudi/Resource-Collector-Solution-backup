using System;
using UnityEngine;

/*
 * ThrownAxe keeps the axe's held pose so it can be attached to the hand
 * after a throw. Launch physics and collision response belong here;
 * PlayerController decides when to throw and recall it.
 */

public class ThrownAxe : MonoBehaviour
{
    public Rigidbody rigidbody;
    public Collider axeCollider;
    public float spinSpeed = 1000f;

    // bool _stuck;
    Transform _hand;
    Vector3 _heldLocalPosition;
    Quaternion _heldLocalRotation;

    public Vector3 CatchPosition => _hand.TransformPoint(_heldLocalPosition);

    public void Launch(Vector3 direction, float impulse, CharacterController thrower)
    {
        _hand = transform.parent;
        _heldLocalPosition = transform.localPosition;
        _heldLocalRotation = transform.localRotation;
        
        // Physics.IgnoreCollision(axeCollider, thrower);
        transform.SetParent(null);
        transform.position += direction * 0.5f;
        
        rigidbody.isKinematic = false;
        axeCollider.enabled = true;
        
        rigidbody.AddForce(direction * impulse, ForceMode.VelocityChange);
        rigidbody.AddTorque(transform.forward * (-spinSpeed * Mathf.Deg2Rad), ForceMode.VelocityChange);
    }

    public void AttachToHand()
    {
        transform.SetParent(_hand);
        transform.SetLocalPositionAndRotation(_heldLocalPosition, _heldLocalRotation);
        rigidbody.isKinematic = true;
        axeCollider.enabled = false;
        // Debug.Log(_hand);
    }

    void OnCollisionEnter(Collision collision)
    {
        rigidbody.isKinematic = true;
    }
}
