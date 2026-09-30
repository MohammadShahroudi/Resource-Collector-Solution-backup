using TMPro;
using Unity.Netcode;
using UnityEngine;

/*
 * PlayerNameLabel sets the overhead nameplate text from this player's replicated
 * NetworkObject owner id and keeps the nameplate facing the main camera.
 */

public class PlayerNameLabel : NetworkBehaviour
{
    [SerializeField] TextMeshProUGUI _label;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _label.text = $"Player {NetworkObject.OwnerClientId}";
    }

    void LateUpdate()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 direction = transform.position - mainCamera.transform.position;
        direction.y = 0;

        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }
}
