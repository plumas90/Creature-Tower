using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 탄도 이동 컴포넌트: Cast + MovePosition + 반사 로직을 재사용 가능한 컴포넌트로 분리.
/// ThreeMonkeyBoss, MonkeyPart 등에서 공통으로 사용한다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BallisticMovementComponent : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Cast 시 사용할 skin 두께")]
    [SerializeField] [Min(0f)] private float sweepSkin = 0.02f;
    
    [Tooltip("한 틱당 최대 반사 횟수")]
    [SerializeField] [Range(0, 4)] private int maxRaycastBouncesPerTick = 2;
    
    [Tooltip("벽/장애물 충돌 시 정지 시간")]
    [SerializeField] [Min(0f)] private float collisionStopDuration = 0.04f;

    [Header("Layer Settings")]
    [Tooltip("반사 가능한 레이어 (비어있으면 자동 설정)")]
    [SerializeField] private LayerMask reflectLayerMask;

    [Header("Debug")]
    [SerializeField] private bool enableMoveDebug = false;
    [SerializeField] [Min(0.02f)] private float moveDebugInterval = 0.1f;

    // 내부 상태
    private Rigidbody2D moveBody2D;
    private Collider2D moveCollider2D;
    private readonly List<RaycastHit2D> sweepHits = new List<RaycastHit2D>(8);
    private ContactFilter2D moveContactFilter;
    private bool moveContactFilterReady;
    private float collisionStopUntilTime;
    private float nextMoveDebugTime;
    private int softBodyLayerMask;
    private bool softBodyLayerMaskCached;

    /// <summary>
    /// 현재 이동 방향 (외부에서 읽고 쓸 수 있음)
    /// </summary>
    public Vector2 CurrentDirection { get; set; }

    /// <summary>
    /// 이동 속도 배율 (외부에서 조정 가능)
    /// </summary>
    public float SpeedMultiplier { get; set; } = 1f;

    private void Awake()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        moveBody2D = GetComponent<Rigidbody2D>();
        moveCollider2D = GetComponent<Collider2D>();

        if (moveBody2D != null)
        {
            moveContactFilter.useLayerMask = true;
            moveContactFilter.layerMask = reflectLayerMask.value != 0 ? reflectLayerMask : GetDefaultReflectLayerMask();
            moveContactFilter.useTriggers = false;
            moveContactFilter.useDepth = false;
            moveContactFilterReady = true;
        }
    }

    /// <summary>
    /// 탄도 이동을 수행한다. FixedUpdate에서 호출하는 것을 권장.
    /// </summary>
    /// <param name="baseSpeed">기본 이동 속도</param>
    public void MoveBallistic(float baseSpeed)
    {
        if (moveBody2D == null)
        {
            LogMoveDebug("Rigidbody2D missing; MoveBallistic skipped", true);
            return;
        }

        // Stop-window 체크
        if (Time.time < collisionStopUntilTime)
        {
            LogMoveDebug($"stop-window active until={collisionStopUntilTime:F3}");
            return;
        }

        // 방향 검증
        if (CurrentDirection.sqrMagnitude < 0.0001f)
        {
            LogMoveDebug("Direction is zero; MoveBallistic skipped");
            return;
        }

        float remainingDistance = baseSpeed * SpeedMultiplier * Time.fixedDeltaTime;
        if (remainingDistance <= 0f)
            return;

        Vector2 moveDir = CurrentDirection.normalized;
        Vector2 startPos = moveBody2D.position;
        Vector2 simPos = startPos;
        bool collided = false;
        string hitName = "none";
        Vector2 hitNormal = Vector2.zero;
        int bounceCount = 0;
        int maxBounces = Mathf.Max(0, maxRaycastBouncesPerTick);

        // Cast + 반사 루프
        while (remainingDistance > 0f)
        {
            float castDistance = remainingDistance + sweepSkin;
            sweepHits.Clear();
            
            int hitCount = moveBody2D.Cast(simPos, moveBody2D.rotation, moveDir, moveContactFilter, sweepHits, castDistance);
            bool hasHit = TryGetNearestCastHit(hitCount, out RaycastHit2D nearestHit);

            // 충돌 없음 - 직진
            if (!hasHit)
            {
                simPos += moveDir * remainingDistance;
                remainingDistance = 0f;
                break;
            }

            // 안전 거리만큼 이동
            float safeDistance = Mathf.Max(0f, nearestHit.distance - sweepSkin);
            if (safeDistance > 0f)
                simPos += moveDir * safeDistance;

            collided = true;
            hitName = nearestHit.collider != null ? nearestHit.collider.name : "null";
            hitNormal = nearestHit.normal;

            remainingDistance -= safeDistance;
            if (remainingDistance <= 0f || bounceCount >= maxBounces)
                break;

            // 반사
            moveDir = Vector2.Reflect(moveDir, nearestHit.normal).normalized;

            // Soft-body 충돌 처리
            bool softBodyHit = IsSoftBodyHitLayer(nearestHit.collider != null ? nearestHit.collider.gameObject.layer : -1);
            if (softBodyHit)
            {
                // 미세 분리 + 잔여 이동 유지
                float separation = Mathf.Max(0.005f, sweepSkin * 0.5f);
                simPos += nearestHit.normal * separation;
                remainingDistance = Mathf.Max(0f, remainingDistance - separation);
            }
            else
            {
                // 벽/장애물 충돌 - stop-window 적용 + 잔여 이동 중단
                collisionStopUntilTime = Time.time + collisionStopDuration;
                remainingDistance = 0f;
            }

            bounceCount++;
        }

        // 최종 위치 적용 및 방향 업데이트
        CurrentDirection = moveDir;
        moveBody2D.MovePosition(simPos);

        LogMoveDebug($"from={startPos} to={simPos} dir={CurrentDirection} collided={collided} hit={hitName} normal={hitNormal} bounces={bounceCount}");
    }

    private bool TryGetNearestCastHit(int hitCount, out RaycastHit2D nearestHit)
    {
        nearestHit = default;
        if (!moveContactFilterReady || hitCount <= 0)
            return false;

        float nearestDistance = float.MaxValue;
        bool hasHit = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = sweepHits[i];
            if (hit.collider == null || hit.collider == moveCollider2D)
                continue;

            if (hit.distance >= 0f && hit.distance < nearestDistance)
            {
                nearestHit = hit;
                nearestDistance = hit.distance;
                hasHit = true;
            }
        }

        return hasHit;
    }

    private bool IsSoftBodyHitLayer(int layer)
    {
        if (layer < 0)
            return false;

        if (!softBodyLayerMaskCached)
        {
            int bossLayer = LayerMask.NameToLayer("Boss");
            int creatureLayer = LayerMask.NameToLayer("Creatuer"); // 오타 그대로 유지
            int creature2Layer = LayerMask.NameToLayer("Creature");

            softBodyLayerMask = 0;
            if (bossLayer >= 0) softBodyLayerMask |= (1 << bossLayer);
            if (creatureLayer >= 0) softBodyLayerMask |= (1 << creatureLayer);
            if (creature2Layer >= 0) softBodyLayerMask |= (1 << creature2Layer);

            softBodyLayerMaskCached = true;
        }

        return (softBodyLayerMask & (1 << layer)) != 0;
    }

    /// <summary>
    /// 반사 레이어 마스크를 가져옴 (Inspector 설정 또는 기본값)
    /// </summary>
    public int GetReflectLayerMask()
    {
        if (reflectLayerMask.value != 0)
            return reflectLayerMask.value;
        return GetDefaultReflectLayerMask();
    }

    private int GetDefaultReflectLayerMask()
    {
        int wallLayer = LayerMask.NameToLayer("Wall");
        int groundLayer = LayerMask.NameToLayer("Ground");
        int playerLayer = LayerMask.NameToLayer("Player");
        int bossLayer = LayerMask.NameToLayer("Boss");
        int creatureLayer = LayerMask.NameToLayer("Creatuer");
        int creature2Layer = LayerMask.NameToLayer("Creature");
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        int mask = 0;
        if (wallLayer >= 0) mask |= (1 << wallLayer);
        if (groundLayer >= 0) mask |= (1 << groundLayer);
        if (playerLayer >= 0) mask |= (1 << playerLayer);
        if (bossLayer >= 0) mask |= (1 << bossLayer);
        if (creatureLayer >= 0) mask |= (1 << creatureLayer);
        if (creature2Layer >= 0) mask |= (1 << creature2Layer);
        if (enemyLayer >= 0) mask |= (1 << enemyLayer);

        return mask;
    }

    /// <summary>
    /// OnCollisionEnter2D/Stay2D fallback용 반사 처리
    /// </summary>
    public void HandleCollisionReflection(Collision2D collision, bool applyStopWindow)
    {
        if (collision == null || collision.contactCount <= 0)
            return;

        // 반사 대상 레이어 체크
        int layer = collision.gameObject.layer;
        int mask = GetReflectLayerMask();
        if ((mask & (1 << layer)) == 0)
            return;

        Vector2 dir = CurrentDirection.sqrMagnitude > 0.0001f ? CurrentDirection.normalized : Vector2.right;
        Vector2 normal = collision.contacts[0].normal;

        // 이미 표면에서 멀어지는 방향이면 반사하지 않는다
        if (Vector2.Dot(dir, normal) >= 0f)
            return;

        CurrentDirection = Vector2.Reflect(dir, normal).normalized;
        
        if (applyStopWindow)
            SetStopWindow(collisionStopDuration);

        LogMoveDebug($"Fallback collision reflect: layer={layer} normal={normal} newDir={CurrentDirection}", true);
    }

    private void LogMoveDebug(string message, bool force = false)
    {
        if (!enableMoveDebug)
            return;

        if (!force && Time.time < nextMoveDebugTime)
            return;

        nextMoveDebugTime = Time.time + moveDebugInterval;
        Debug.Log($"[BallisticMovement][{gameObject.name}] {message}");
    }

    /// <summary>
    /// 외부에서 stop-window를 수동으로 설정할 수 있음
    /// </summary>
    public void SetStopWindow(float duration)
    {
        collisionStopUntilTime = Time.time + Mathf.Max(0f, duration);
    }

    /// <summary>
    /// 현재 stop-window가 활성 상태인지 확인
    /// </summary>
    public bool IsInStopWindow()
    {
        return Time.time < collisionStopUntilTime;
    }
}
