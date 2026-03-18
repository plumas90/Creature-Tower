using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class NewBossExample : BossBase
{
    [Header("Intro")]
    [SerializeField] private float postIntroDelay = 1f;
    [SerializeField] private Animator introAnimator;
    [SerializeField] private AnimationClip introClip;
    [SerializeField] private string introStateName = "BossIntro";

    [Header("Simple AI")]
    [SerializeField] private float chaseDistance = 12f;

    private Transform target;
    private PlayableGraph introGraph;

    public override void OnBossActivatedBeforeIntro()
    {
        if (introAnimator != null)
            introAnimator.speed = 0f;
    }

    protected override void OnBeforeIntroStart()
    {
        if (introAnimator != null)
            introAnimator.speed = 1f;

        if (TryPlayIntroState())
            return;

        PlayIntroClipWithPlayable();
    }

    protected override float ResolveIntroTime()
    {
        if (introClip != null)
            return Mathf.Max(0f, introClip.length);

        return base.ResolveIntroTime();
    }

    protected override float ResolvePostIntroDelay()
    {
        return Mathf.Max(0f, postIntroDelay);
    }

    protected override BossBTNode CreateBehaviorTree()
    {
        return new BossSelectorNode(
            new BossSequenceNode(
                new BossConditionNode(() => live && !wait),
                new BossConditionNode(EnsureTarget),
                new BossConditionNode(IsTargetInRange),
                new BossActionNode(() =>
                {
                    MoveTowardTarget();
                    return BossBTState.Running;
                })
            ),
            new BossActionNode(() => BossBTState.Running)
        );
    }

    public override void First()
    {
        StopIntroGraph();
        EnsureTarget();
    }

    protected override void OnDisable()
    {
        StopIntroGraph();
        base.OnDisable();
    }

    private bool EnsureTarget()
    {
        if (target == null && Player != null)
            target = Player.transform;

        return target != null;
    }

    private bool IsTargetInRange()
    {
        if (target == null)
            return false;

        float sqr = ((Vector2)target.position - (Vector2)transform.position).sqrMagnitude;
        return sqr <= chaseDistance * chaseDistance;
    }

    private void MoveTowardTarget()
    {
        if (target == null)
            return;

        Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
        transform.Translate(dir * speed * Time.deltaTime);
    }

    private bool TryPlayIntroState()
    {
        if (introAnimator == null || string.IsNullOrEmpty(introStateName))
            return false;

        int stateHash = Animator.StringToHash(introStateName);
        if (!introAnimator.HasState(0, stateHash))
            return false;

        introAnimator.Play(stateHash, 0, 0f);
        introAnimator.Update(0f);
        return true;
    }

    private void PlayIntroClipWithPlayable()
    {
        if (introAnimator == null || introClip == null)
            return;

        StopIntroGraph();

        introGraph = PlayableGraph.Create($"{name}_IntroGraph");
        introGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(introGraph, "IntroOutput", introAnimator);
        AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(introGraph, introClip);
        clipPlayable.SetApplyFootIK(false);
        clipPlayable.SetApplyPlayableIK(false);
        clipPlayable.SetTime(0d);
        clipPlayable.SetSpeed(1d);
        output.SetSourcePlayable(clipPlayable);

        introGraph.Play();
    }

    private void StopIntroGraph()
    {
        if (introGraph.IsValid())
            introGraph.Destroy();
    }
}
