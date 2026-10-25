
using UnityEngine;
using Unity.Netcode;

public class NetworkPlayerSpawner : MonoBehaviour
{
    [Header("Player Prefab")]
    [SerializeField] private NetworkObject playerPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform spawnPointP1;
    [SerializeField] private Transform spawnPointP2;

    [Header("Arena Bounds")]
    [SerializeField] private BoxCollider2D arenaBoundsP1;
    [SerializeField] private BoxCollider2D arenaBoundsP2;

    private NetworkManager networkManager;

    private void Start()
    {
        networkManager = NetworkManager.Singleton;

        if (networkManager != null)
            networkManager.OnServerStarted += HandleServerStarted;
    }

    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.OnServerStarted -= HandleServerStarted;
            networkManager.OnClientConnectedCallback -= HandleClientConnected;
        }
    }

    private void HandleServerStarted()
    {
        if (networkManager == null || !networkManager.IsServer)
            return;

        networkManager.OnClientConnectedCallback += HandleClientConnected;

        foreach (ulong clientId in networkManager.ConnectedClientsIds)
        {
            SpawnPlayer(clientId);
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        SpawnPlayer(clientId);
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (!networkManager.IsServer)
            return;

        if (!networkManager.ConnectedClients.TryGetValue(
                clientId, out var client))
            return;

        if (client.PlayerObject != null)
            return;

        bool isHost = clientId == NetworkManager.ServerClientId;

        Transform spawnPoint = isHost ? spawnPointP1 : spawnPointP2;
        BoxCollider2D bounds = isHost ? arenaBoundsP1 : arenaBoundsP2;

        if (playerPrefab == null || spawnPoint == null || bounds == null)
        {
            Debug.LogError(
                $"Missing prefab, spawn point, or arena bounds for client {clientId}.",
                this
            );
            return;
        }

        NetworkObject player = Instantiate(
            playerPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        PlayerMovement movement = player.GetComponent<PlayerMovement>();

        if (movement != null)
            movement.SetArenaBounds(bounds);

        player.SpawnAsPlayerObject(clientId, true);
    }
}
