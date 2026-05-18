using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BossBase : CreatureBase
{
    //[HideInInspector] 
    public int bossCount;
    //[HideInInspector] 
    public bool wait = true;
    public Stage StageOwner;
    public float IntroTime;

    // ──────────────────── 물리 (보스 전용 옵션) ────────────────────
    [Header("Boss Physics")]
    [SerializeField] private bool forceKinematicBody2D = true;

    // ──────────────────── Hurtbox 분리 시스템 ────────────────────
    [Header("Collision Split (Optional)")]
    [SerializeField] private bool useSeparateHurtbox = false;
    private bool hasChildHurtbox;

    // ──────────────────── BT ────────────────────
    protected BossBTNode behaviorTreeRoot;
    public BossBTBlackboard blackboard;  // BTTask가 접근할 수 있도록 public
    protected bool brainRunning;

    [Header("BT Debug")]
    [SerializeField] private bool enableBTChecklistLog;

    private int firstCallCount;
    private int brainStartCount;

    // ──────────────────── 타이밍 코루틴 ────────────────────
    private Coroutine invincibilityRoutine;
    private Coroutine waitRoutine;
    private Coroutine firstRoutine;

    // =========================================================
    // StatSet (보스 초기화)
    // =========================================================

    public virtual void StatSet()
    {
        if (MainSO == null)
        {
            Debug.LogError($"[BossBase] MainSO is null: {name}");
            return;
        }

        atk = MainSO.atk;
        maxHp = MainSO.hp;
        curHp = MainSO.hp;
        speed = MainSO.speed;
        IntroTime = ResolveIntroTime();
        float postIntroDelay = Mathf.Max(0f, ResolvePostIntroDelay());
        float battleStartDelay = IntroTime + postIntroDelay;
        live = true;
        isDead = false;
        brainRunning = false;
        firstCallCount = 0;
        brainStartCount = 0;
        blackboard = new BossBTBlackboard();
        StopTimingCoroutines();

        ResolvePlayer();

        OnBeforeIntroStart();

        StartInvincibilityNSecond(battleStartDelay);
        WaitPls(battleStartDelay);
        FirstPls(battleStartDelay);

        behaviorTreeRoot = CreateBehaviorTree();
    }

    // ──────────────────── 훅 ────────────────────

    public virtual void OnBossActivatedBeforeIntro()
    {
        invincibility = true;
        wait = true;
    }

    protected virtual void OnBeforeIntroStart() { }

    protected virtual BossBTNode CreateBehaviorTree() => null;

    protected virtual float ResolveIntroTime()
        => MainSO != null ? Mathf.Max(0f, MainSO.IntroAnimationTime) : 0f;

    protected virtual float ResolvePostIntroDelay() => 0f;

    // =========================================================
    // Unity 생명주기
    // =========================================================

    protected override void Awake()
    {
        // 보스 전용 물리 처리 후 CreatureBase.Awake 상당 부분을 수동 호출
        ConfigureBossPhysics();
        // SortingGroup 캐시는 CreatureBase에서 처리하도록 base.Awake() 호출
        base.Awake();
        hasChildHurtbox = HasAnyChildHurtbox();
    }

    private void ConfigureBossPhysics()
    {
        // CreatureBase.ConfigurePhysics()가 Awake에서 호출되기 전에
        // 보스 전용 키네마틱 강제가 필요하므로 별도로 처리한다.
        // 실제 Rigidbody2D 설정은 base.Awake() → ConfigurePhysics()에서 수행.
        // 여기서는 useKinematic 플래그만 보스 설정에 맞춰 세팅한다.
        if (forceKinematicBody2D)
            useKinematic = true;
    }

    protected override void LateUpdate()
    {
        base.LateUpdate(); // Y소팅
    }

    // =========================================================
    // 피격 오버라이드 (보스 전용: HP UI 연동)
    // =========================================================

    public override void Damege(float damage)
    {
        if (isDead || !live || invincibility) return;

        float finalDamage = CalculateFinalDamage(damage);
        if (finalDamage <= 0f) return;

        curHp -= finalDamage;
        UIBossHP.NotifyBossDamaged(this);

        if (curHp <= 0f)
            OnCreatureDie();
    }

    protected override void OnDamagedByBullet(Bullet bullet, float finalDamage)
    {
        UIBossHP.NotifyBossDamaged(this);
    }

    // =========================================================
    // 사망 (OnCreatureDie → BossDie)
    // =========================================================

    protected override void OnCreatureDie()
    {
        BossDie();
    }

    public virtual void BossDie()
    {
        if (isDead) return;
        isDead = true;
        live = false;
        StopBrain();
        behaviorTreeRoot = null;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.BossCountMinus(bossCount);
        }

        if (StageOwner != null)
            StageOwner.NotifyBossDied(this, bossCount);

        UIBossHP.NotifyBossDied(this);

        CancelInvoke();
        Destroy(this);
    }

    // =========================================================
    // 충돌 (Hurtbox 분리 시스템 오버라이드)
    // =========================================================

    public override void OnTriggerEnter2D(Collider2D collision)
    {
        if (UseHurtboxOnlyDamage())
        {
            // hurtbox 전용 모드: 접촉 데미지는 hurtbox에서만, 총알도 hurtbox에서만
            return;
        }

        base.OnTriggerEnter2D(collision);
    }

    public override void OnTriggerStay2D(Collider2D collision)
    {
        if (UseHurtboxOnlyDamage())
            return;

        base.OnTriggerStay2D(collision);
    }

    /// <summary>BossHurtbox에서 트리거 이벤트를 위임받아 처리한다.</summary>
    public override void HandleHurtboxTrigger(Collider2D collision)
    {
        if (!UseHurtboxOnlyDamage())
            return;

        TryHandleBulletTrigger(collision);
    }

    private bool UseHurtboxOnlyDamage()
        => useSeparateHurtbox || hasChildHurtbox;

    private bool HasAnyChildHurtbox()
    {
        BossHurtbox[] hurtboxes = GetComponentsInChildren<BossHurtbox>(true);
        for (int i = 0; i < hurtboxes.Length; i++)
        {
            BossHurtbox hb = hurtboxes[i];
            if (hb == null) continue;
            if (hb.transform == transform) continue;
            return true;
        }
        return false;
    }

    // =========================================================
    // BT
    // =========================================================

    protected void TickBehaviorTree()
    {
        if (!live || wait)
            return;

        if (!brainRunning)
        {
            if (enableBTChecklistLog)
                Debug.LogWarning($"[BossBTChecklist] brain auto-start fallback: {name}");
            StartBrain();
        }

        if (behaviorTreeRoot != null)
            behaviorTreeRoot.Tick();
    }

    protected virtual void StartBrain()
    {
        if (brainRunning) return;
        brainRunning = true;
        brainStartCount++;

        if (enableBTChecklistLog)
            Debug.Log($"[BossBTChecklist] Brain started: {name} (count={brainStartCount})");
    }

    protected virtual void StopBrain()
    {
        if (!brainRunning) return;
        brainRunning = false;

        if (enableBTChecklistLog)
            Debug.Log($"[BossBTChecklist] Brain stopped: {name}");
    }

    // =========================================================
    // First / Wait / Invincibility 타이밍
    // =========================================================

    public virtual void First() { }

    public void FirstPls(float second)
    {
        if (firstRoutine != null)
            StopCoroutine(firstRoutine);
        firstRoutine = StartCoroutine(CoInvokeFirstOnceAfter(second));
    }

    private IEnumerator CoInvokeFirstOnceAfter(float second)
    {
        if (second > 0f)
            yield return new WaitForSeconds(second);
        firstRoutine = null;
        InvokeFirstOnce();
    }

    private void InvokeFirstOnce()
    {
        if (!live || isDead) return;

        if (firstCallCount > 0)
        {
            if (enableBTChecklistLog)
                Debug.LogWarning($"[BossBTChecklist] Duplicate First blocked: {name}");
            return;
        }

        firstCallCount++;
        UIBossHP.NotifyBossEngaged(this);

        if (enableBTChecklistLog)
            Debug.Log($"[BossBTChecklist] First gate opened: {name} | wait={wait} inv={invincibility} live={live}");

        StartBrain();
        First();

        if (enableBTChecklistLog)
            Debug.Log($"[BossBTChecklist] First invoked once: {name}");
    }

    public void StartInvincibilityNSecond(float second)
    {
        invincibility = true;
        if (invincibilityRoutine != null)
            StopCoroutine(invincibilityRoutine);
        invincibilityRoutine = StartCoroutine(CoEndInvincibilityAfter(second));
    }

    private IEnumerator CoEndInvincibilityAfter(float second)
    {
        if (second > 0f)
            yield return new WaitForSeconds(second);
        invincibilityRoutine = null;
        EndInvincibility();
    }

    public void EndInvincibility()
    {
        invincibility = false;
        if (enableBTChecklistLog)
            Debug.Log($"[BossBTChecklist] EndInvincibility: {name}");
    }

    // 레거시 호환
    public void endinvincibility() => EndInvincibility();

    public void WaitPls(float second)
    {
        wait = true;
        if (waitRoutine != null)
            StopCoroutine(waitRoutine);
        waitRoutine = StartCoroutine(CoWaitStopAfter(second));
    }

    private IEnumerator CoWaitStopAfter(float second)
    {
        if (second > 0f)
            yield return new WaitForSeconds(second);
        waitRoutine = null;
        WaitStop();
    }

    public void WaitStop()
    {
        wait = false;
        if (enableBTChecklistLog)
            Debug.Log($"[BossBTChecklist] WaitStop: {name}");

        if (firstCallCount == 0 && live && !isDead)
        {
            if (enableBTChecklistLog)
                Debug.LogWarning($"[BossBTChecklist] First auto-invoke fallback: {name}");
            InvokeFirstOnce();
        }
    }

    // 레거시 호환
    public void WaitStop(float second) => WaitStop();

    // =========================================================
    // 비활성화
    // =========================================================

    protected virtual void OnDisable()
    {
        StopTimingCoroutines();
        StopBrain();
        behaviorTreeRoot = null;
    }

    private void StopTimingCoroutines()
    {
        if (invincibilityRoutine != null)
        {
            StopCoroutine(invincibilityRoutine);
            invincibilityRoutine = null;
        }
        if (waitRoutine != null)
        {
            StopCoroutine(waitRoutine);
            waitRoutine = null;
        }
        if (firstRoutine != null)
        {
            StopCoroutine(firstRoutine);
            firstRoutine = null;
        }
    }

    // =========================================================
    // BT 디버그
    // =========================================================

    [ContextMenu("BT/Print Checklist Report")]
    private void PrintBTChecklistReport()
    {
        Debug.Log(BuildBTChecklistReport());
    }

    public string BuildBTChecklistReport()
    {
        bool introGateReady = wait || !brainRunning;
        bool firstOnce = firstCallCount <= 1;
        bool brainStartOnce = brainStartCount <= 1;

        return $"[BossBTChecklist] {name}\n" +
               $"- intro gate safe(wait || !brainRunning): {introGateReady}\n" +
               $"- first invoked <= 1: {firstOnce} (count={firstCallCount})\n" +
               $"- brain started <= 1: {brainStartOnce} (count={brainStartCount})\n" +
               $"- live={live}, wait={wait}, invincibility={invincibility}, brainRunning={brainRunning}";
    }
}
