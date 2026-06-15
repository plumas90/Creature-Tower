using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ?�도 ?�동 컴포?�트: Cast + MovePosition + 반사 로직???�사??가?�한 컴포?�트�?분리.
/// ThreeMonkeyBoss, MonkeyPart ?�에??공통?�로 ?�용?�다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BallisticMovementComponent : MonoBehaviour
{
    [Header("Contact Damage Settings")]
    [Tooltip("Cast 충돌 ??PlayerStatControl???�촉 ?��?지�??�도?��? ?��?")]
    [SerializeField] private bool applyCastContactDamage = true;

    [Header("Movement Settings")]
    [Tooltip("Cast ???�용??skin ?�께")]
    [SerializeField] [Min(0f)] private float sweepSkin = 0.02f;
    
    [Tooltip("???�당 최�? 반사 ?�수")]
    [SerializeField] [Range(0, 4)] private int maxRaycastBouncesPerTick = 2;
    
    [Tooltip("�??�애�?충돌 ???��? ?�간")]
    [SerializeField] [Min(0f)] private float collisionStopDuration = 0.04f;

    [Header("Layer Settings")]
    [Tooltip("반사 가?�한 ?�이??(비어?�으�??�동 ?�정)")]
    [SerializeField] private LayerMask reflectLayerMask;

    [Header("Debug")]
    [SerializeField] private bool enableMoveDebug = false;
    [SerializeField] [Min(0.02f)] private float moveDebugInterval = 0.1f;

    // ?��? ?�태
    private Rigidbody2D moveBody2D;
    private Collider2D moveCollider2D;
    private BossBase ownerBoss;
    private readonly List<RaycastHit2D> sweepHits = new List<RaycastHit2D>(8);
    private ContactFilter2D moveContactFilter;
    private bool moveContactFilterReady;
    private float collisionStopUntilTime;
    private float nextMoveDebugTime;
    private int softBodyLayerMask;
    private bool softBodyLayerMaskCached;

    /// <summary>
    /// ?�재 ?�동 방향 (?��??�서 ?�고 ?????�음)
    /// </summary>
    public Vector2 CurrentDirection { get; set; }

    /// <summary>
    /// ?�동 ?�도 배율 (?��??�서 조정 가??
    /// </summary>
    public float SpeedMultiplier { get; set; } = 1f;
    public Action<PlayerStatControl> OnCastPlayerHit { get; set; }

    private void Awake()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        moveBody2D = GetComponent<Rigidbody2D>();
        moveCollider2D = GetComponent<Collider2D>();
        ownerBoss = GetComponent<BossBase>();

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
    /// ?�도 ?�동???�행?�다. FixedUpdate?�서 ?�출?�는 것을 권장.
    /// </summary>
    /// <param name="baseSpeed">기본 ?�동 ?�도</param>
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

        // 방향 검�?
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

            // 충돌 ?�음 - 직진
            if (!hasHit)
            {
                simPos += moveDir * remainingDistance;
                remainingDistance = 0f;
                break;
            }

            // ?�전 거리만큼 ?�동
            float safeDistance = Mathf.Max(0f, nearestHit.distance - sweepSkin);
            if (safeDistance > 0f)
                simPos += moveDir * safeDistance;

            collided = true;
            hitName = nearestHit.collider != null ? nearestHit.collider.name : "null";
            hitNormal = nearestHit.normal;

            if (nearestHit.collider != null)
                TryApplyCastContactDamage(nearestHit.collider);

            remainingDistance -= safeDistance;
            if (remainingDistance <= 0f || bounceCount >= maxBounces)
                break;

            // 반사
            moveDir = Vector2.Reflect(moveDir, nearestHit.normal).normalized;

            // Soft-body 충돌 처리
            bool softBodyHit = IsSoftBodyHitLayer(nearestHit.collider != null ? nearestHit.collider.gameObject.layer : -1);
            if (softBodyHit)
            {
                // 미세 분리 + ?�여 ?�동 ?��?
                float separation = Mathf.Max(0.005f, sweepSkin * 0.5f);
                simPos += nearestHit.normal * separation;
                remainingDistance = Mathf.Max(0f, remainingDistance - separation);
            }
            else
            {
                // �??�애�?충돌 - stop-window ?�용 + ?�여 ?�동 중단
                collisionStopUntilTime = Time.time + collisionStopDuration;
                remainingDistance = 0f;
            }

            bounceCount++;
        }

        // 최종 ?�치 ?�용 �?방향 ?�데?�트
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
            int creatureLayer = LayerMask.NameToLayer("Creatuer");
            int creature2Layer = LayerMask.NameToLayer("Creature");

            softBodyLayerMask = 0;
            if (bossLayer >= 0) softBodyLayerMask |= (1 << bossLayer);
            if (creatureLayer >= 0) softBodyLayerMask |= (1 << creatureLayer);
            if (creature2Layer >= 0) softBodyLayerMask |= (1 << creature2Layer);

            softBodyLayerMaskCached = true;
        }

        return (softBodyLayerMask & (1 << layer)) != 0;
    }

    private void TryApplyCastContactDamage(Collider2D hitCollider)
    {
        if (!applyCastContactDamage || hitCollider == null)
            return;

        if (ownerBoss == null)
            ownerBoss = GetComponent<BossBase>();

        if (ownerBoss == null || ownerBoss.atk <= 0f)
            return;

        PlayerStatControl playerStat = hitCollider.GetComponentInParent<PlayerStatControl>();
        if (playerStat == null)
            return;

        OnCastPlayerHit?.Invoke(playerStat);

        bool applied = playerStat.TryApplyContactDamage(ownerBoss.atk, gameObject.GetInstanceID());
        LogMoveDebug($"Cast contact damage tried: target={playerStat.name} atk={ownerBoss.atk} applied={applied}", true);
    }

    /// <summary>
    /// 반사 ?�이??마스?��? 가?�옴 (Inspector ?�정 ?�는 기본�?
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
        //int groundLayer = LayerMask.NameToLayer("Ground");
        int playerLayer = LayerMask.NameToLayer("Player");
        int bossLayer = LayerMask.NameToLayer("Boss");
        int creatureLayer = LayerMask.NameToLayer("Creatuer");
        int creature2Layer = LayerMask.NameToLayer("Creature");
        //int enemyLayer = LayerMask.NameToLayer("Enemy");

        int mask = 0;
        if (wallLayer >= 0) mask |= (1 << wallLayer);
        
        if (playerLayer >= 0) mask |= (1 << playerLayer);
        
        
        
        

        return mask;
    }

    /// <summary>
    /// OnCollisionEnter2D/Stay2D fallback??반사 처리
    /// </summary>
    public void HandleCollisionReflection(Collision2D collision, bool applyStopWindow)
    {
        if (collision == null || collision.contactCount <= 0)
            return;

        // 반사 ?�???�이??체크
        int layer = collision.gameObject.layer;
        int mask = GetReflectLayerMask();
        if ((mask & (1 << layer)) == 0)
            return;

        Vector2 dir = CurrentDirection.sqrMagnitude > 0.0001f ? CurrentDirection.normalized : Vector2.right;
        Vector2 normal = collision.contacts[0].normal;

        // ?��? ?�면?�서 멀?��???방향?�면 반사?��? ?�는??
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
    /// ?��??�서 stop-window�??�동?�로 ?�정?????�음
    /// </summary>
    public void SetStopWindow(float duration)
    {
        collisionStopUntilTime = Time.time + Mathf.Max(0f, duration);
    }

    /// <summary>
    /// ?�재 stop-window가 ?�성 ?�태?��? ?�인
    /// </summary>
    public bool IsInStopWindow()
    {
        return Time.time < collisionStopUntilTime;
    }
}


