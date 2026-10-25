
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private bool isPlayerOne = true;

    [Header("Arena")]
    [SerializeField] private BoxCollider2D arenaBounds;
    [SerializeField] private float boundaryPadding = 0.35f;

    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private Vector2 movementInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    public override void OnNetworkSpawn()
    {
        isPlayerOne = OwnerClientId == NetworkManager.ServerClientId;

        if (IsOwner)
        {
            AssignArenaBounds();
        }
    }

    public void SetArenaBounds(BoxCollider2D bounds)
    {
        arenaBounds = bounds;
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner)
        {
            movementInput = Vector2.zero;
            return;
        }

        if (Keyboard.current != null &&
    Keyboard.current.wKey.wasPressedThisFrame)
        {
            Debug.Log($"W pressed by owner {OwnerClientId}");
        }

        movementInput = Vector2.zero;

        if (Keyboard.current == null)
        {
            Debug.LogWarning("No keyboard detected by Input System.", this);
            return;
        }

        if (isPlayerOne)
        {
            if (Keyboard.current.wKey.isPressed) movementInput.y++;
            if (Keyboard.current.sKey.isPressed) movementInput.y--;
            if (Keyboard.current.aKey.isPressed) movementInput.x--;
            if (Keyboard.current.dKey.isPressed) movementInput.x++;
        }
        else
        {
            if (Keyboard.current.upArrowKey.isPressed) movementInput.y++;
            if (Keyboard.current.downArrowKey.isPressed) movementInput.y--;
            if (Keyboard.current.leftArrowKey.isPressed) movementInput.x--;
            if (Keyboard.current.rightArrowKey.isPressed) movementInput.x++;
        }

        movementInput = Vector2.ClampMagnitude(movementInput, 1f);
    }

    private void FixedUpdate()
    {
        if (!IsSpawned || !IsOwner)
            return;

        Vector2 nextPosition =
            rb.position + movementInput * moveSpeed * Time.fixedDeltaTime;

        if (arenaBounds != null && playerCollider != null)
        {
            Bounds bounds = arenaBounds.bounds;

            float halfWidth = playerCollider.bounds.extents.x;
            float halfHeight = playerCollider.bounds.extents.y;

            nextPosition.x = Mathf.Clamp(
                nextPosition.x,
                bounds.min.x + halfWidth + boundaryPadding,
                bounds.max.x - halfWidth - boundaryPadding
            );

            nextPosition.y = Mathf.Clamp(
                nextPosition.y,
                bounds.min.y + halfHeight + boundaryPadding,
                bounds.max.y - halfHeight - boundaryPadding
            );
        }

        // Apply the movement after calculating the boundaries.
        rb.MovePosition(nextPosition);
    }

    private void AssignArenaBounds()
    {
        string arenaName = isPlayerOne ? "Arena_P1" : "Arena_P2";
        GameObject arena = GameObject.Find(arenaName);

        if (arena != null)
        {
            arenaBounds = arena.GetComponent<BoxCollider2D>();
        }
        else
        {
            Debug.LogError($"Could not find {arenaName} in the scene.", this);
        }
    }

}
