using UnityEngine;

public class BTTask_PeaPodGrowVineChain : BTTask
{
    private readonly PeaPodBossSO so;

    public BTTask_PeaPodGrowVineChain(BossBase boss, PeaPodBossSO soData) : base(boss)
    {
        so = soData;
    }

    protected override void OnEnter()
    {
        if (so == null || so.vineSegmentPrefab == null || boss == null || !boss.live)
            return;

        Transform playerTr = GetPlayerTransform();
        Vector2 toPlayer = playerTr != null
            ? (Vector2)(playerTr.position - boss.transform.position)
            : Vector2.right;
        Vector2 direction = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector2.right;

        int curveSign;
        if (so.randomizeCurveDirection)
            curveSign = Random.value < 0.5f ? -1 : 1;
        else
            curveSign = so.fixedCurveDirectionSign >= 0 ? 1 : -1;

        // Spawn background chain controller to handle growing segments concurrently
        GameObject controllerObj = new GameObject("PeaPodVineChainController");
        PeaPodVineChainController controller = controllerObj.AddComponent<PeaPodVineChainController>();
        controller.StartChain((PeaPodBoss)boss, so, direction, curveSign);

        Debug.Log("[BTTask_PeaPodGrowVineChain] Vine chain started in background.");
    }

    protected override BossBTState OnTick()
    {
        // Return Success immediately so that the next node (BTTask_Wait) begins waiting for attackInterval
        return BossBTState.Success;
    }
}
