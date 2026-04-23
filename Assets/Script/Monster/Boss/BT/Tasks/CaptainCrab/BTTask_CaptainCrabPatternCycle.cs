using UnityEngine;

public class BTTask_CaptainCrabPatternCycle : BTTask
{
    private readonly CaptainCrabBossSO so;
    private CaptainCrabBoss crabBoss;

    public BTTask_CaptainCrabPatternCycle(BossBase boss, CaptainCrabBossSO soData) : base(boss)
    {
        so = soData;
    }

    protected override void OnEnter()
    {
        crabBoss = boss as CaptainCrabBoss;
        if (crabBoss == null)
            Debug.LogError("[BTTask_CaptainCrabPatternCycle] Boss is not CaptainCrabBoss.");
    }

    protected override BossBTState OnTick()
    {
        if (!IsBossValid() || crabBoss == null || so == null)
            return BossBTState.Failure;

        crabBoss.TickPatternCycle();
        return BossBTState.Running;
    }
}

