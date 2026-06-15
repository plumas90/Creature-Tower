using System;
using UnityEngine;

/// <summary>
/// 재사용 가능한 BT Task의 베이스 클래스.
/// 모든 커스텀 Task는 이 클래스를 상속해서 OnEnter/OnTick/OnExit를 구현한다.
/// </summary>
public abstract class BTTask : BossBTNode
{
    protected BossBase boss;
    protected BossBTBlackboard blackboard;

    private bool isEntered;

    /// <summary>
    /// Task를 생성한다.
    /// </summary>
    /// <param name="boss">이 Task를 실행할 보스</param>
    public BTTask(BossBase boss)
    {
        this.boss = boss;
        this.blackboard = boss?.blackboard;
        this.isEntered = false;
    }

    /// <summary>
    /// BT 시스템에서 호출하는 Tick. 라이프사이클을 관리한다.
    /// </summary>
    public sealed override BossBTState Tick()
    {
        // Task 진입 시 OnEnter 1회 호출
        if (!isEntered)
        {
            isEntered = true;
            OnEnter();
        }

        // 매 Tick마다 OnTick 호출
        BossBTState state = OnTick();

        // Success 또는 Failure로 종료되면 OnExit 호출
        if (state == BossBTState.Success || state == BossBTState.Failure)
        {
            if (isEntered)
            {
                OnExit();
                isEntered = false;
            }
        }

        return state;
    }

    /// <summary>
    /// Task가 처음 실행될 때 1회 호출된다.
    /// 초기화 작업을 여기서 수행한다.
    /// </summary>
    protected virtual void OnEnter()
    {
        // 기본 구현 없음 (필요하면 override)
    }

    /// <summary>
    /// 매 Tick마다 호출된다. Task의 실제 로직을 여기에 구현한다.
    /// </summary>
    /// <returns>Success, Failure, Running 중 하나</returns>
    protected abstract BossBTState OnTick();

    /// <summary>
    /// Task가 종료될 때 (Success 또는 Failure) 1회 호출된다.
    /// 정리 작업을 여기서 수행한다.
    /// </summary>
    protected virtual void OnExit()
    {
        // 기본 구현 없음 (필요하면 override)
    }

    /// <summary>
    /// Task를 강제로 리셋한다. 다음 Tick에서 OnEnter부터 다시 시작한다.
    /// </summary>
    public override void Reset()
    {
        if (isEntered)
        {
            OnExit();
            isEntered = false;
        }
    }

    /// <summary>
    /// Blackboard에서 값을 안전하게 가져온다.
    /// </summary>
    protected T GetBlackboardValue<T>(string key, T defaultValue = default(T))
    {
        if (blackboard == null)
            return defaultValue;

        if (blackboard.TryGet<T>(key, out T value))
            return value;

        return defaultValue;
    }

    /// <summary>
    /// Blackboard에 값을 저장한다.
    /// </summary>
    protected void SetBlackboardValue<T>(string key, T value)
    {
        if (blackboard != null)
            blackboard.Set(key, value);
    }

    /// <summary>
    /// 보스가 유효한지 확인한다.
    /// </summary>
    protected bool IsBossValid()
    {
        return boss != null && boss.live && !boss.wait;
    }

    /// <summary>
    /// 플레이어 GameObject를 가져온다.
    /// </summary>
    protected GameObject GetPlayer()
    {
        return boss?.Player;
    }

    /// <summary>
    /// 플레이어 Transform을 가져온다.
    /// </summary>
    protected Transform GetPlayerTransform()
    {
        GameObject player = GetPlayer();
        return player != null ? player.transform : null;
    }

    /// <summary>
    /// 보스 Transform을 가져온다.
    /// </summary>
    protected Transform GetBossTransform()
    {
        return boss?.transform;
    }


/// <summary>
    /// 디버그 로그 출력 (보스 이름 포함)
    /// </summary>
    protected void LogDebug(string message)
    {
        if (boss != null)
            Debug.Log($"[{boss.GetType().Name}][{GetType().Name}] {message}");
        else
            Debug.Log($"[{GetType().Name}] {message}");
    }
}

/// <summary>
/// 간단한 액션을 람다로 실행하기 위한 헬퍼 Task.
/// 기존 BossActionNode와의 호환성을 위해 제공.
/// </summary>
public class BTTask_Lambda : BTTask
{
    private readonly Func<BossBTState> action;

    public BTTask_Lambda(BossBase boss, Func<BossBTState> action) : base(boss)
    {
        this.action = action;
    }

    protected override BossBTState OnTick()
    {
        return action != null ? action() : BossBTState.Failure;
    }
}

/// <summary>
/// 조건을 체크하는 Task.
/// </summary>
public class BTTask_Condition : BTTask
{
    private readonly Func<bool> condition;

    public BTTask_Condition(BossBase boss, Func<bool> condition) : base(boss)
    {
        this.condition = condition;
    }

    protected override BossBTState OnTick()
    {
        return condition != null && condition() ? BossBTState.Success : BossBTState.Failure;
    }
}
