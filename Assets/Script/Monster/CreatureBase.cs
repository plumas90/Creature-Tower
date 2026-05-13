using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// BossBase와 EnemyBase의 공통 부모 클래스.
/// 총알 피격, 플레이어 접촉 데미지, Y소팅, 물리 초기화를 단일화한다.
/// 파생 클래스: BossBase (보스), EnemyBase (일반 몬스터)
/// </summary>
public abstract class CreatureBase : MonoBehaviour
{
    // ──────────────────── 공통 스탯 ────────────────────
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
    [SerializeField] protected bool useKinematic = false;

    // ──────────────────── Y 소팅 ────────────────────
    [Header("Y Sorting")]
    [SerializeField] private bool useYBasedSorting = true;
    [SerializeField] private string ySortLayerName = "World_Dynamic";
    [SerializeField] protected int ySortBaseOrder = 1000;
    [SerializeField] private int ySortScale = 10;
    [SerializeField] protected int ySortOrderOffset = 0;
    [SerializeField] private Transform ySortPivot;

    // ──────────────────── 내부 캐시 ────────────────────
    protected Rigidbody2D _rb2d;
    private SortingGroup _sortingGroup;
    private SpriteRenderer[] _cachedRenderers;
    protected bool isDead;

    // =========================================================
    // Unity 생명주기
    // =========================================================

    protected virtual void Awake()
    {
        ConfigurePhysics();
        CacheSortingTargets();
    }

    protected virtual void LateUpdate()
    {
        ApplyYBasedSorting();
    }

    // =========================================================
    // 물리 초기화
    // =========================================================

    protected void ConfigurePhysics()
    {
        _rb2d = GetComponent<Rigidbody2D>();
        if (_rb2d == null)
            _rb2d = gameObject.AddComponent<Rigidbody2D>();

        _rb2d.gravityScale = 0f;
        _rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb2d.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rb2d.linearVelocity = Vector2.zero;
        _rb2d.angularVelocity = 0f;
        _rb2d.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (useKinematic)
        {
            _rb2d.bodyType = RigidbodyType2D.Kinematic;
            _rb2d.useFullKinematicContacts = true;
        }
    }

    protected Rigidbody2D Body2D => _rb2d;

    // =========================================================
    // Y 소팅
    // =========================================================

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
        int order = ySortBaseOrder - Mathf.RoundToInt(pivot.position.y * ySortScale) + ySortOrderOffset;

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

    // =========================================================
    // 피격 공통
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
            OnCreatureDie();
    }

    /// <summary>데미지 최종 계산 훅. 방어 배율 등 override 가능.</summary>
    protected virtual float CalculateFinalDamage(float incomingDamage) => incomingDamage;

    /// <summary>피격 직후 훅 (사망 판정 전). 피격 이펙트 등 구현.</summary>
    protected virtual void OnDamaged(float finalDamage) { }

    /// <summary>
    /// HP가 0 이하가 됐을 때 호출된다.
    /// BossBase → BossDie(), EnemyBase → Die() 로 각자 구현한다.
    /// </summary>
    protected abstract void OnCreatureDie();

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
    // 총알 피격 처리 (공통)
    // =========================================================

    /// <summary>OnTriggerEnter2D에서 호출. 총알 컴포넌트 감지 → Damege 적용.</summary>
    protected void TryHandleBulletTrigger(Collider2D collision)
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

        float finalDamage = CalculateFinalDamage(bullet.ATK);
        if (finalDamage > 0f)
        {
            curHp -= finalDamage;
            OnDamaged(finalDamage);
            OnDamagedByBullet(bullet, finalDamage);

            if (curHp <= 0f)
                OnCreatureDie();
        }

        if (!bullet.Penetrate)
            bullet.Destroy();
    }

    /// <summary>총알 피격 훅 (파생 클래스에서 override 가능).</summary>
    protected virtual void OnDamagedByBullet(Bullet bullet, float finalDamage) { }

    /// <summary>외부 Hurtbox 컴포넌트에서 트리거 이벤트를 위임받아 처리한다.</summary>
    public virtual void HandleHurtboxTrigger(Collider2D collision)
    {
        TryHandleBulletTrigger(collision);
    }

    // =========================================================
    // 헬퍼
    // =========================================================

    protected bool CanProcessContactDamage()
        => !isDead && live && !invincibility && atk > 0f;

    protected void ResolvePlayer()
    {
        if (Player != null) return;

        if (GameManager.Instance != null && GameManager.Instance.playerOBJ != null)
        {
            Player = GameManager.Instance.playerOBJ;
            return;
        }

        // TestGameManager 폴백
        TestGameManager testManager = FindObjectOfType<TestGameManager>();
        if (testManager != null)
        {
            GameObject testPlayer = GameObject.FindGameObjectWithTag("Player");
            if (testPlayer != null)
            {
                Player = testPlayer;
                return;
            }
        }

        GameObject found = GameObject.FindGameObjectWithTag("Player");
        if (found != null)
            Player = found;
    }

    public string GetDisplayName()
    {
        if (MainSO != null && !string.IsNullOrEmpty(MainSO.enemyName))
            return MainSO.enemyName;
        return name;
    }

    // ──────────────────── Collision/Trigger 플레이어 탐색 ────────────────────

    private PlayerStatControl ResolvePlayerStatFromCollision(Collision2D collision)
    {
        if (collision == null) return null;
        if (collision.gameObject.TryGetComponent(out PlayerStatControl s)) return s;
        if (collision.collider != null)
        {
            s = collision.collider.GetComponentInParent<PlayerStatControl>();
            if (s != null) return s;
        }
        if (collision.rigidbody != null)
        {
            s = collision.rigidbody.GetComponentInParent<PlayerStatControl>();
            if (s != null) return s;
        }
        if (collision.transform != null)
        {
            s = collision.transform.GetComponentInParent<PlayerStatControl>();
            if (s != null) return s;
        }
        return null;
    }

    private PlayerStatControl ResolvePlayerStatFromCollider(Collider2D collision)
    {
        if (collision == null) return null;
        if (collision.TryGetComponent(out PlayerStatControl s)) return s;
        if (collision.attachedRigidbody != null)
        {
            s = collision.attachedRigidbody.GetComponentInParent<PlayerStatControl>();
            if (s != null) return s;
        }
        return collision.transform.GetComponentInParent<PlayerStatControl>();
    }
}
