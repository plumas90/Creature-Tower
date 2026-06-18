using UnityEngine;

public class BTTask_GhostKnightWait : BTTask
{
    private float startTime;

    public BTTask_GhostKnightWait(BossBase boss) : base(boss)
    {
    }

    protected override void OnEnter()
    {
        startTime = Time.time;
    }

    protected override BossBTState OnTick()
    {
        if (!IsBossValid())
        {
            return BossBTState.Success; // Fallback
        }

        GhostKnightSO so = boss.MainSO as GhostKnightSO;
        float duration = so != null ? so.globalCooldown : 2.0f;

        if (Time.time - startTime >= duration)
        {
            return BossBTState.Success;
        }

        return BossBTState.Running;
    }

    protected override void OnExit()
    {
    }
}
