using UnityEngine;

/// <summary>
/// 지정된 시간 동안 대기하는 BT Task.
/// </summary>
public class BTTask_Wait : BTTask
{
    private readonly float waitDuration;
    private readonly string blackboardDurationKey;
    private readonly bool useDynamicDuration;
    private float startTime;
    private float actualDuration;

    /// <summary>
    /// 고정된 시간 동안 대기.
    /// </summary>
    public BTTask_Wait(BossBase boss, float duration) : base(boss)
    {
        this.waitDuration = Mathf.Max(0f, duration);
        this.blackboardDurationKey = null;
        this.useDynamicDuration = false;
    }

    /// <summary>
    /// Blackboard에서 대기 시간을 읽어와서 대기.
    /// </summary>
    public BTTask_Wait(BossBase boss, string durationKey) : base(boss)
    {
        this.waitDuration = 1f;
        this.blackboardDurationKey = durationKey;
        this.useDynamicDuration = true;
    }

    protected override void OnEnter()
    {
        startTime = Time.time;

        // 대기 시간 결정
        if (useDynamicDuration && !string.IsNullOrEmpty(blackboardDurationKey))
        {
            actualDuration = GetBlackboardValue(blackboardDurationKey, waitDuration);
        }
        else
        {
            actualDuration = waitDuration;
        }

        actualDuration = Mathf.Max(0f, actualDuration);
        
        Debug.Log($"[BTTask_Wait] OnEnter - Started at {Time.time:F2}. Duration: {actualDuration}s");
    }

    protected override BossBTState OnTick()
    {
        if (!IsBossValid())
        {
            Debug.LogWarning($"[BTTask_Wait] Failure: IsBossValid is false. live={boss?.live}, wait={boss?.wait}");
            return BossBTState.Success; // Failure 방지 폴백
        }

        // 대기 시간 체크
        float elapsed = Time.time - startTime;
        if (elapsed >= actualDuration)
         {
            Debug.Log($"[BTTask_Wait] OnTick - SUCCESS at {Time.time:F2}! Elapsed: {elapsed:F2}s");
            return BossBTState.Success;
        }

        return BossBTState.Running;
    }

    protected override void OnExit()
    {
        // 정리 작업
    }
}
