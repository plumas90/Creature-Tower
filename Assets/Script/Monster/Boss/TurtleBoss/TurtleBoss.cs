using UnityEngine;
using System.Collections;

/// <summary>
/// 거북이 보스.
/// 
/// Phase 1 (HP > 30%):
///   - 가시 토네이도: 8방향으로 가시 발사 (쿨다운마다 반복)
///   - 구르기: 플레이어 방향으로 구르면서 벽 반사, 반사 시 가시 발사
///   - 미사일: 플레이어를 향해 3발 순차 발사
///
/// Phase 2 (HP <= 30%):
///   - 무한 구르기: 멈추지 않고 구르며 반사 시마다 가시 발사
/// </summary>
public class TurtleBoss : BossBase
{
    [Header("SO Reference")]
    [SerializeField] private TurtleBossSO turtleSO;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer bodyRenderer;

    // 내부 상태
    private bool isPhase2;
    private Coroutine animCoroutine;

    // BT 태스크 참조 (Phase 2 전환 시 재구성 필요)
    private BossBTNode phase2Tree;

    // ──────────────────── Unity 생명주기 ────────────────────

    protected override void Awake()
    {
        base.Awake();

        if (turtleSO != null)
            MainSO = turtleSO;
    }

    private void FixedUpdate()
    {
        TickBehaviorTree();
    }

    // ──────────────────── StatSet (초기화) ────────────────────

    public override void StatSet()
    {
        if (turtleSO == null && MainSO is TurtleBossSO soFromMain)
            turtleSO = soFromMain;

        if (turtleSO != null)
            MainSO = turtleSO;

        if (turtleSO == null)
        {
            Debug.LogError("[TurtleBoss] TurtleBossSO is not assigned.");
            return;
        }

        isPhase2 = false;
        base.StatSet();
        bossCount = Mathf.Max(1, MainSO != null ? MainSO.bossCount : 1);
    }

    // ──────────────────── 인트로 ────────────────────

    protected override float ResolveIntroTime()
    {
        return MainSO != null ? Mathf.Max(0f, MainSO.IntroAnimationTime) : 2f;
    }

    protected override void OnBeforeIntroStart()
    {
        base.OnBeforeIntroStart();
        // 인트로 애니메이션: idle 첫 프레임으로 설정
        if (bodyRenderer != null && turtleSO?.idleSprites != null && turtleSO.idleSprites.Length > 0)
            bodyRenderer.sprite = turtleSO.idleSprites[0];
    }

    public override void First()
    {
        base.First();
        StartIdleAnimation();
    }

    // ──────────────────── BT 트리 ────────────────────

    protected override BossBTNode CreateBehaviorTree()
    {
        TurtleBossSO so = turtleSO;
        if (so == null)
        {
            Debug.LogError("[TurtleBoss] CreateBehaviorTree: TurtleBossSO is null.");
            return new BossActionNode(() => BossBTState.Running);
        }

        // Phase 2 트리: 무한 구르기
        var phase2Node = new BossSequenceNode(
            new BossConditionNode(() => live && !wait),
            new BTTask_TurtleInfinityRolling(this, so)
        );

        // Phase 1 패턴 (쿨다운 기반 Selector)
        //   - ThornTornado, Rolling, Missile 중 쿨다운이 끝난 것을 순서대로 시도
        var phase1Patterns = new BossSelectorNode(
            // 1순위: 가시 토네이도
            new BossCooldownNode(so.thornTornadoCooldown,
                new BossSequenceNode(
                    new BossConditionNode(() => live && !wait && !isPhase2),
                    new BTTask_TurtleThornTornado(this, so)
                )
            ),
            // 2순위: 구르기
            new BossCooldownNode(so.rollingCooldown,
                new BossSequenceNode(
                    new BossConditionNode(() => live && !wait && !isPhase2),
                    new BTTask_TurtleRolling(this, so, false)
                )
            ),
            // 3순위: 미사일
            new BossCooldownNode(so.missileCooldown,
                new BossSequenceNode(
                    new BossConditionNode(() => live && !wait && !isPhase2),
                    new BTTask_TurtleMissile(this, so)
                )
            )
        );

        // Phase 1 트리
        var phase1Node = new BossSequenceNode(
            new BossConditionNode(() => !isPhase2),
            phase1Patterns
        );

        // 루트 셀렉터: Phase 2 조건 체크 → Phase 1 → 폴백 idle
        return new BossSelectorNode(
            // Phase 2로 전환 체크
            new BossSequenceNode(
                new BossConditionNode(CheckAndSetPhase2),
                phase2Node
            ),
            // Phase 1 패턴
            phase1Node,
            // 폴백: 항상 Running (대기)
            new BossActionNode(() => BossBTState.Running)
        );
    }

