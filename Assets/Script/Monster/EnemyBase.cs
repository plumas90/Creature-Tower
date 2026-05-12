using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 일반 몬스터 베이스 클래스.
/// - BossBase와 달리 BT/인트로 없이 단순 구조.
/// - 파생 클래스에서 AI(이동/공격 패턴)를 구현한다.
/// - NormalStage.NotifyNormalMonsterDied()를 통해 몬스터 게이트와 연동된다.
/// </summary>
public class EnemyBase : MonoBehaviour
{
    // ──────────────────── 스탯 ────────────────────
    [HideInInspector] public EnemySO MainSO;

    public float atk;
    public float maxHp;
    public float curHp;
    public float speed;

    public bool live;
    public bool invincibility;

    [HideInInspector] public GameObject Player;

    // ──────────────────── 물리 ────────────────────
    [Header("Physics")]
    [SerializeField] private bool useKinematic = false;

    // ──────────────────── Y 소팅 ────────────────────
    [Header("Y Sorting")]
    [SerializeField] private bool useYBasedSorting = true;
    [SerializeField] private string ySortLayerName = "World_Dynamic";
    [SerializeField] private int ySortBaseOrder = 1000;
    [SerializeField] private int ySortScale = 10;
    [SerializeField] private Transform ySortPivot;

    // ──────────────────── 접촉 데미지 게이트 ────────────────────
    [Header("Contact Damage Gate")]
    [SerializeField] private float contactHitInvincibilityDuration = 1.0f;
    [SerializeField] private float contactHitTickInterval = 1.0f;
    private readonly System.Collections.Generic.Dictionary<int, float> nextContactTickByAttacker
        = new System.Collections.Generic.Dictionary<int, float>();

    // ──────────────────── 내부 캐시 ────────────────────
    protected Rigidbody2D _rb2d;
    private SortingGroup _sortingGroup;
    private SpriteRenderer[] _cachedRenderers;
    private bool isDead;

    // ──────────────────── 스테이지 연결 ────────────────────
    /// <summary>이 몬스터가 속한 NormalStage. 사망 시 카운트 차감에 사용.</summary>
    [HideInInspector] public NormalStage ownerStage;

    // =========================================================
    // Unity 생명주기
    // =========================================================

    protected virtual void Awake()
    {
        ConfigurePhysics();
        CacheSortingTargets();
    }

    protected virtual void Start()
    {
        ResolvePlayer();
    }

    protected virtual void Update()
    {
        Tick();
    }

