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

        if (GameManager.Instance != null)
            Player = GameManager.Instance.playerOBJ;

        OnBeforeIntroStart();

        //�Ʒ� �ð� ��� ���� ���� ���� ���� ���� ���� �ִϸ��̼��� �ִٸ� �׷����� �ƴϸ� 1�ʰ���
        // �׷��� �ȴٸ� 1�ʸ� ���׹� ���̽��� ���� ��ŸƮ ��� ������ �����ɷ� �����߰���
        StartInvincibilityNSecond(battleStartDelay);
        WaitPls(battleStartDelay);
        FirstPls(battleStartDelay);

        behaviorTreeRoot = CreateBehaviorTree();

        UIBossHP.NotifyBossEngaged(this);
    }

    // 보스 오브젝트가 활성화된 직후(이름 표시 전)에 호출되는 훅.
    // 파생 보스에서 기본 애니메이션 잠금 등 사전 준비에 사용한다.
    public virtual void OnBossActivatedBeforeIntro()
    {
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
        if (!live || wait || !brainRunning)
            return;

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
        _rb2d = GetComponent<Rigidbody2D>();
        if (_rb2d == null || !forceKinematicBody2D)
            return;

        // 보스가 플레이어 충돌에 밀려나지 않도록 2D 물리를 키네마틱 기반으로 고정한다.
        _rb2d.bodyType = RigidbodyType2D.Kinematic;
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
        CancelInvoke(nameof(InvokeFirstOnce));
        Invoke(nameof(InvokeFirstOnce), second);
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
        if (isDead || !live || invincibility) return;
        if (bullet.targets == null || !bullet.targets.ContainsValue((int)BulletTarget.Enemy)) return;

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
        CancelInvoke(nameof(EndInvincibility));
        Invoke(nameof(EndInvincibility), second);
    }

    public void EndInvincibility()
    {
        invincibility = false;
    }

    // 레거시 호환 (이전 Invoke 문자열/외부 호출 대응)
    public void endinvincibility()
    {
        EndInvincibility();
    }

    public void WaitPls(float second)
    {
        wait = true;
        CancelInvoke(nameof(WaitStop));
        Invoke(nameof(WaitStop), second);
    }

    public void WaitStop()
    {
        wait = false;
    }

    // 레거시 호환
    public void WaitStop(float second)
    {
        WaitStop();
    }

    protected virtual void OnDisable()
    {
        CancelInvoke();
        StopBrain();
        behaviorTreeRoot = null;
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