    private bool CheckAndSetPhase2()
    {
        if (isPhase2) return true;
        if (maxHp <= 0f) return false;

        float threshold = turtleSO != null ? turtleSO.phase2HpThreshold : 0.3f;
        if (curHp / maxHp <= threshold)
        {
            TriggerPhase2();
            return true;
        }
        return false;
    }

    private void TriggerPhase2()
    {
        if (isPhase2) return;
        isPhase2 = true;
        Debug.Log("[TurtleBoss] Phase 2 activated! Infinity rolling begins.");

        // 구르기 애니메이션으로 전환 (Phase 2는 항상 구름)
        StopIdleAnimation();
        // Phase 2 구르기는 BTTask_TurtleInfinityRolling에서 SetRollingAnim(true) 호출
    }

    // ──────────────────── 애니메이션 ────────────────────

    public void SetRollingAnim(bool rolling)
    {
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
            animCoroutine = null;
        }

        if (rolling)
            animCoroutine = StartCoroutine(CoRollingAnimation());
        else
            animCoroutine = StartCoroutine(CoIdleAnimation());
    }

    private void StartIdleAnimation()
    {
        if (animCoroutine != null)
            StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(CoIdleAnimation());
    }

    private void StopIdleAnimation()
    {
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
            animCoroutine = null;
        }
    }

    private IEnumerator CoIdleAnimation()
    {
        if (turtleSO == null || turtleSO.idleSprites == null || turtleSO.idleSprites.Length == 0)
            yield break;

        float fps = Mathf.Max(1f, turtleSO.idleAnimFps);
        float delay = 1f / fps;
        int frameIndex = 0;

        while (live && !isDead)
        {
            if (bodyRenderer != null)
                bodyRenderer.sprite = turtleSO.idleSprites[frameIndex];

            frameIndex = (frameIndex + 1) % turtleSO.idleSprites.Length;
            yield return new WaitForSeconds(delay);
        }
    }

    private IEnumerator CoRollingAnimation()
    {
        if (turtleSO == null || turtleSO.rollingSprites == null || turtleSO.rollingSprites.Length == 0)
            yield break;

        float fps = Mathf.Max(1f, turtleSO.rollingAnimFps);
        float delay = 1f / fps;
        int frameIndex = 0;

        while (live && !isDead)
        {
            if (bodyRenderer != null)
                bodyRenderer.sprite = turtleSO.rollingSprites[frameIndex];

            frameIndex = (frameIndex + 1) % turtleSO.rollingSprites.Length;
            yield return new WaitForSeconds(delay);
        }
    }

    // ──────────────────── 방향 (좌우 플립) ────────────────────

    private void UpdateFacingDirection()
    {
        if (Player == null || bodyRenderer == null) return;

        bool playerOnLeft = Player.transform.position.x < transform.position.x;
        bodyRenderer.flipX = playerOnLeft;
    }

    // ──────────────────── 사망 ────────────────────

    public override void BossDie()
    {
        if (isDead) return;

        StopIdleAnimation();
        base.BossDie();
        gameObject.SetActive(false);
    }

    protected override void OnDisable()
    {
        StopIdleAnimation();
        base.OnDisable();
    }
}
