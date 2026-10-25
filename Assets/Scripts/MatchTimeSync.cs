
using UnityEngine;
using Unity.Netcode;

public class MatchTimerSync : NetworkBehaviour
{
    [SerializeField] private float matchDuration = 120f;

    public NetworkVariable<float> TimeRemaining =
        new NetworkVariable<float>(
            120f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public NetworkVariable<bool> TimerStarted =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private EnemySpawner enemySpawner;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            TimeRemaining.Value = matchDuration;

        enemySpawner = FindFirstObjectByType<EnemySpawner>();
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (!TimerStarted.Value)
        {
            if (enemySpawner == null)
                enemySpawner = FindFirstObjectByType<EnemySpawner>();

            if (enemySpawner == null || !enemySpawner.MatchStarted)
                return;

            TimerStarted.Value = true;
        }

        if (TimeRemaining.Value > 0f)
        {
            TimeRemaining.Value = Mathf.Max(
                0f,
                TimeRemaining.Value - Time.deltaTime
            );
        }
    }
}
