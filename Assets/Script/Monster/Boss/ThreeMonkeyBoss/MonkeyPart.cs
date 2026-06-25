using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonkeyPart : BossBase
{
    [Header("Monkey Identity")]
    public MonkeyEffectType effectType = MonkeyEffectType.Eye;
    public float collisionEffectDuration = 3f;

    [Header("Stuck Detection")]
    [SerializeField] private float stuckCheckInterval = 0.2f; // 끼임 체크 간격
    [SerializeField] private float stuckThreshold = 0.2f; // 이동 판정 최소 거리
    [SerializeField] private float unstuckMoveStep = 0.5f; // 탈출 시도 이동 거리
    [SerializeField] private int maxUnstuckAttempts = 10; // 최대 탈출 시도 횟수

    private BallisticMovementComponent ballisticMovement;
    private float btMoveSpeedMultiplier = 1f;
    private Coroutine stuckDetectionCoroutine;

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

        // BallisticMovementComponent 초기화
        ballisticMovement = GetComponent<BallisticMovementComponent>();
        if (ballisticMovement == null)
            ballisticMovement = gameObject.AddComponent<BallisticMovementComponent>();
        ballisticMovement.OnCastPlayerHit = HandleCastPlayerHit;

        Vector2 initialDir = vecter.sqrMagnitude > 0.0001f ? vecter.normalized : Vector2.right;
        ballisticMovement.CurrentDirection = initialDir;
        ballisticMovement.SpeedMultiplier = btMoveSpeedMultiplier;

        // Blackboard 초기화 및 방향 저장
        if (blackboard == null)
            blackboard = new BossBTBlackboard();
        blackboard.Set("MoveDirection", initialDir);

        invincibility = false;
        wait = false;

        // StatSet 경로를 타지 않는 분리 보스는 Init 시점에 BT를 직접 준비/시작해야 이동한다.
        behaviorTreeRoot = CreateBehaviorTree();
        StartBrain();
        UIBossHP.NotifyBossEngaged(this);

        // 끼임 감지 시작
        StartStuckDetection();
    }

    public override void BossDie()
    {
        // 끼임 감지 중단
        if (stuckDetectionCoroutine != null)
        {
            StopCoroutine(stuckDetectionCoroutine);
            stuckDetectionCoroutine = null;
        }

        base.BossDie();
        gameObject.SetActive(false);
    }

    public override void Damege(float damege)
    {
        base.Damege(damege);
    }
    public override void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerStatControl playerStat = ResolvePlayerStat(collision);
        if (playerStat != null)
        {
            Debug.Log($"[MonkeyPart] Player collision detected: {playerStat.name}");
            bool applied = playerStat.TryApplyContactDamage(atk, gameObject.GetInstanceID());
            Debug.Log($"[MonkeyPart] Contact damage applied={applied}, atk={atk}");

            var receiver = playerStat.GetComponent<PlayerBossStatusEffectReceiver>();
            if (receiver == null)
                receiver = playerStat.gameObject.AddComponent<PlayerBossStatusEffectReceiver>();

            receiver.ApplyEffect(effectType, collisionEffectDuration);
        }
        else
        {
            base.OnCollisionEnter2D(collision);
        }

        TryReflectByCollision(collision, true);
    }

    private PlayerStatControl ResolvePlayerStat(Collision2D collision)
    {
        if (collision == null)
            return null;

        if (collision.gameObject != null && collision.gameObject.TryGetComponent(out PlayerStatControl direct))
            return direct;

        if (collision.collider != null)
        {
            PlayerStatControl fromCollider = collision.collider.GetComponentInParent<PlayerStatControl>();
            if (fromCollider != null)
                return fromCollider;
        }

        if (collision.rigidbody != null)
        {
            PlayerStatControl fromRigidbody = collision.rigidbody.GetComponentInParent<PlayerStatControl>();
            if (fromRigidbody != null)
                return fromRigidbody;
        }

        return null;
    }

    private void HandleCastPlayerHit(PlayerStatControl playerStat)
    {
        if (playerStat == null)
            return;

        var receiver = playerStat.GetComponent<PlayerBossStatusEffectReceiver>();
        if (receiver == null)
            receiver = playerStat.gameObject.AddComponent<PlayerBossStatusEffectReceiver>();

        receiver.ApplyEffect(effectType, collisionEffectDuration);
    }

    public override void OnCollisionStay2D(Collision2D collision)
    {
        base.OnCollisionStay2D(collision);
        // Stay 구간에서 stop-window를 매 프레임 갱신하면 서로 붙어 멈춘 것처럼 보일 수 있다.
        TryReflectByCollision(collision, false);
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
                new BTTask_BallisticMove(this, "MoveDirection")
            ),
            new BossActionNode(() => BossBTState.Running)
        );
    }

    private void TryReflectByCollision(Collision2D collision, bool applyStopWindow)
    {
        if (collision == null || collision.collider == null)
            return;

        int layer = collision.collider.gameObject.layer;
        
        ContactPoint2D[] contacts = new ContactPoint2D[collision.contactCount];
        collision.GetContacts(contacts);

        if (contacts.Length == 0)
            return;

        Vector2 avgNormal = Vector2.zero;
        foreach (var contact in contacts)
            avgNormal += contact.normal;

        if (avgNormal.sqrMagnitude < 0.0001f)
            avgNormal = Vector2.right;

        avgNormal.Normalize();

        // BallisticMovementComponent에서 방향 가져오기
        if (ballisticMovement == null)
            return;

        Vector2 currentDir = ballisticMovement.CurrentDirection;
        float reflectedDot = Vector2.Dot(currentDir, avgNormal);

        // 이미 벗어나는 중이면 반사하지 않음
        if (reflectedDot > 0f)
            return;

        // 반사 적용
        Vector2 newDir = Vector2.Reflect(currentDir, avgNormal).normalized;
        ballisticMovement.CurrentDirection = newDir;
        
        // Blackboard에도 업데이트
        if (blackboard != null)
            blackboard.Set("MoveDirection", newDir);

        // stop-window 적용 (Enter시에만)
        if (applyStopWindow)
        {
            int bossLayer = LayerMask.NameToLayer("Boss");
            int creatureLayer = LayerMask.NameToLayer("Creatuer");
            int creature2Layer = LayerMask.NameToLayer("Creature");
            
            bool isSoftBody = (layer == bossLayer || layer == creatureLayer || layer == creature2Layer);
            
            if (!isSoftBody && ballisticMovement != null)
            {
                ballisticMovement.SetStopWindow(0.04f);
            }
        }
    }

    /// <summary>
    /// 끼임 감지 시작 - 소환 직후 자동으로 호출됨
    /// </summary>
    private void StartStuckDetection()
    {
        if (stuckDetectionCoroutine != null)
            StopCoroutine(stuckDetectionCoroutine);

        stuckDetectionCoroutine = StartCoroutine(CoStuckDetection());
    }

    /// <summary>
    /// 끼임 감지 코루틴: 0.2초마다 위치 체크하여 안 움직이면 탈출 시도
    /// </summary>
    private IEnumerator CoStuckDetection()
    {
        yield return new WaitForSeconds(0.1f); // 초기 대기 (소환 애니메이션 고려)

        Vector2 lastPosition = transform.position;
        int attemptCount = 0;

        while (attemptCount < maxUnstuckAttempts)
        {
            // 체크 간격 대기
            yield return new WaitForSeconds(stuckCheckInterval);

            if (!live || isDead)
            {
                Debug.Log($"[MonkeyPart] Stuck detection stopped (dead)");
                yield break;
            }

            Vector2 currentPosition = transform.position;
            float deltaX = Mathf.Abs(currentPosition.x - lastPosition.x);
            float deltaY = Mathf.Abs(currentPosition.y - lastPosition.y);

            // X 또는 Y 중 하나라도 stuckThreshold 이상 움직였으면 정상 동작 중
            if (deltaX >= stuckThreshold || deltaY >= stuckThreshold)
            {
                //Debug.Log($"[MonkeyPart] Moving normally. Delta: ({deltaX:F3}, {deltaY:F3})");
                lastPosition = currentPosition;
                attemptCount = 0; // 탈출 시도 카운트 리셋
                continue;
            }

            // 끼임 감지!
            attemptCount++;
            Debug.LogWarning($"[MonkeyPart] Stuck detected! Attempt {attemptCount}/{maxUnstuckAttempts}. Delta: ({deltaX:F3}, {deltaY:F3})");

            // 스테이지 중심으로 이동 시도
            Vector2 stageCenter = GetStageCenter();
            Vector2 toCenter = (stageCenter - currentPosition).normalized;
            Vector2 newPosition = currentPosition + toCenter * unstuckMoveStep;

            transform.position = newPosition;
            lastPosition = newPosition;

            Debug.Log($"[MonkeyPart] Moved towards center: {currentPosition} → {newPosition}");
        }

        // 최대 시도 횟수 도달
        Debug.LogError($"[MonkeyPart] Failed to unstuck after {maxUnstuckAttempts} attempts. Forcing to stage center.");
        transform.position = GetStageCenter();
    }

    /// <summary>
    /// 스테이지 중심 위치 가져오기 (nxnZone 중심 우선)
    /// </summary>
    private Vector2 GetStageCenter()
    {
        if (StageOwner != null)
            return StageOwner.GetZoneCenter();

        if (GameManager.Instance != null && GameManager.Instance.transform != null)
            return GameManager.Instance.transform.position;

        return Vector2.zero; // 최후의 수단
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
}
