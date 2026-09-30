using Unity.Netcode;
using UnityEngine;

/*
 * SessionManager owns the small session surface: start as host, join as client,
 * and disconnect. Session start/stop is local, so this is a plain MonoBehaviour.
 */

public class SessionManager : MonoBehaviour
{
    public void Disconnect() => NetworkManager.Singleton.Shutdown();

    public void StartClient() => NetworkManager.Singleton.StartClient();

    public void StartHost() => NetworkManager.Singleton.StartHost();
}
