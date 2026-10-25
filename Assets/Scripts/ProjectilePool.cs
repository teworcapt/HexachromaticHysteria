
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ProjectilePool : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField] private int initialSize = 50;

    private readonly Queue<EnemyProjectile> available =
        new Queue<EnemyProjectile>();

    private void Awake()
    {
        for (int i = 0; i < initialSize; i++)
            CreateProjectile();
    }

    private EnemyProjectile CreateProjectile()
    {
        EnemyProjectile projectile = Instantiate(
            projectilePrefab,
            transform.position,
            Quaternion.identity,
            transform
        );

        projectile.gameObject.SetActive(false);
        available.Enqueue(projectile);

        return projectile;
    }


    public void GetProjectile(
        Vector2 position,
        Vector2 direction,
        ulong targetClientId)
    {
        Debug.Log($"[ProjectilePool] GetProjectile called. IsServer: " +
            $"{NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer}");

        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[ProjectilePool] Not running on server.");
            return;
        }

        if (projectilePrefab == null)
        {
            Debug.LogError("[ProjectilePool] Projectile Prefab is not assigned!", this);
            return;
        }

        EnemyProjectile projectile = null;


        while (available.Count > 0 && projectile == null)
        {
            EnemyProjectile candidate = available.Dequeue();

            if (candidate == null)
                continue;

            NetworkObject candidateNetObj =
                candidate.GetComponent<NetworkObject>();

            if (candidateNetObj == null)
            {
                Debug.LogError(
                    "[ProjectilePool] Pooled bullet is missing NetworkObject!",
                    candidate.gameObject
                );
                continue;
            }

            if (!candidateNetObj.IsSpawned)
                projectile = candidate;
        }


        if (projectile == null)
        {
            Debug.Log("[ProjectilePool] Creating a new projectile.");
            projectile = CreateProjectile();
        }

        if (projectile == null)
        {
            Debug.LogError("[ProjectilePool] Failed to create projectile!", this);
            return;
        }

        if (projectile == null)
        {
            Debug.LogError("[ProjectilePool] Projectile is null!");
            return;
        }

        NetworkObject netObj = projectile.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogError(
                "[ProjectilePool] Bullet prefab is missing NetworkObject!",
                projectile.gameObject
            );
            return;
        }


        projectile.transform.position = position;
        projectile.gameObject.SetActive(true);

        projectile.Launch(direction, this, targetClientId);

        if (!netObj.IsSpawned)
            netObj.Spawn(true);


        Debug.Log($"[ProjectilePool] Spawned: {netObj.IsSpawned}");


        Debug.Log($"[ProjectilePool] Spawned: {projectile.NetworkObject.IsSpawned}");
    }


    public void ReturnProjectile(EnemyProjectile projectile)
    {
        if (projectile == null ||
            NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsServer)
            return;

        NetworkObject netObj = projectile.GetComponent<NetworkObject>();

        if (netObj != null && netObj.IsSpawned)
            netObj.Despawn(false);

        projectile.gameObject.SetActive(false);

        // Don't re-parent network objects after despawning.
        available.Enqueue(projectile);
    }

}
