using UnityEngine;

public class BTTask_GhostKnightWait : BTTask
{
    private float startTime;
    private bool armRotationStarted;

    public BTTask_GhostKnightWait(BossBase boss) : base(boss)
    {
    }

    protected override void OnEnter()
    {
        startTime = Time.time;
        armRotationStarted = false;
    }

    protected override BossBTState OnTick()
    {
        if (!IsBossValid())
        {
            return BossBTState.Success; // Fallback
        }

        GhostKnightSO so = boss.MainSO as GhostKnightSO;
        float duration = so != null ? so.globalCooldown : 2.0f;
        float elapsed = Time.time - startTime;

        if (elapsed >= duration)
        {
            return BossBTState.Success;
        }

        // Trigger arm rotation up 1.5s before wait finishes (1.0s rotation, 0.5s pause)
        float triggerTime = Mathf.Max(0f, duration - 1.5f);
        if (elapsed >= triggerTime && !armRotationStarted)
        {
            armRotationStarted = true;
            if (boss is GhostKnight gk)
            {
                float rotDuration = Mathf.Min(1.0f, duration - triggerTime);
                gk.StartRotateArmUp(rotDuration);
            }
        }

        return BossBTState.Running;
    }

    protected override void OnExit()
    {
    }
}
