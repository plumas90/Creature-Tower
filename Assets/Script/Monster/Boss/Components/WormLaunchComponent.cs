using UnityEngine;

/// <summary>
/// TheWorm 전용 발사 물리 컴포넌트
/// Rigidbody2D.AddForce를 사용하여 한 번의 폭발적인 힘으로 발사 후 자연 감속
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class WormLaunchComponent : MonoBehaviour
{
    [Header("Launch Settings")]
    [SerializeField] private float launchForce = 50f;
    [SerializeField] private float stopVelocityThreshold = 0.5f;

    [Header("Knockback Settings")]
    [SerializeField] private float playerKnockbackForce = 10f;
    [SerializeField] private float playerKnockbackDuration = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = false;

    private Rigidbody2D rb;
    private bool isLaunched = false;
    private Vector2 launchDirection;

    public bool IsLaunched => isLaunched;
    public bool IsStopped => rb != null && rb.linearVelocity.magnitude < stopVelocityThreshold;
    public Vector2 CurrentVelocity => rb != null ? rb.linearVelocity : Vector2.zero;

    /// <summary>
    /// SO에서 설정값 적용
    /// </summary>
    public void SetLaunchSettings(float force, float stopThreshold, float knockbackForce)
    {
        launchForce = force;
        stopVelocityThreshold = stopThreshold;
        playerKnockbackForce = knockbackForce;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// 지정된 방향으로 발사
    /// </summary>
    public void Launch(Vector2 direction)
    {
        if (rb == null)
        {
            Debug.LogError("[WormLaunchComponent] Rigidbody2D not found!");
            return;
        }

        // Rigidbody가 Kinematic이면 경고
        if (rb.bodyType == RigidbodyType2D.Kinematic)
        {
            Debug.LogError("[WormLaunchComponent] Rigidbody2D is Kinematic! AddForce won't work. Change to Dynamic in Inspector.");
            return;
        }

        launchDirection = direction.normalized;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(launchDirection * launchForce, ForceMode2D.Impulse);
        isLaunched = true;

        if (enableDebugLog)
            Debug.Log($"[WormLaunch] Launched in direction {launchDirection} with force {launchForce}");
    }

    /// <summary>
    /// 발사 상태 해제 (Idle로 복귀 시 호출)
    /// </summary>
    public void ResetLaunch()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        
        isLaunched = false;
        
        if (enableDebugLog)
            Debug.Log("[WormLaunch] Reset to idle state");
    }

    /// <summary>
    /// 플레이어와 충돌 시 넉백 방향 계산
    /// 날아가는 방향의 좌우 중 플레이어가 가까운 쪽으로 넉백
    /// </summary>
    public Vector2 CalculateKnockbackDirection(Vector2 playerPosition)
    {
        if (!isLaunched || launchDirection.sqrMagnitude < 0.0001f)
            return Vector2.zero;

        Vector2 myPos = transform.position;
        Vector2 toPlayer = (playerPosition - myPos).normalized;

        // 날아가는 방향의 좌우 수직 벡터 (시계방향 90도 회전)
        Vector2 rightSide = new Vector2(launchDirection.y, -launchDirection.x);
        Vector2 leftSide = -rightSide;

        // 플레이어가 어느 쪽에 있는지 내적으로 판단
        float dotRight = Vector2.Dot(toPlayer, rightSide);
        float dotLeft = Vector2.Dot(toPlayer, leftSide);

        // 더 가까운 쪽으로 넉백
        Vector2 knockbackDir = dotRight > dotLeft ? rightSide : leftSide;

        if (enableDebugLog)
            Debug.Log($"[WormLaunch] Knockback: launchDir={launchDirection}, playerSide={knockbackDir}");

        return knockbackDir;
    }

    /// <summary>
    /// 플레이어에게 넉백 적용
    /// </summary>
    public void ApplyKnockbackToPlayer(PlayerStatControl player)
    {
        if (player == null)
            return;

        Vector2 knockbackDir = CalculateKnockbackDirection(player.transform.position);
        
        // PlayerStatControl의 넉백 메서드가 있다고 가정
        // 없으면 직접 Rigidbody2D 접근
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.AddForce(knockbackDir * playerKnockbackForce, ForceMode2D.Impulse);
            
            if (enableDebugLog)
                Debug.Log($"[WormLaunch] Applied knockback to player: {knockbackDir * playerKnockbackForce}");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isLaunched)
            return;

        // 플레이어 충돌 처리
        PlayerStatControl player = collision.gameObject.GetComponent<PlayerStatControl>();
        if (player != null)
        {
            ApplyKnockbackToPlayer(player);
        }
    }
}
