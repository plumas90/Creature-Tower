using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonkeyPart : BossBase
{
    [Header("Monkey Identity")]
    public MonkeyEffectType effectType = MonkeyEffectType.Eye;
    public float collisionEffectDuration = 3f;

    private BallisticMovementComponent ballisticMovement;
    private float btMoveSpeedMultiplier = 1f;

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

        PlayerStatControl playerStat = ResolvePlayerStat(collision);
        if (playerStat != null)
        {
            var receiver = playerStat.GetComponent<PlayerBossStatusEffectReceiver>();
            if (receiver == null)
                receiver = playerStat.gameObject.AddComponent<PlayerBossStatusEffectReceiver>();

            receiver.ApplyEffect(effectType, collisionEffectDuration);
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
