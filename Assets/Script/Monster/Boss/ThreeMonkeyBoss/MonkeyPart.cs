using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonkeyPart : BossBase
{
    [Header("Monkey Identity")]
    public MonkeyEffectType effectType = MonkeyEffectType.Eye;
    public float collisionEffectDuration = 1f;

    Vector2 direction = Vector2.zero;
    private float btMoveSpeedMultiplier = 1f;
    private Collider2D moveCollider;
    private Rigidbody2D moveBody2D;
    private readonly List<RaycastHit2D> sweepHits = new List<RaycastHit2D>(8);
    private ContactFilter2D moveContactFilter;
    private bool moveContactFilterReady;
    [SerializeField] [Min(0f)] private float sweepSkin = 0.02f;
    [SerializeField] [Range(0, 4)] private int maxRaycastBouncesPerTick = 2;
    [SerializeField] [Min(0f)] private float collisionStopDuration = 0.04f;
    [SerializeField] private bool enableMoveDebug = true;
    [SerializeField] [Min(0.02f)] private float moveDebugInterval = 0.15f;
    private int reflectLayerMask;
    private bool reflectLayerMaskCached;
    private float collisionStopUntilTime;
    private float nextMoveDebugTime;

    public void Init(Vector2 vecter)
    {
        EnsureMinionLayer();

        bossCount = 1;
        atk = MainSO.atk;
        maxHp = MainSO.hp;
        curHp = MainSO.hp;
        speed = MainSO.speed;
        btMoveSpeedMultiplier = (MainSO != null && MainSO.btMoveSpeedMultiplier > 0f) ? MainSO.btMoveSpeedMultiplier : 1f;
        live = true;

        if (GameManager.Instance != null)
            Player = GameManager.Instance.playerOBJ;

        direction = vecter.sqrMagnitude > 0.0001f ? vecter.normalized : Vector2.right;
        invincibility = false;
        wait = false;

        Debug.Log($"[MonkeyPart][MoveDebug] enabled={enableMoveDebug} interval={moveDebugInterval:F2} name={name}");

        // StatSet 경로를 타지 않는 분리 보스는 Init 시점에 BT를 직접 준비/시작해야 이동한다.
        behaviorTreeRoot = CreateBehaviorTree();
        StartBrain();
    }

    public override void BossDie()
    {
        base.BossDie();
        gameObject.SetActive(false);
    }

    public override void Damege(float damege)
    {
        base.Damege(damege);
    }
    public override void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision);

        if (collision.gameObject.TryGetComponent(out PlayerStatControl playerStat))
        {
            var receiver = playerStat.GetComponent<PlayerBossStatusEffectReceiver>();
            if (receiver == null)
                receiver = playerStat.gameObject.AddComponent<PlayerBossStatusEffectReceiver>();

            receiver.ApplyEffect(effectType, collisionEffectDuration);
        }

        TryReflectByCollision(collision, true);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        base.OnCollisionStay2D(collision);
        // Stay 구간에서 stop-window를 매 프레임 갱신하면 서로 붙어 멈춘 것처럼 보일 수 있다.
        TryReflectByCollision(collision, false);
    }

    private void TryReflectByCollision(Collision2D collision, bool applyStopWindow)
    {
        if (collision == null)
            return;

        if (collision.contactCount <= 0)
            return;

        if (!IsReflectTargetLayer(collision.gameObject.layer))
            return;

        Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        Vector2 normal = collision.contacts[0].normal;

        if (Vector2.Dot(dir, normal) >= 0f)
            return;

        direction = Vector2.Reflect(dir, normal).normalized;
        if (applyStopWindow)
            collisionStopUntilTime = Time.time + collisionStopDuration;
        LogMoveDebug($"fallback reflect by collision layer={collision.gameObject.layer} normal={normal} dir={direction}", true);
    }

    public void Update()
    {
    }

    private void FixedUpdate()
    {
        TickBehaviorTree();
    }

    protected override BossBTNode CreateBehaviorTree()
    {
        return new BossSelectorNode(
            new BossSequenceNode(
                new BossConditionNode(() => live && !wait),
                new BossActionNode(() =>
                {
                    SweptMoveAndReflect();
                    return BossBTState.Running;
                })
            ),
            new BossActionNode(() => BossBTState.Running)
        );
    }

    private void SweptMoveAndReflect()
    {
        if (Time.time < collisionStopUntilTime)
        {
            LogMoveDebug($"stop-window active until={collisionStopUntilTime:F3}");
            return;
        }

        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.right;

        float remainingDistance = speed * btMoveSpeedMultiplier * Time.fixedDeltaTime;
        if (remainingDistance <= 0f)
            return;

        EnsureMoveCollider();
        if (moveBody2D == null)
        {
            LogMoveDebug("Rigidbody2D missing; movement skipped", true);
            return;
        }

        Vector2 moveDir = direction.normalized;
        Vector2 startPos = moveBody2D.position;
        Vector2 simPos = startPos;
        bool collided = false;
        string hitName = "none";
        Vector2 hitNormal = Vector2.zero;
        int bounceCount = 0;
        int maxBounces = Mathf.Max(0, maxRaycastBouncesPerTick);

        while (remainingDistance > 0f)
        {
            float castDistance = remainingDistance + sweepSkin;
            sweepHits.Clear();
            int hitCount = moveBody2D.Cast(simPos, moveBody2D.rotation, moveDir, moveContactFilter, sweepHits, castDistance);
            bool hasHit = TryGetNearestCastHit(hitCount, out RaycastHit2D nearestHit);

            if (!hasHit)
            {
                simPos += moveDir * remainingDistance;
                remainingDistance = 0f;
                break;
            }

            float safeDistance = Mathf.Max(0f, nearestHit.distance - sweepSkin);
            if (safeDistance > 0f)
                simPos += moveDir * safeDistance;

            collided = true;
            hitName = nearestHit.collider != null ? nearestHit.collider.name : "null";
            hitNormal = nearestHit.normal;

            remainingDistance -= safeDistance;
            if (remainingDistance <= 0f || bounceCount >= maxBounces)
                break;

            moveDir = Vector2.Reflect(moveDir, nearestHit.normal).normalized;
            bool softBodyHit = IsSoftBodyHitLayer(nearestHit.collider != null ? nearestHit.collider.gameObject.layer : -1);
            if (softBodyHit)
            {
                float separation = Mathf.Max(0.005f, sweepSkin * 0.5f);
                simPos += nearestHit.normal * separation;
                remainingDistance = Mathf.Max(0f, remainingDistance - separation);
            }
            else
            {
                collisionStopUntilTime = Time.time + collisionStopDuration;
                // 벽/장애물 충돌은 기존처럼 즉시 잔여 이동을 끊어 터널링을 줄인다.
                remainingDistance = 0f;
            }
            bounceCount++;
        }

        direction = moveDir;
        moveBody2D.MovePosition(simPos);
        LogMoveDebug($"from={startPos} to={simPos} dir={direction} collided={collided} hit={hitName} normal={hitNormal} bounces={bounceCount}");
    }

    private void LogMoveDebug(string message, bool force = false)
    {
        if (!enableMoveDebug)
            return;

        if (!force && Time.time < nextMoveDebugTime)
            return;

        nextMoveDebugTime = Time.time + moveDebugInterval;
        Debug.Log($"[MonkeyPart][MoveDebug] {message}");
    }

    private void EnsureMoveCollider()
    {
        if (moveCollider != null)
            return;

        moveCollider = GetComponent<Collider2D>();
        moveBody2D = Body2D != null ? Body2D : GetComponent<Rigidbody2D>();

        if (moveBody2D != null)
        {
            moveContactFilter.useLayerMask = true;
            moveContactFilter.layerMask = GetReflectLayerMask();
            moveContactFilter.useTriggers = false;
            moveContactFilter.useDepth = false;
            moveContactFilterReady = true;
        }
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
            if (!IsValidCastHit(hit))
                continue;

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearestHit = hit;
                hasHit = true;
            }
        }

        return hasHit;
    }

    private bool IsValidCastHit(RaycastHit2D hit)
    {
        Collider2D col = hit.collider;
        if (col == null)
            return false;

        if (col.isTrigger)
            return false;

        if (col.transform.root == transform.root)
            return false;

        if (!IsReflectTargetLayer(col.gameObject.layer))
            return false;

        return hit.distance >= 0f;
    }

    private void EnsureMinionLayer()
    {
        int creatureLayer = LayerMask.NameToLayer("Creatuer");
        if (creatureLayer < 0)
            return;

        SetLayerRecursively(gameObject, creatureLayer);
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null)
            return;

        target.layer = layer;
        Transform tr = target.transform;
        for (int i = 0; i < tr.childCount; i++)
        {
            Transform child = tr.GetChild(i);
            if (child != null)
                SetLayerRecursively(child.gameObject, layer);
        }
    }

    private int GetReflectLayerMask()
    {
        if (reflectLayerMaskCached)
            return reflectLayerMask;

        reflectLayerMaskCached = true;
        reflectLayerMask = 0;
        AddLayerToMask("Wall");
        AddLayerToMask("Player");
        AddLayerToMask("Creatuer");
        AddLayerToMask("Creature");
        AddLayerToMask("Enemy");
        AddLayerToMask("Boss");
        return reflectLayerMask;
    }

    private void AddLayerToMask(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
            return;

        reflectLayerMask |= 1 << layer;
    }

    private bool IsReflectTargetLayer(int layer)
    {
        int wall = LayerMask.NameToLayer("Wall");
        int player = LayerMask.NameToLayer("Player");
        int creatureTypo = LayerMask.NameToLayer("Creatuer");
        int creature = LayerMask.NameToLayer("Creature");
        int enemy = LayerMask.NameToLayer("Enemy");
        int boss = LayerMask.NameToLayer("Boss");

        return layer == wall
            || layer == player
            || layer == enemy
            || layer == boss
            || (creatureTypo >= 0 && layer == creatureTypo)
            || (creature >= 0 && layer == creature);
    }

    private bool IsSoftBodyHitLayer(int layer)
    {
        int creatureTypo = LayerMask.NameToLayer("Creatuer");
        int creature = LayerMask.NameToLayer("Creature");
        int boss = LayerMask.NameToLayer("Boss");

        return layer == boss
            || (creatureTypo >= 0 && layer == creatureTypo)
            || (creature >= 0 && layer == creature);
    }
}
