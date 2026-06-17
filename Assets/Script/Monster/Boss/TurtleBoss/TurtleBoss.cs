using UnityEngine;
using System.Collections;

/// <summary>
/// 거북이 보스.
/// 
/// Phase 1 (HP > 30%):
///   - 가시 토네이도: 8방향으로 가시 발사 (쿨다운마다 반복)
///   - 구르기: 플레이어 방향으로 구르면서 벽반사, 반사 시마다 가시 발사
///   - 미사일: 플레이어를 향해 3연차 발사
///
/// Phase 2 (HP <= 30%):
///   - 무한 구르기: 멈추지 않고 구르며 반사 시마다 가시 발사
/// </summary>
public class TurtleBoss : BossBase
{
    [Header("Aiming")]
    [SerializeField] private Transform bossAim;

    private void UpdateAim()
    {
        if (bossAim == null || !live || !bossAim.gameObject.activeSelf) return;

        // 인트로 연출 동안(wait = true 또는 introMoveRoutine 실행 중)에는 대포를 플레이어 방향으로 꺾지 않고 기본 상태(0도)로 유지합니다.
        if (wait || introMoveRoutine != null)
        {
            bossAim.localRotation = Quaternion.identity;
            SpriteRenderer aimSR = bossAim.GetComponent<SpriteRenderer>();
            if (aimSR != null)
            {
                aimSR.flipY = false;
            }
            return;
        }

        GameObject playerObj = GameObject.FindWithTag("Player");
        Transform playerTr = playerObj != null ? playerObj.transform : null;
        if (playerTr != null)
        {
            Vector3 directiontarget = (playerTr.position - bossAim.position).normalized;
            float angle = Mathf.Atan2(directiontarget.y, directiontarget.x) * Mathf.Rad2Deg;
            bossAim.rotation = Quaternion.Euler(0, 0, angle);

            // Flip the entire boss Root scale so body and child pivots flip correctly
            Vector3 scale = transform.localScale;
            bool playerOnRight = playerTr.position.x > transform.position.x;
            scale.x = playerOnRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;

            // bossAim의 로컬 스케일을 보정하여 부모 flip 시 발생하는 월드 스케일 반전을 상쇄
            Vector3 aimScale = bossAim.localScale;
            aimScale.x = (scale.x < 0) ? -Mathf.Abs(aimScale.x) : Mathf.Abs(aimScale.x);
            bossAim.localScale = aimScale;

            // Keep bodyRenderer flipX at false so scale is the sole flipping mechanism
            if (bodyRenderer != null)
            {
                bodyRenderer.flipX = false;
            }

            // Prevent the weapon pivot sprite from looking upside-down when pointing left
            SpriteRenderer aimSR = bossAim.GetComponent<SpriteRenderer>();
            if (aimSR != null)
            {
                float normAngle = bossAim.eulerAngles.z;
                if (normAngle > 180f) normAngle -= 360f;
                aimSR.flipY = (Mathf.Abs(normAngle) > 90f);
            }
        }
    }

    [Header("SO Reference")]
    [SerializeField] private TurtleBossSO turtleSO;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Transform makeMissilePosition;

    [Header("Colliders")]
    [SerializeField] private Collider2D normalCollider;
    [SerializeField] private Collider2D rollingCollider;

    public Transform MakeMissilePosition => makeMissilePosition;

    // 보스 상태
    private bool isPhase2;
    private Coroutine idleCoroutine;
    private Coroutine rollingCoroutine;

    public bool IsPhase2 => isPhase2;
    public bool IsRollingState => isRollingState;

    // BT 태스크 참조 (Phase 2 전환 시 복구 필요)
    private BossBTNode phase2Tree;

    // =========================================================
    // Unity 생명주기
    // =========================================================

    protected override void Awake()
    {
        base.Awake();

        if (turtleSO != null)
            MainSO = turtleSO;

        if (makeMissilePosition == null)
        {
            Transform[] allChildren = GetComponentsInChildren<Transform>(true);
            foreach (Transform child in allChildren)
            {
                if (child.name == "MakeMissilePosition" || child.name == "MakeMissilePositon")
                {
                    makeMissilePosition = child;
                    break;
                }
            }
        }

        // 콜라이더 자동 할당
        if (normalCollider == null)
            normalCollider = GetComponent<BoxCollider2D>();
        if (rollingCollider == null)
            rollingCollider = GetComponent<CapsuleCollider2D>();

        if (normalCollider == null)
        {
            Collider2D[] cols = GetComponents<Collider2D>();
            foreach (var col in cols)
            {
                if (!(col is CapsuleCollider2D))
                {
                    normalCollider = col;
                    break;
                }
            }
        }
    }

    private void FixedUpdate()
    {
        TickBehaviorTree();
        UpdateAim();
    }

    // =========================================================
    // StatSet (초기화)
    // =========================================================

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

    protected override float CalculateFinalDamage(float incomingDamage)
    {
        float damage = base.CalculateFinalDamage(incomingDamage);
        if (isRollingState)
        {
            damage *= 0.5f;
        }
        return damage;
    }

