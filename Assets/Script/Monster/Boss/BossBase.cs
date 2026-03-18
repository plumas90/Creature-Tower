using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossBase : MonoBehaviour
{
    public EnemySO MainSO;
    //[HideInInspector] 
    public float atk;             // ���ݷ�
    //[HideInInspector] 
    public float maxHp;              // ü��
    //[HideInInspector] 
    public float curHp;
    //[HideInInspector] 
    public float speed;
    //[HideInInspector] 
    public int bossCount;
    //[HideInInspector] 
    public bool live;
    //[HideInInspector] 
    public GameObject Player;
    public Stage StageOwner;
    public float IntroTime;

    public bool wait =true;
    public bool invincibility;

    [Header("Physics")]
    [SerializeField] private bool forceKinematicBody2D = true;

    protected bool isDead;
    protected BossBTNode behaviorTreeRoot;
    protected bool brainRunning;

    [Header("BT Debug")]
    [SerializeField] private bool enableBTChecklistLog;

    private int firstCallCount;
    private int brainStartCount;

    private Rigidbody2D _rb2d;
    private Coroutine invincibilityRoutine;
    private Coroutine waitRoutine;
    private Coroutine firstRoutine;

    // Start is called before the first frame update
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
        StopTimingCoroutines();

        if (GameManager.Instance != null)
            Player = GameManager.Instance.playerOBJ;

        OnBeforeIntroStart();

        //�Ʒ� �ð� ��� ���� ���� ���� ���� ���� ���� �ִϸ��̼��� �ִٸ� �׷����� �ƴϸ� 1�ʰ���
        // �׷��� �ȴٸ� 1�ʸ� ���׹� ���̽��� ���� ��ŸƮ ��� ������ �����ɷ� �����߰���
        StartInvincibilityNSecond(battleStartDelay);
        WaitPls(battleStartDelay);
        FirstPls(battleStartDelay);

        behaviorTreeRoot = CreateBehaviorTree();
    }

    // 보스 오브젝트가 활성화된 직후(이름 표시 전)에 호출되는 훅.
    // 파생 보스에서 기본 애니메이션 잠금 등 사전 준비에 사용한다.
    public virtual void OnBossActivatedBeforeIntro()
    {
        // 이름 표시/인트로 전 구간은 무적 + 행동 정지 상태를 기본값으로 강제한다.
        invincibility = true;
        wait = true;
    }

    // StatSet 시작 시점에 호출되는 훅.
    // 파생 보스에서 인트로 시작 직전 상태 복구에 사용한다.
    protected virtual void OnBeforeIntroStart()
    {
    }

    protected virtual BossBTNode CreateBehaviorTree()
    {
        return null;
    }

    protected virtual float ResolveIntroTime()
    {
        return MainSO != null ? Mathf.Max(0f, MainSO.IntroAnimationTime) : 0f;
    }

    protected virtual float ResolvePostIntroDelay()
    {
        return 0f;
    }

    protected void TickBehaviorTree()
    {
        if (!live || wait)
            return;

        // 타이밍 훅 누락으로 brainRunning이 false로 남는 경우를 방어한다.
        if (!brainRunning)
        {
            if (enableBTChecklistLog)
                Debug.LogWarning($"[BossBTChecklist] brain auto-start fallback: {name}");
            StartBrain();
        }

        if (behaviorTreeRoot != null)
        {
            behaviorTreeRoot.Tick();
        }
    }

    protected virtual void Awake()
    {
        ConfigureBossPhysics();
    }

    protected void ConfigureBossPhysics()
    {
        if (!forceKinematicBody2D)
            return;

        _rb2d = GetComponent<Rigidbody2D>();
        if (_rb2d == null)
            _rb2d = gameObject.AddComponent<Rigidbody2D>();

        // 보스가 플레이어 충돌에 밀려나지 않도록 2D 물리를 키네마틱 기반으로 고정한다.
        _rb2d.bodyType = RigidbodyType2D.Kinematic;
        _rb2d.simulated = true;
        _rb2d.gravityScale = 0f;
        _rb2d.useFullKinematicContacts = true;
        _rb2d.linearVelocity = Vector2.zero;
        _rb2d.angularVelocity = 0f;
        _rb2d.constraints = _rb2d.constraints | RigidbodyConstraints2D.FreezeRotation;
    }

    public virtual void Damege(float damege) 
    {
        if (isDead || !live || invincibility) return;

        float finalDamage = CalculateFinalDamage(damege);
        if (finalDamage <= 0f) return;

        curHp -= finalDamage;
        UIBossHP.NotifyBossDamaged(this);
        if (curHp <= 0)
            BossDie();
    }

    // 파생 보스에서 고정 1데미지/상한/하한 등 정책을 override하기 위한 훅
    protected virtual float CalculateFinalDamage(float incomingDamage)
    {
        return incomingDamage;
    }

    protected virtual void OnDamagedByBullet(Bullet bullet, float finalDamage)
    {
    }

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
        if (!live || isDead)
            return;

        if (firstCallCount > 0)
        {
            if (enableBTChecklistLog)
                Debug.LogWarning($"[BossBTChecklist] Duplicate First blocked: {name}");
            return;
        }

        firstCallCount++;
        // 인트로(및 후딜) 종료 시점에 보스 HP UI를 노출한다.
        UIBossHP.NotifyBossEngaged(this);

        if (enableBTChecklistLog)
            Debug.Log($"[BossBTChecklist] First gate opened: {name} | wait={wait} inv={invincibility} live={live}");

        StartBrain();
        First();

        if (enableBTChecklistLog)
            Debug.Log($"[BossBTChecklist] First invoked once: {name}");
    }

    public virtual void First() 
    { 

    }

    protected virtual void StartBrain()
    {
        if (brainRunning)
            return;

        brainRunning = true;
        brainStartCount++;

        if (enableBTChecklistLog)
            Debug.Log($"[BossBTChecklist] Brain started: {name} (count={brainStartCount})");
    }

    protected virtual void StopBrain()
    {
        if (!brainRunning)
            return;

        brainRunning = false;

        if (enableBTChecklistLog)
            Debug.Log($"[BossBTChecklist] Brain stopped: {name}");
    }

    public virtual void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerStatControl playerStat = collision.gameObject.GetComponent<PlayerStatControl>();
        if (playerStat) 
        {
            playerStat.Damage(atk);
        }
    }

    public virtual void OnTriggerEnter2D(Collider2D collision)
    {
        Bullet bullet = collision.GetComponent<Bullet>();
        if (bullet == null) return;
        if (bullet.targets == null || !bullet.targets.ContainsValue((int)BulletTarget.Enemy)) return;

        // 인트로/비활성/사망 구간엔 데미지를 무시하되,
        // 비관통 탄은 즉시 소모해서 인트로 종료 직후 누적 히트가 터지지 않게 한다.
        if (isDead || !live || invincibility)
        {
            if (!bullet.Penetrate)
                bullet.Destroy();
            return;
        }

        float finalDamage = CalculateFinalDamage(bullet.ATK);
        if (finalDamage > 0f)
        {
            curHp -= finalDamage;
            UIBossHP.NotifyBossDamaged(this);
            OnDamagedByBullet(bullet, finalDamage);

            if (curHp <= 0f)
                BossDie();
        }

        if (!bullet.Penetrate)
            bullet.Destroy();
    }
    public virtual void BossDie() 
    {
        if (isDead) return;
        isDead = true;
        live = false;
        StopBrain();
        behaviorTreeRoot = null;

        if (GameManager.Instance != null)
            GameManager.Instance.BossCountMinus(bossCount);

        if (StageOwner != null)
            StageOwner.NotifyBossDied(this, bossCount);

        UIBossHP.NotifyBossDied(this);

        // 보스 인스턴스의 예약 호출 정리
        CancelInvoke();
        Destroy(this);
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

    // 레거시 호환 (이전 Invoke 문자열/외부 호출 대응)
    public void endinvincibility()
    {
        EndInvincibility();
    }

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

        // 어떤 이유로 First 예약이 누락되더라도 전투 시작이 막히지 않도록 보정한다.
        if (firstCallCount == 0 && live && !isDead)
        {
            if (enableBTChecklistLog)
                Debug.LogWarning($"[BossBTChecklist] First auto-invoke fallback: {name}");
            InvokeFirstOnce();
        }
    }

    // 레거시 호환
    public void WaitStop(float second)
    {
        WaitStop();
    }

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

    public string GetDisplayName()
    {
        if (MainSO != null && !string.IsNullOrEmpty(MainSO.enemyName))
            return MainSO.enemyName;

        return name;
    }
}
