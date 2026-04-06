using System;
using UnityEngine;

/// <summary>
/// Decorator 노드들: 자식 노드의 동작을 변형하는 노드들
/// </summary>

/// <summary>
/// 자식 노드의 결과를 반전시킨다 (Success ↔ Failure).
/// Running은 그대로 유지.
/// </summary>
public sealed class BTDecorator_Inverter : BossBTNode
{
    private readonly BossBTNode child;

    public BTDecorator_Inverter(BossBTNode child)
    {
        this.child = child;
    }

    public override BossBTState Tick()
    {
        if (child == null)
            return BossBTState.Failure;

        BossBTState state = child.Tick();

        if (state == BossBTState.Success)
            return BossBTState.Failure;
        if (state == BossBTState.Failure)
            return BossBTState.Success;

        return state; // Running은 그대로
    }
}

/// <summary>
/// 자식 노드를 지정 횟수만큼 반복한다.
/// count회 모두 Success면 Success, 중간에 Failure면 Failure.
/// </summary>
public sealed class BTDecorator_Repeater : BossBTNode
{
    private readonly BossBTNode child;
    private readonly int maxCount;
    private int currentCount;

    public BTDecorator_Repeater(BossBTNode child, int count)
    {
        this.child = child;
        this.maxCount = Mathf.Max(1, count);
        this.currentCount = 0;
    }

    public override BossBTState Tick()
    {
        if (child == null)
            return BossBTState.Failure;

        while (currentCount < maxCount)
        {
            BossBTState state = child.Tick();

            if (state == BossBTState.Running)
                return BossBTState.Running;

            if (state == BossBTState.Failure)
            {
                currentCount = 0; // 실패 시 리셋
                return BossBTState.Failure;
            }

            // Success면 카운트 증가
            currentCount++;
        }

        // 목표 횟수 달성
        currentCount = 0;
        return BossBTState.Success;
    }
}

/// <summary>
/// 자식 노드를 무한 반복한다. 항상 Running을 반환.
/// </summary>
public sealed class BTDecorator_RepeatForever : BossBTNode
{
    private readonly BossBTNode child;

    public BTDecorator_RepeatForever(BossBTNode child)
    {
        this.child = child;
    }

    public override BossBTState Tick()
    {
        if (child == null)
            return BossBTState.Failure;

        child.Tick();
        return BossBTState.Running; // 항상 Running
    }
}

/// <summary>
/// 자식 노드가 Failure를 반환할 때까지 반복한다.
/// Failure가 나오면 Success를 반환.
/// </summary>
public sealed class BTDecorator_UntilFail : BossBTNode
{
    private readonly BossBTNode child;

    public BTDecorator_UntilFail(BossBTNode child)
    {
        this.child = child;
    }

    public override BossBTState Tick()
    {
        if (child == null)
            return BossBTState.Success;

        BossBTState state = child.Tick();

        if (state == BossBTState.Failure)
            return BossBTState.Success;

        return BossBTState.Running;
    }
}

/// <summary>
/// 자식 노드의 결과와 관계없이 항상 Success를 반환한다.
/// </summary>
public sealed class BTDecorator_AlwaysSucceed : BossBTNode
{
    private readonly BossBTNode child;

    public BTDecorator_AlwaysSucceed(BossBTNode child)
    {
        this.child = child;
    }

    public override BossBTState Tick()
    {
        if (child != null)
            child.Tick();

        return BossBTState.Success;
    }
}

/// <summary>
/// 자식 노드의 결과와 관계없이 항상 Failure를 반환한다.
/// </summary>
public sealed class BTDecorator_AlwaysFail : BossBTNode
{
    private readonly BossBTNode child;

    public BTDecorator_AlwaysFail(BossBTNode child)
    {
        this.child = child;
    }

    public override BossBTState Tick()
    {
        if (child != null)
            child.Tick();

        return BossBTState.Failure;
    }
}

/// <summary>
/// 지정된 시간 동안 자식 노드 실행을 지연시킨다.
/// </summary>
public sealed class BTDecorator_Delay : BossBTNode
{
    private readonly BossBTNode child;
    private readonly float delaySeconds;
    private float startTime;
    private bool isDelaying;

    public BTDecorator_Delay(BossBTNode child, float delaySeconds)
    {
        this.child = child;
        this.delaySeconds = Mathf.Max(0f, delaySeconds);
        this.isDelaying = false;
    }

    public override BossBTState Tick()
    {
        if (child == null)
            return BossBTState.Failure;

        // 지연 시작
        if (!isDelaying)
        {
            startTime = Time.time;
            isDelaying = true;
        }

        // 지연 시간 체크
        if (Time.time < startTime + delaySeconds)
            return BossBTState.Running;

        // 지연 종료, 자식 실행
        BossBTState state = child.Tick();

        if (state != BossBTState.Running)
            isDelaying = false; // 자식이 종료되면 리셋

        return state;
    }
}

/// <summary>
/// 자식 노드 실행에 제한 시간을 둔다.
/// 제한 시간 내에 완료되지 않으면 Failure.
/// </summary>
public sealed class BTDecorator_Timeout : BossBTNode
{
    private readonly BossBTNode child;
    private readonly float timeoutSeconds;
    private float startTime;
    private bool isRunning;

    public BTDecorator_Timeout(BossBTNode child, float timeoutSeconds)
    {
        this.child = child;
        this.timeoutSeconds = Mathf.Max(0f, timeoutSeconds);
        this.isRunning = false;
    }

    public override BossBTState Tick()
    {
        if (child == null)
            return BossBTState.Failure;

        // 실행 시작
        if (!isRunning)
        {
            startTime = Time.time;
            isRunning = true;
        }

        // 타임아웃 체크
        if (Time.time >= startTime + timeoutSeconds)
        {
            isRunning = false;
            return BossBTState.Failure;
        }

        // 자식 실행
        BossBTState state = child.Tick();

        if (state != BossBTState.Running)
            isRunning = false;

        return state;
    }
}
