using UnityEngine;

/// <summary>
/// TheWorm 발사 태스크: 저장된 방향으로 AddForce 발사 + 속도/시간 모니터링
/// </summary>
public class BTTask_WormLaunch : BTTask
{
    private WormLaunchComponent launchComponent;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool launched;
    private float launchTime;
    private float maxDuration;

    public BTTask_WormLaunch(BossBase boss, float maxLaunchDuration = 2.0f) : base(boss)
    {
        maxDuration = maxLaunchDuration;
    }

    protected override void OnEnter()
    {
        launchComponent = boss.GetComponent<WormLaunchComponent>();
        if (launchComponent == null)
        {
            Debug.LogError("[BTTask_WormLaunch] WormLaunchComponent not found!");
            return;
        }

        spriteRenderer = boss.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = Color.white;
            // 색상은 이미 하얀색이어야 함 (차지에서 빨강 → 하양으로 복원됨)
        }

        // Animator 트리거 (있으면)
        Animator animator = boss.GetComponent<Animator>();
        if (animator != null)
            animator.SetTrigger("Launch");

        // Blackboard에서 방향 가져오기
        Vector2 direction = GetBlackboardValue<Vector2>("WormLaunchDirection");
        
        if (direction.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning("[BTTask_WormLaunch] No launch direction found! Using right as default.");
            direction = Vector2.right;
        }

        // 발사!
        launchComponent.Launch(direction);
        launched = true;
        launchTime = Time.time;

        Debug.Log($"[WormLaunch] OnEnter - Launched at {Time.time:F2}! Direction: {direction}, Max duration: {maxDuration}s");
    }

    protected override BossBTState OnTick()
    {
        if (!IsBossValid() || launchComponent == null)
            return BossBTState.Failure;

        if (!launched)
            return BossBTState.Failure;

        float elapsed = Time.time - launchTime;

        // 타임아웃: 2초 이상 날아갔으면 강제 종료
        if (elapsed >= maxDuration)
        {
            Debug.Log($"[WormLaunch] OnTick - TIMEOUT at {Time.time:F2}! Elapsed: {elapsed:F2}s. Ending launch.");
            return BossBTState.Success;
        }

        // 속도 체크: 멈췄으면 성공
        if (launchComponent.IsStopped)
        {
            Debug.Log($"[WormLaunch] OnTick - STOPPED at {Time.time:F2}! Elapsed: {elapsed:F2}s. Launch complete.");
            return BossBTState.Success;
        }

        return BossBTState.Running;
    }

    protected override void OnExit()
    {
        Debug.Log($"[WormLaunch] OnExit - Exiting at {Time.time:F2}. Resetting launch.");
        
        if (launchComponent != null)
            launchComponent.ResetLaunch();

        // 색상 원래대로 (하양)
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }
}
