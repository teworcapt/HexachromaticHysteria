
using UnityEngine;
using Unity.Netcode;

public class EnemyShooter : NetworkBehaviour
{
    [Header("Shooting")]
    [SerializeField] private float fireInterval = 1.5f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float horizontalRange = 3f;

    [SerializeField] private ProjectilePool projectilePool;

    private Transform target;
    private ulong targetClientId;
    private float fireTimer;
    private Vector3 startingPosition;
    private int moveDirection = 1;

    public void SetTargetClient(ulong clientId)
    {
        targetClientId = clientId;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        startingPosition = transform.position;
        fireTimer = fireInterval;
        FindTarget();
    }

    private void FindTarget()
    {
        if (NetworkManager.Singleton == null)
            return;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(
            targetClientId, out NetworkClient client))
        {
            if (client.PlayerObject != null)
                target = client.PlayerObject.transform;
        }
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (target == null)
            FindTarget();

        MoveSideToSide();

        if (target == null)
            return;

        fireTimer -= Time.deltaTime;

        if (fireTimer <= 0f)
        {
            Shoot();
            fireTimer = fireInterval;
        }
    }

    private void MoveSideToSide()
    {
        transform.position += Vector3.right *
            moveDirection * moveSpeed * Time.deltaTime;

        float distanceFromStart =
            transform.position.x - startingPosition.x;

        if (distanceFromStart >= horizontalRange)
            moveDirection = -1;
        else if (distanceFromStart <= -horizontalRange)
            moveDirection = 1;
    }

    public void SetProjectilePool(ProjectilePool pool)
    {
        projectilePool = pool;
    }

    private void Shoot()
    {
        if (projectilePool == null)
        {
            Debug.LogError("Projectile Pool is missing!", this);
            return;
        }

        if (target == null)
        {
            Debug.LogError("Enemy target is missing!", this);
            return;
        }

        Vector2 direction =
            ((Vector2)target.position - (Vector2)transform.position).normalized;

        Debug.Log("Enemy is firing!");

        projectilePool.GetProjectile(
            transform.position,
            direction,
            targetClientId
        );
    }
}
