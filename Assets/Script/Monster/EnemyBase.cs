using System.Collections;
using UnityEngine;

/// <summary>
/// 일반 몬스터 베이스 클래스.
/// - BossBase와 달리 BT/인트로 없이 단순 구조.
/// - 파생 클래스에서 AI(이동/공격 패턴)를 구현한다.
/// - NormalStage.NotifyNormalMonsterDied()를 통해 몬스터 게이트와 연동된다.
/// </summary>
public class EnemyBase : CreatureBase
{
    // ──────────────────── 접촉 데미지 게이트 ────────────────────
    [Header("Contact Damage Gate")]
    [SerializeField] private float contactHitInvincibilityDuration = 1.0f;
    [SerializeField] private float contactHitTickInterval = 1.0f;
    private readonly System.Collections.Generic.Dictionary<int, float> nextContactTickByAttacker
        = new System.Collections.Generic.Dictionary<int, float>();

    // ──────────────────── 스테이지 연결 ────────────────────
    /// <summary>이 몬스터가 속한 NormalStage. 사망 시 카운트 차감에 사용.</summary>
    [HideInInspector] public NormalStage ownerStage;

    // =========================================================
    // Unity 생명주기
    // =========================================================

    protected override void Awake()
    {
        base.Awake();
    }

    protected virtual void Start()
    {
        ResolvePlayer();
    }

    protected virtual void Update()
    {
        Tick();
    }

    // =========================================================
    // 초기화
    // =========================================================

    /// <summary>
    /// 스테이지가 이 몬스터를 활성화할 때 호출한다.
    /// EnemySO를 주입하고 스탯을 세팅한다.
    /// </summary>
    public virtual void StatSet(EnemySO so = null)
    {
        if (so != null)
            MainSO = so;

        if (MainSO == null)
        {
            Debug.LogError($"[EnemyBase] MainSO is null: {name}");
            return;
        }

        atk   = MainSO.atk;
        maxHp = MainSO.hp;
        curHp = MainSO.hp;
        speed = MainSO.speed;

        live = true;
        isDead = false;
        invincibility = false;

        nextContactTickByAttacker.Clear();

        ResolvePlayer();
        OnStatSetDone();
    }

    /// <summary>StatSet 완료 직후 파생 클래스 훅.</summary>
    protected virtual void OnStatSetDone() { }

    // =========================================================
    // AI 훅 (파생 클래스에서 override)
    // =========================================================

    /// <summary>
    /// Update마다 호출. 파생 클래스에서 이동/공격 AI를 구현한다.
    /// live=false / isDead=true 일 때는 호출되지 않는다.
    /// </summary>
    protected virtual void Tick()
    {
        if (!live || isDead) return;
        OnTick();
    }

    /// <summary>AI 구현 포인트. 파생 클래스에서 override.</summary>
    protected virtual void OnTick() { }

    // =========================================================
    // 사망 처리 (CreatureBase 추상 메서드 구현)
    // =========================================================

    protected override void OnCreatureDie()
    {
        Die();
    }

    /// <summary>사망 처리. 파생 클래스에서 override 후 반드시 base.Die() 호출.</summary>
    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        live = false;

        // 코인 드랍 (10% 확률)
        if (GameManager.Instance != null)
        {
            if (UnityEngine.Random.value <= 0.1f) // 10% 확률로 코인 드랍 시도
            {
                float coinRoll = UnityEngine.Random.value * 100f; // 0 ~ 100
                if (coinRoll <= 93f) // 93% 확률로 1원
                {
                    GameManager.Instance.SpawnCoinsForAmount(transform.position, 1);
                }
                else if (coinRoll <= 99f) // 6% 확률로 니켈 (5원)
                {
                    GameManager.Instance.SpawnCoinsForAmount(transform.position, 5);
                }
                // 나머지 1%는 0원이므로 드랍하지 않음
            }

            // 플레이어 킬 이벤트
            if (GameManager.Instance.playerOBJ != null)
            {
                PlayerStatControl stat = GameManager.Instance.playerOBJ
                    .GetComponent<PlayerStatControl>();
                stat?.KillEvent();
            }
        }

        // 스테이지 게이트 카운트 차감
        ownerStage?.NotifyNormalMonsterDied(1);

        OnDie();

        gameObject.SetActive(false);
    }

    /// <summary>사망 연출 훅 (SetActive 전). 파티클, 사운드 등 구현.</summary>
    protected virtual void OnDie() { }
}