    // =========================================================
    // 인트로
    // =========================================================

    private Coroutine introMoveRoutine;

    protected override float ResolveIntroTime()
    {
        return 5f;
    }

    protected override void OnBeforeIntroStart()
    {
        base.OnBeforeIntroStart();

        // 초기 콜라이더 세팅 (굴러오는 상태이므로 캡슐 활성화)
        if (normalCollider != null) normalCollider.enabled = false;
        if (rollingCollider != null) rollingCollider.enabled = true;

        Vector3 targetCenter = transform.position;
        if (StageOwner != null && StageOwner is BossStage bossStage && bossStage.BossSpawnPoint != null)
        {
            targetCenter = bossStage.BossSpawnPoint.position;
        }

        // 왼쪽 화면 밖 멀리(-20f)에서 시작
        Vector3 startPos = targetCenter + new Vector3(-20f, 0f, 0f);
        transform.position = startPos;

        // 구르기 상태로 설정
        isRollingState = false;
        SetRollingAnim(true);

        // 5초의 인트로 시간 중 처음 1.5초 동안 아주 빠르게 굴러서 착지함
        if (introMoveRoutine != null) StopCoroutine(introMoveRoutine);
        introMoveRoutine = StartCoroutine(CoIntroMove(startPos, targetCenter, 1.5f));
    }

    private IEnumerator CoIntroMove(Vector3 start, Vector3 end, float moveDuration)
    {
        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            // 감속(EaseOut)을 주어 화면 밖에서 쏜살같이 들어와 부드럽게 멈춤
            float easeOutT = Mathf.Sin(t * Mathf.PI * 0.5f);
            transform.position = Vector3.Lerp(start, end, easeOutT);
            yield return null;
        }
        transform.position = end;
        
        // 제자리에서 1초 동안 추가로 구르기(회전) 상태를 유지합니다.
        yield return new WaitForSeconds(1f);
        