    protected virtual void LateUpdate()
    {
        ApplyYBasedSorting();
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

    private void ResolvePlayer()
    {
        if (Player != null) return;

        if (GameManager.Instance != null && GameManager.Instance.playerOBJ != null)
        {
            Player = GameManager.Instance.playerOBJ;
            return;
        }

        GameObject found = GameObject.FindGameObjectWithTag("Player");
        if (found != null)
            Player = found;
    }

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
    // 피격 / 사망
    // =========================================================

    /// <summary>외부에서 데미지를 입힐 때 호출한다.</summary>
    public virtual void Damege(float damage)
    {
        if (isDead || !live || invincibility) return;

        float finalDamage = CalculateFinalDamage(damage);
        if (finalDamage <= 0f) return;

        curHp -= finalDamage;
        OnDamaged(finalDamage);

        if (curHp <= 0f)
            Die();
    }

    /// <summary>데미지 최종 계산 훅. 방어 배율 등 override 가능.</summary>
    protected virtual float CalculateFinalDamage(float incomingDamage) => incomingDamage;

    /// <summary>피격 직후 훅 (사망 판정 전). 피격 이펙트 등 구현.</summary>
    protected virtual void OnDamaged(float finalDamage) { }

    /// <summary>사망 처리. 파생 클래스에서 override 후 반드시 base.Die() 호출.</summary>
    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        live = false;

        // 코인 드랍 (5 ± 2원)
        if (GameManager.Instance != null)
        {
            int drop = UnityEngine.Random.Range(3, 8);
            GameManager.Instance.SpawnCoinsForAmount(transform.position, drop);

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

    // =========================================================
    // 접촉 데미지 (플레이어에게)
    // =========================================================

    public virtual void OnCollisionEnter2D(Collision2D collision)
    {
        TryContactDamageFromCollision(collision);
    }

    public virtual void OnCollisionStay2D(Collision2D collision)
    {
        TryContactDamageFromCollision(collision);
    }

    public virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (CanProcessContactDamage())
            TryContactDamageFromTrigger(collision);

        TryHandleBulletTrigger(collision);
    }

    public virtual void OnTriggerStay2D(Collider2D collision)
    {
        if (CanProcessContactDamage())
            TryContactDamageFromTrigger(collision);
    }

    private void TryContactDamageFromCollision(Collision2D collision)
    {
        if (!CanProcessContactDamage()) return;

        PlayerStatControl stat = ResolvePlayerStatFromCollision(collision);
        if (stat != null)
            stat.TryApplyContactDamage(atk, gameObject.GetInstanceID());
    }

    private void TryContactDamageFromTrigger(Collider2D collision)
    {
        PlayerStatControl stat = ResolvePlayerStatFromCollider(collision);
        if (stat != null)
            stat.TryApplyContactDamage(atk, gameObject.GetInstanceID());
    }

    // =========================================================
    // 총알 피격 처리
    // =========================================================

    private void TryHandleBulletTrigger(Collider2D collision)
    {
        Bullet bullet = collision != null ? collision.GetComponent<Bullet>() : null;
        if (bullet == null) return;

        // Enemy를 타겟으로 하는 총알만 처리
        if (bullet.targets == null || !bullet.targets.ContainsValue((int)BulletTarget.Enemy)) return;

        // 관통 탄이 아니면 다중 히트 방지
        if (!bullet.TryMarkBossHit(gameObject.GetInstanceID())) return;

        if (isDead || !live || invincibility)
        {
            if (!bullet.Penetrate) bullet.Destroy();
            return;
        }

        Damege(bullet.ATK);

        if (!bullet.Penetrate)
            bullet.Destroy();
    }

    // =========================================================
    // 헬퍼
    // =========================================================

    private bool CanProcessContactDamage()
        => !isDead && live && !invincibility && atk > 0f;

    private PlayerStatControl ResolvePlayerStatFromCollision(Collision2D collision)
    {
        if (collision == null) return null;
        if (collision.gameObject.TryGetComponent(out PlayerStatControl s)) return s;
        if (collision.collider != null)
        {
            s = collision.collider.GetComponentInParent<PlayerStatControl>();
            if (s != null) return s;
        }
        return null;
    }

    private PlayerStatControl ResolvePlayerStatFromCollider(Collider2D collision)
    {
        if (collision == null) return null;
        if (collision.TryGetComponent(out PlayerStatControl s)) return s;
        s = collision.attachedRigidbody != null
            ? collision.attachedRigidbody.GetComponentInParent<PlayerStatControl>()
            : null;
        if (s != null) return s;
        return collision.transform.GetComponentInParent<PlayerStatControl>();
    }

    // =========================================================
    // 물리 / 렌더링 초기화
    // =========================================================

    private void ConfigurePhysics()
    {
        _rb2d = GetComponent<Rigidbody2D>();
        if (_rb2d == null)
            _rb2d = gameObject.AddComponent<Rigidbody2D>();

        _rb2d.gravityScale = 0f;
        _rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb2d.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rb2d.linearVelocity = Vector2.zero;
        _rb2d.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (useKinematic)
        {
            _rb2d.bodyType = RigidbodyType2D.Kinematic;
            _rb2d.useFullKinematicContacts = true;
        }
    }

    private void CacheSortingTargets()
    {
        _sortingGroup = GetComponent<SortingGroup>();
        if (_sortingGroup == null)
            _sortingGroup = gameObject.AddComponent<SortingGroup>();

        if (!string.IsNullOrEmpty(ySortLayerName))
            _sortingGroup.sortingLayerName = ySortLayerName;

        _cachedRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void ApplyYBasedSorting()
    {
        if (!useYBasedSorting) return;

        Transform pivot = ySortPivot != null ? ySortPivot : transform;
        int order = ySortBaseOrder - Mathf.RoundToInt(pivot.position.y * ySortScale);

        if (_sortingGroup != null)
        {
            _sortingGroup.sortingOrder = order;
            return;
        }

        if (_cachedRenderers == null || _cachedRenderers.Length == 0)
            _cachedRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < _cachedRenderers.Length; i++)
        {
            if (_cachedRenderers[i] != null)
                _cachedRenderers[i].sortingOrder = order;
        }
    }

    protected Rigidbody2D Body2D => _rb2d;
}
