using UnityEngine;

/// <summary>
/// TheWorm 차지 태스크: 2초 동안 플레이어 추적 회전 + 색상 변화 (하양→빨강)
/// </summary>
public class BTTask_WormCharge : BTTask
{
    private float chargeDuration;
    private float rotationSpeed;
    private Color targetColor;
    private PlayerTargetingComponent playerTargeting;
    private SpriteRenderer spriteRenderer;
    private float chargeStartTime;
    private Color originalColor;

    public BTTask_WormCharge(BossBase boss, float duration = 2.0f, float rotSpeed = 720f, Color? chargeColor = null) 
        : base(boss)
    {
        chargeDuration = duration;
        rotationSpeed = rotSpeed;
        targetColor = chargeColor ?? Color.red;
    }

    protected override void OnEnter()
    {
        playerTargeting = boss.GetComponent<PlayerTargetingComponent>();
        if (playerTargeting == null)
            playerTargeting = boss.gameObject.AddComponent<PlayerTargetingComponent>();

        spriteRenderer = boss.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            Debug.Log($"[WormCharge] OnEnter - SpriteRenderer found! Original: {originalColor}, Target: {targetColor}");
        }
        else
        {
            originalColor = Color.white;
            Debug.LogWarning("[WormCharge] OnEnter - SpriteRenderer NOT FOUND!");
        }

        chargeStartTime = Time.time;
        
        // Animator 트리거 (있으면)
        Animator animator = boss.GetComponent<Animator>();
        if (animator != null)
            animator.SetTrigger("Charge");

        Debug.Log($"[WormCharge] OnEnter - Started at {Time.time:F2}. Duration: {chargeDuration}s, RotSpeed: {rotationSpeed}");
    }

    protected override BossBTState OnTick()
    {
        if (!IsBossValid())
            return BossBTState.Failure;

        float elapsed = Time.time - chargeStartTime;
        float progress = Mathf.Clamp01(elapsed / chargeDuration);

        // 차지 중에는 제자리에 고정 (이동 방지)
        Rigidbody2D rb = boss.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // 이동 차단
        }

        // 색상 변화 (하양 → 빨강)
        if (spriteRenderer != null)
        {
            Color newColor = Color.Lerp(originalColor, targetColor, progress);
            spriteRenderer.color = newColor;
        }

        // 플레이어 방향으로 회전만 (이동 X)
        if (playerTargeting != null && playerTargeting.IsTargetValid)
        {
            Vector2 dirToPlayer = playerTargeting.GetDirectionToTarget();
            float targetAngle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
            float currentAngle = boss.transform.eulerAngles.z;
            
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
            boss.transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
        }

        // 차지 완료
        if (elapsed >= chargeDuration)
        {
            // 최종 방향을 Blackboard에 저장
            Vector2 finalDirection = boss.transform.right; // 오른쪽 방향 (0도 기준)
            SetBlackboardValue("WormLaunchDirection", finalDirection);
            
            Debug.Log($"[WormCharge] OnTick - SUCCESS at {Time.time:F2}! Elapsed: {elapsed:F2}s, Final direction: {finalDirection}, Final color: {spriteRenderer?.color}");
            return BossBTState.Success;
        }

        return BossBTState.Running;
    }

    protected override void OnExit()
    {
        Debug.Log($"[WormCharge] OnExit - Exiting at {Time.time:F2}. Resetting color to white.");
        
        // 차지 완료 시 색상을 완전히 하얀색으로 복원 (발사 전에 원래대로)
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }
}
