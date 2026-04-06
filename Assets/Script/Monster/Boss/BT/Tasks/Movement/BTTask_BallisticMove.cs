using UnityEngine;

/// <summary>
/// BallisticMovementComponent를 사용해서 탄도 이동을 수행하는 BT Task.
/// </summary>
public class BTTask_BallisticMove : BTTask
{
    private BallisticMovementComponent movementComponent;
    private readonly Vector2 initialDirection;
    private readonly bool useBlackboardDirection;
    private readonly string blackboardDirectionKey;

    /// <summary>
    /// 초기 방향을 지정해서 생성.
    /// </summary>
    public BTTask_BallisticMove(BossBase boss, Vector2 initialDirection) : base(boss)
    {
        this.initialDirection = initialDirection;
        this.useBlackboardDirection = false;
        this.blackboardDirectionKey = null;
    }

    /// <summary>
    /// Blackboard에서 방향을 읽어오도록 생성.
    /// </summary>
    public BTTask_BallisticMove(BossBase boss, string directionKey) : base(boss)
    {
        this.initialDirection = Vector2.right;
        this.useBlackboardDirection = true;
        this.blackboardDirectionKey = directionKey;
    }

    protected override void OnEnter()
    {
        if (boss == null)
            return;

        // BallisticMovementComponent 가져오기 또는 추가
        movementComponent = boss.GetComponent<BallisticMovementComponent>();
        if (movementComponent == null)
        {
            movementComponent = boss.gameObject.AddComponent<BallisticMovementComponent>();
        }

        // 초기 방향 설정
        Vector2 direction = useBlackboardDirection 
            ? GetBlackboardValue(blackboardDirectionKey, initialDirection)
            : initialDirection;

        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.right;

        movementComponent.CurrentDirection = direction.normalized;

        // 속도 배율 설정 (MainSO에서 가져오기)
        if (boss.MainSO != null && boss.MainSO.btMoveSpeedMultiplier > 0f)
            movementComponent.SpeedMultiplier = boss.MainSO.btMoveSpeedMultiplier;
        else
            movementComponent.SpeedMultiplier = 1f;
    }

    protected override BossBTState OnTick()
    {
        if (!IsBossValid() || movementComponent == null)
            return BossBTState.Failure;

        // Blackboard에서 방향 업데이트 (필요 시)
        if (useBlackboardDirection && !string.IsNullOrEmpty(blackboardDirectionKey))
        {
            Vector2 direction = GetBlackboardValue<Vector2>(blackboardDirectionKey, movementComponent.CurrentDirection);
            if (direction.sqrMagnitude > 0.0001f)
                movementComponent.CurrentDirection = direction.normalized;
        }

        // 탄도 이동 수행
        movementComponent.MoveBallistic(boss.speed);

        // 변경된 방향을 Blackboard에 저장 (필요 시)
        if (useBlackboardDirection && !string.IsNullOrEmpty(blackboardDirectionKey))
        {
            SetBlackboardValue(blackboardDirectionKey, movementComponent.CurrentDirection);
        }

        return BossBTState.Running;
    }

    protected override void OnExit()
    {
        // 정리 작업 (필요 시)
    }
}
