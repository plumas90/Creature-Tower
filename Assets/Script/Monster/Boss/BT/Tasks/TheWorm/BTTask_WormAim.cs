using UnityEngine;

/// <summary>
/// TheWorm 조준 대기 태스크: 0.2초 동안 방향 고정 (플레이어 회피 타이밍)
/// </summary>
public class BTTask_WormAim : BTTask
{
    private float aimDuration;
    private float aimStartTime;

    public BTTask_WormAim(BossBase boss, float duration = 0.2f) : base(boss)
    {
        aimDuration = duration;
    }

    protected override void OnEnter()
    {
        aimStartTime = Time.time;

        // Animator 트리거 (있으면)
        Animator animator = boss.GetComponent<Animator>();
        if (animator != null)
            animator.SetTrigger("Aim");

        // 방향은 이미 Charge에서 Blackboard에 저장됨
        Vector2 direction = GetBlackboardValue<Vector2>("WormLaunchDirection");
        
        Debug.Log($"[WormAim] OnEnter - Started at {Time.time:F2}. Duration: {aimDuration}s, Locked direction: {direction}");
    }

    protected override BossBTState OnTick()
    {
        if (!IsBossValid())
            return BossBTState.Failure;

        float elapsed = Time.time - aimStartTime;

        // 조준 시간 종료
        if (elapsed >= aimDuration)
        {
            Debug.Log($"[WormAim] OnTick - SUCCESS at {Time.time:F2}! Elapsed: {elapsed:F2}s. Ready to launch!");
            return BossBTState.Success;
        }

        return BossBTState.Running;
    }

    protected override void OnExit()
    {
        Debug.Log($"[WormAim] OnExit - Exiting at {Time.time:F2}");
        // 방향은 그대로 유지 (Launch에서 사용)
    }
}
