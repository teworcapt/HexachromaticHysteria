
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(NetworkObject))]
public class EnemyProjectile : NetworkBehaviour
{
    [SerializeField] private float speed = 4f;
    [SerializeField] private float lifetime = 5f;

    private Rigidbody2D rb;
    private Vector2 direction;
    private ProjectilePool pool;
    private float remainingLifetime;

    private readonly NetworkVariable<ulong> targetClientId =
        new NetworkVariable<ulong>(
            ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }


    public void Launch(
        Vector2 newDirection,
        ProjectilePool ownerPool,
        ulong targetId)
    {
        pool = ownerPool;
        direction = newDirection.normalized;
        remainingLifetime = lifetime;

        rb.position = transform.position;
        rb.linearVelocity = Vector2.zero;

        pendingTargetClientId = targetId;
    }

    private ulong pendingTargetClientId;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            targetClientId.Value = pendingTargetClientId;
    }


    private void FixedUpdate()
    {
        if (!IsSpawned || !IsServer)
            return;

        remainingLifetime -= Time.fixedDeltaTime;

        if (remainingLifetime <= 0f)
        {
            ReturnToPool();
            return;
        }

        rb.MovePosition(
            rb.position + direction * speed * Time.fixedDeltaTime
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsSpawned || !IsServer)
            return;

        Debug.Log($"[Bullet] Collided with: {other.name}");

        NetworkObject hitNetworkObject =
            other.GetComponentInParent<NetworkObject>();

        if (hitNetworkObject != null)
        {
            Debug.Log(
                $"[Bullet] NetworkObject: {hitNetworkObject.name}, " +
                $"Owner ID: {hitNetworkObject.OwnerClientId}"
            );
        }
        else
        {
            Debug.Log("[Bullet] No NetworkObject found in collider parents.");
        }

        PlayerLives lives =
            other.GetComponentInParent<PlayerLives>();

        if (lives == null)
        {
            Debug.Log("[Bullet] No PlayerLives found in collider parents.");
            return;
        }

        Debug.Log($"[Bullet] Found PlayerLives on: {lives.gameObject.name}");

        if (hitNetworkObject == null ||
            hitNetworkObject.OwnerClientId != targetClientId.Value)
        {
            Debug.Log("[Bullet] Collision is not with the intended player.");
            return;
        }

        lives.LoseLife();
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (!IsServer)
            return;

        if (pool != null)
            pool.ReturnProjectile(this);
        else
            NetworkObject.Despawn(true);
    }
}
