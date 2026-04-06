using System;
using System.Collections.Generic;
using UnityEngine;

public enum BossBTState
{
    Success,
    Failure,
    Running,
}

public abstract class BossBTNode
{
    public abstract BossBTState Tick();
}

public sealed class BossSequenceNode : BossBTNode
{
    private readonly List<BossBTNode> children;
    private int currentChildIndex = 0;

    public BossSequenceNode(params BossBTNode[] nodes)
    {
        children = new List<BossBTNode>(nodes);
    }

    public override BossBTState Tick()
    {
        // 모든 자식을 순서대로 실행
        while (currentChildIndex < children.Count)
        {
            BossBTState state = children[currentChildIndex].Tick();
            
            // Failure면 즉시 실패
            if (state == BossBTState.Failure)
            {
                currentChildIndex = 0; // 리셋
                return BossBTState.Failure;
            }
            
            // Running이면 여기서 멈춤 (다음 자식은 실행 안 함!)
            if (state == BossBTState.Running)
            {
                return BossBTState.Running;
            }
            
            // Success면 다음 자식으로
            currentChildIndex++;
        }
        
        // 모든 자식이 Success → Sequence 성공
        currentChildIndex = 0; // 리셋
        return BossBTState.Success;
    }
}

public sealed class BossSelectorNode : BossBTNode
{
    private readonly List<BossBTNode> children;

    public BossSelectorNode(params BossBTNode[] nodes)
    {
        children = new List<BossBTNode>(nodes);
    }

    public override BossBTState Tick()
    {
        for (int i = 0; i < children.Count; i++)
        {
            BossBTState state = children[i].Tick();
            if (state == BossBTState.Success || state == BossBTState.Running)
                return state;
        }

        return BossBTState.Failure;
    }
}

public sealed class BossConditionNode : BossBTNode
{
    private readonly Func<bool> condition;

    public BossConditionNode(Func<bool> condition)
    {
        this.condition = condition;
    }

    public override BossBTState Tick()
    {
        return condition != null && condition() ? BossBTState.Success : BossBTState.Failure;
    }
}

public sealed class BossActionNode : BossBTNode
{
    private readonly Func<BossBTState> action;

    public BossActionNode(Func<BossBTState> action)
    {
        this.action = action;
    }

    public override BossBTState Tick()
    {
        return action != null ? action() : BossBTState.Failure;
    }
}

public sealed class BossCooldownNode : BossBTNode
{
    private readonly BossBTNode child;
    private readonly Func<float> cooldownGetter;
    private float nextAllowedTime;

    public BossCooldownNode(float cooldownSeconds, BossBTNode child)
        : this(() => cooldownSeconds, child)
    {
    }

    public BossCooldownNode(Func<float> cooldownGetter, BossBTNode child)
    {
        this.cooldownGetter = cooldownGetter;
        this.child = child;
        nextAllowedTime = 0f;
    }

    public override BossBTState Tick()
    {
        if (child == null)
            return BossBTState.Failure;

        if (Time.time < nextAllowedTime)
            return BossBTState.Failure;

        BossBTState state = child.Tick();
        if (state == BossBTState.Success)
        {
            float cooldown = Mathf.Max(0f, cooldownGetter != null ? cooldownGetter() : 0f);
            nextAllowedTime = Time.time + cooldown;
        }

        return state;
    }
}

public sealed class BossRandomChanceNode : BossBTNode
{
    private readonly BossBTNode child;
    private readonly Func<float> chanceGetter;

    public BossRandomChanceNode(float chance, BossBTNode child)
        : this(() => chance, child)
    {
    }

    public BossRandomChanceNode(Func<float> chanceGetter, BossBTNode child)
    {
        this.chanceGetter = chanceGetter;
        this.child = child;
    }

    public override BossBTState Tick()
    {
        if (child == null)
            return BossBTState.Failure;

        float chance = Mathf.Clamp01(chanceGetter != null ? chanceGetter() : 1f);
        if (UnityEngine.Random.value > chance)
            return BossBTState.Failure;

        return child.Tick();
    }
}
