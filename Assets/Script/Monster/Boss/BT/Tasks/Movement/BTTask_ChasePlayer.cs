using UnityEngine;

/// <summary>
/// PlayerTargetingComponent를 사용해서 플레이어를 추적하는 BT Task.
/// </summary>
public class BTTask_ChasePlayer : BTTask
{
    private PlayerTargetingComponent targetingComponent;
    private readonly float moveSpeed;
    private readonly float chaseRange;
    private readonly bool useComponentSpeed;

    /// <summary>
    /// 이동 속도를 직접 지정해서 생성.
    /// </summary>
    public BTTask_ChasePlayer(BossBase boss, float moveSpeed, float chaseRange = 15f) : base(boss)
    {
        this.moveSpeed = moveSpeed;
        this.chaseRange = chaseRange;
        this.useComponentSpeed = false;
    }

    /// <summary>
    /// 보스의 speed를 사용하도록 생성.
    /// </summary>
    public BTTask_ChasePlayer(BossBase boss, float chaseRange = 15f) : base(boss)
    {
        this.moveSpeed = 0f;
        this.chaseRange = chaseRange;
        this.useComponentSpeed = true;
    }

    protected override void OnEnter()
    {
        if (boss == null)
            return;

        // PlayerTargetingComponent 가져오기 또는 추가
        targetingComponent = boss.GetComponent<PlayerTargetingComponent>();
        if (targetingComponent == null)
        {
            targetingComponent = boss.gameObject.AddComponent<PlayerTargetingComponent>();
        }
    }

    protected override BossBTState OnTick()
    {
        if (!IsBossValid() || targetingComponent == null)
            return BossBTState.Failure;

        // 타겟이 유효하지 않으면 실패
        if (!targetingComponent.IsTargetValid)
            return BossBTState.Failure;

        // 추적 범위 체크
        if (!targetingComponent.IsTargetInRange(chaseRange))
            return BossBTState.Failure;

        // 플레이어 방향 계산
        Vector2 direction = targetingComponent.GetDirectionToTarget();
        if (direction.sqrMagnitude < 0.0001f)
            return BossBTState.Failure;

        // 이동 속도 결정
        float speed = useComponentSpeed ? boss.speed : moveSpeed;
        if (speed <= 0f)
            return BossBTState.Failure;

        // 이동
        Vector2 movement = direction * speed * Time.deltaTime;
        boss.transform.position = (Vector2)boss.transform.position + movement;

        // Blackboard에 플레이어 정보 저장 (다른 Task에서 사용 가능)
        SetBlackboardValue("PlayerPosition", (Vector2)targetingComponent.TargetTransform.position);
        SetBlackboardValue("PlayerDirection", direction);
        SetBlackboardValue("PlayerDistance", targetingComponent.GetDistanceToTarget());

        return BossBTState.Running;
    }

    protected override void OnExit()
    {
        // 정리 작업
    }
}