        // 구르기를 풀고 웅장하게 서 있는 모습(idle)으로 전환
        SetRollingAnim(false);
        introMoveRoutine = null;
    }

    public override void First()
    {
        if (introMoveRoutine != null)
        {
            StopCoroutine(introMoveRoutine);
            introMoveRoutine = null;
        }

        base.First();
        SetRollingAnim(false);
    }

    // =========================================================
    // BT 트리 생성
    // =========================================================

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

        // Phase 1 패턴 (공격 -> 2초 대기 -> 구르기 -> 2초 대기 순환)
        var phase1Patterns = new BossSequenceNode(
            // 1단계: 공격 (미사일 혹은 가시 토네이도 중 하나를 진입 시 무작위 선택)
            new BTTask_TurtleRandomAttack(this, so),
            // 2단계: 공격 후 2초 대기
            new BTTask_Wait(this, 2f),
            // 3단계: 구르기
            new BTTask_TurtleRolling(this, so, false),
            // 4단계: 구르기 후 2초 대기
            new BTTask_Wait(this, 2f)
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

    // =========================================================
    // 애니메이션
    // =========================================================

    private bool isRollingState = false;

    private void StopAllAnimationCoroutines()
    {
        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }
        if (rollingCoroutine != null)
        {
            StopCoroutine(rollingCoroutine);
            rollingCoroutine = null;
        }
    }

    public void SetRollingAnim(bool rolling)
    {
        // 동일 상태로 재호출 시 불필요한 코루틴 재시작 방지
        if (isRollingState == rolling) return;

        isRollingState = rolling;
        StopAllAnimationCoroutines();
        Debug.Log($"[TurtleBoss] SetRollingAnim: {rolling}, frame={Time.frameCount}");

        // 구르기 중에는 팔(bossAim)을 숨김
        if (bossAim != null)
        {
            bossAim.gameObject.SetActive(!rolling);
        }

        // 콜라이더 전환 (구르기 시 캡슐 활성화 / 사각 비활성화)
        if (rolling)
        {
            if (rollingCollider != null) rollingCollider.enabled = true;
            if (normalCollider != null) normalCollider.enabled = false;
        }
        else
        {
            // 구르기 -> 평상시로 돌아올 때 벽 끼임 보정
            AdjustPositionForWallOverlap();

            if (normalCollider != null) normalCollider.enabled = true;
            if (rollingCollider != null) rollingCollider.enabled = false;
        }

        Animator anim = GetComponent<Animator>();
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            anim.Play(rolling ? "roll" : "idle");
            return;
        }

        if (rolling)
            rollingCoroutine = StartCoroutine(CoRollingAnimation());
        else
            idleCoroutine = StartCoroutine(CoIdleAnimation());
    }

    // Update()에서 매 프레임 anim.Play()를 강제 호출하면
    // rolling→idle 전환이 늦어지거나 idle이 roll 도중에 재생되는 버그가 발생한다.
    // 상태 전환은 SetRollingAnim() 호출 시점에만 수행하도록 Update()를 제거.

    private void StartIdleAnimation()
    {
        SetRollingAnim(false);
    }

    private void StopIdleAnimation()
    {
        StopAllAnimationCoroutines();
    }

    private IEnumerator CoIdleAnimation()
    {
        if (turtleSO == null || turtleSO.idleSprites == null || turtleSO.idleSprites.Length == 0)
            yield break;

        float fps = Mathf.Max(1f, turtleSO.idleAnimFps);
        float delay = 1f / fps;
        int frameIndex = 0;

        // isRollingState가 true로 바뀌면 즉시 중단 (SetRollingAnim이 이 코루틴을 Stop하지만,
        // 같은 프레임에 중복 실행되는 엣지케이스를 방어하기 위해 플래그도 체크)
        while (live && !isDead && !isRollingState)
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

        // isRollingState가 false로 바뀌면 즉시 중단
        while (live && !isDead && isRollingState)
        {
            if (bodyRenderer != null)
                bodyRenderer.sprite = turtleSO.rollingSprites[frameIndex];

            frameIndex = (frameIndex + 1) % turtleSO.rollingSprites.Length;
            yield return new WaitForSeconds(delay);
        }
    }

    // =========================================================
    // 방향 (좌우 플립)
    // =========================================================

    private void UpdateFacingDirection()
    {
        if (Player == null) return;

        Vector3 scale = transform.localScale;
        bool playerOnRight = Player.transform.position.x > transform.position.x;
        scale.x = playerOnRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;

        if (bodyRenderer != null)
        {
            bodyRenderer.flipX = false;
        }
    }

    // =========================================================
    // 사망
    // =========================================================

    public override void BossDie()
    {
        if (isDead) return;

        StopIdleAnimation();
        base.BossDie();
        gameObject.SetActive(false);
    }

    private void AdjustPositionForWallOverlap()
    {
        if (normalCollider == null || rollingCollider == null) return;

        // 아래쪽 벽(Wall 레이어) 감지
        int wallMask = 1 << LayerMask.NameToLayer("Wall");
        
        // 보스 위치(피벗) 기준 발밑 방향으로 Raycast 수행
        float rayLength = 2.0f;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, rayLength, wallMask);
        
        if (hit.collider != null)
        {
            float normalBottom = 0f;
            float rollingBottom = 0f;

            if (normalCollider is BoxCollider2D box)
                normalBottom = box.offset.y - (box.size.y / 2f);
            else if (normalCollider is CircleCollider2D circle)
                normalBottom = circle.offset.y - circle.radius;

            if (rollingCollider is CapsuleCollider2D cap)
                rollingBottom = cap.offset.y - (cap.size.y / 2f);
            else if (rollingCollider is CircleCollider2D capCircle)
                rollingBottom = capCircle.offset.y - capCircle.radius;

            float diff = normalBottom - rollingBottom; // 예: -1.0f - (-0.5f) = -0.5f
            
            if (diff < 0f)
            {
                float distanceToNormalBottom = Mathf.Abs(normalBottom);
                // 발밑 벽까지의 거리(hit.distance)가 normalCollider의 로컬 바닥 깊이보다 작거나 겹칠 위기일 때 보정
                if (hit.distance < distanceToNormalBottom + 0.1f)
                {
                    float pushDistance = (distanceToNormalBottom + 0.1f) - hit.distance;
                    Vector3 pos = transform.position;
                    pos.y += pushDistance;
                    transform.position = pos;
                    Debug.Log($"[TurtleBoss] Wall overlap avoided! Pushed boss up by {pushDistance}f at frame {Time.frameCount}");
                }
            }
        }
    }

    protected override void OnDisable()
    {
        StopIdleAnimation();
        base.OnDisable();
    }
}

/// <summary>
/// 진입 시점에 미사일 공격과 가시 토네이도 공격 중 하나를 무작위로 한 번만 결정하여 실행하는 데코레이터성 Task.
/// 매 틱마다 확률을 검사하면 상태가 취소되는 문제가 발생하므로, OnEnter에서만 한 번 확률을 판정합니다.
/// </summary>
public class BTTask_TurtleRandomAttack : BTTask
{
    private readonly BTTask_TurtleMissile missileTask;
    private readonly BTTask_TurtleThornTornado thornTask;
    private BTTask chosenTask;

    public BTTask_TurtleRandomAttack(TurtleBoss boss, TurtleBossSO so) : base(boss)
    {
        missileTask = new BTTask_TurtleMissile(boss, so);
        thornTask = new BTTask_TurtleThornTornado(boss, so);
    }

    protected override void OnEnter()
    {
        chosenTask = (UnityEngine.Random.value < 0.5f) ? (BTTask)missileTask : (BTTask)thornTask;
        chosenTask.Reset();
    }

    protected override BossBTState OnTick()
    {
        if (chosenTask == null) return BossBTState.Failure;
        return chosenTask.Tick();
    }

    protected override void OnExit()
    {
        if (chosenTask != null)
        {
            chosenTask.Reset();
        }
        chosenTask = null;
    }
}
