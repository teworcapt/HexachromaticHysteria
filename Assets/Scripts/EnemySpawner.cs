
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NetworkObject enemyPrefab;
    [SerializeField] private Transform player1SpawnPoint;
    [SerializeField] private Transform player2SpawnPoint;
    [SerializeField] private ProjectilePool projectilePool;
    [SerializeField] private NetworkObject matchTimerPrefab;

    private NetworkManager networkManager;
    private bool matchStarted;

    private readonly HashSet<ulong> spawnedPlayers = new();

    public bool MatchStarted => matchStarted;
    private readonly List<NetworkObject> spawnedEnemies = new();

    private void Start()
    {
        Debug.Log("[EnemySpawner] Start() called.");

        networkManager = NetworkManager.Singleton;

        if (networkManager == null)
        {
            Debug.LogError("[EnemySpawner] NetworkManager not found!");
            return;
        }

        networkManager.OnServerStarted += HandleServerStarted;
        networkManager.OnClientConnectedCallback += HandleClientConnected;

        Debug.Log(
            $"[EnemySpawner] Initialized. IsServer: {networkManager.IsServer}, " +
            $"IsClient: {networkManager.IsClient}"
        );

        if (networkManager.IsServer)
            TryStartMatch();
    }

    private void OnDestroy()
    {
        if (networkManager == null)
            return;

        networkManager.OnServerStarted -= HandleServerStarted;
        networkManager.OnClientConnectedCallback -= HandleClientConnected;
    }

    private void HandleServerStarted()
    {
        Debug.Log("[EnemySpawner] Server started.");
        TryStartMatch();
    }

    private void HandleClientConnected(ulong clientId)
    {
        Debug.Log($"[EnemySpawner] Client connected: {clientId}");

        if (networkManager == null || !networkManager.IsServer)
            return;

        TryStartMatch();
    }

    private void TryStartMatch()
    {
        if (networkManager == null)
        {
            Debug.LogError("[EnemySpawner] NetworkManager is null.");
            return;
        }

        Debug.Log(
            $"[EnemySpawner] Checking players: " +
            $"{networkManager.ConnectedClientsIds.Count}"
        );

        if (matchStarted)
            return;

        if (!networkManager.IsServer ||
            networkManager.ConnectedClientsIds.Count < 2)
        {
            Debug.Log("[EnemySpawner] Waiting for second player.");
            return;
        }

        if (enemyPrefab == null)
        {
            Debug.LogError("[EnemySpawner] Enemy prefab is not assigned!");
            return;
        }

        if (matchTimerPrefab == null)
        {
            Debug.LogError("[EnemySpawner] Match timer prefab is not assigned!");
            return;
        }

        Debug.Log("[EnemySpawner] Both players connected! Starting match.");

        // Spawn the networked timer.
        NetworkObject timer = Instantiate(
            matchTimerPrefab,
            Vector3.zero,
            Quaternion.identity
        );

        timer.Spawn();

        // Mark the match as started.
        matchStarted = true;

        // Spawn one enemy for each connected player.
        foreach (ulong clientId in networkManager.ConnectedClientsIds)
            SpawnEnemyForPlayer(clientId);
    }

    private void SpawnEnemyForPlayer(ulong clientId)
    {
        if (spawnedPlayers.Contains(clientId))
            return;

        bool isPlayerOne =
            clientId == NetworkManager.ServerClientId;

        Transform spawnPoint = isPlayerOne
            ? player1SpawnPoint
            : player2SpawnPoint;

        if (spawnPoint == null)
        {
            Debug.LogError(
                $"[EnemySpawner] Missing spawn point for client {clientId}."
            );
            return;
        }

        NetworkObject enemy = Instantiate(
            enemyPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        EnemyShooter shooter = enemy.GetComponent<EnemyShooter>();

        if (shooter != null)
        {
            shooter.SetTargetClient(clientId);
            shooter.SetProjectilePool(projectilePool);
        }
        else
        {
            Debug.LogError("[EnemySpawner] EnemyShooter component missing!");
        }

        enemy.Spawn(true);
        spawnedEnemies.Add(enemy);
        spawnedPlayers.Add(clientId);

        Debug.Log($"[EnemySpawner] Spawned enemy for client {clientId}.");
    }

    public void StopSpawning()
    {
        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsServer)
            return;

        matchStarted = false;
    }

    public void RestartMatch()
    {
        if (networkManager == null || !networkManager.IsServer)
            return;

        // Remove old enemies.
        foreach (NetworkObject enemy in spawnedEnemies)
        {
            if (enemy != null && enemy.IsSpawned)
                enemy.Despawn(true);
        }

        spawnedEnemies.Clear();
        spawnedPlayers.Clear();

        // Remove any active enemy projectiles.
        EnemyProjectile[] projectiles =
            FindObjectsByType<EnemyProjectile>(
                FindObjectsSortMode.None
            );

        foreach (EnemyProjectile projectile in projectiles)
        {
            NetworkObject netObj =
                projectile.GetComponent<NetworkObject>();

            if (netObj != null && netObj.IsSpawned)
                netObj.Despawn(true);
        }

        matchStarted = false;

        foreach (ulong clientId in networkManager.ConnectedClientsIds)
        {
            SpawnEnemyForPlayer(clientId);
        }

        matchStarted = spawnedPlayers.Count >= 2;

        Debug.Log(
            matchStarted
                ? "[EnemySpawner] New round started."
                : "[EnemySpawner] Could not spawn all enemies."
        );
    }

}
