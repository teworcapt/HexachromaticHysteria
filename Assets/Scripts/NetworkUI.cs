
using UnityEngine;
using Unity.Netcode;

public class NetworkUI : MonoBehaviour
{
    private void OnGUI()
    {
        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.IsListening)
        {
            if (GUI.Button(new Rect(20, 20, 150, 40), "Start Host"))
            {
                NetworkManager.Singleton.StartHost();
            }

            if (GUI.Button(new Rect(20, 70, 150, 40), "Join as Client"))
            {
                NetworkManager.Singleton.StartClient();
            }
        }
        else
        {
            string status = NetworkManager.Singleton.IsHost
                ? "Running as Host"
                : "Connected as Client";

            GUI.Label(new Rect(20, 20, 250, 40), status);
        }
    }
}
