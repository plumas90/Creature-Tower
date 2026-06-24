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
    // 접촉 데미지 무적/쿨타임은 PlayerStatControl.TryApplyContactDamage() 에서 일원화 관리

    // ──────────────────────────────────────────────────────────
    /// <summary>이 몬스터가 속한 NormalStage. 사망 시 카운트 차감에 사용.</summary>
    [HideInInspector] public NormalStage ownerStage;

    // ──────────────────── Context Steering ────────────────────
    [Header("Context Steering (군집 경로 회피)")]
    [Tooltip("체크 시 주변 몬스터/벽을 감지해 자연스럽게 돌아서 접근합니다. 덕덕거림 없음.")]
    [SerializeField] protected bool useContextSteering = true;

    [Tooltip("방향 후보 수. 8이면 45도 간격, 16이면 22.5도 간격.")]
    [SerializeField] protected int steerRayCount = 8;

    [Tooltip("장애물 감지 거리. 이 반경 안에 다른 몬스터나 벽이 있으면 피해서 접근.")]
    [SerializeField] protected float steerRayLength = 1.5f;

    [Tooltip("방향 전환 부드러움. 클수록 빠르게 방향 변경.")]
    [SerializeField] protected float steerSmoothSpeed = 10f;

    [Tooltip("벽/지형 레이어 마스크. Wall, Ground 레이어를 설정하면 벽도 피합니다. 없이도 몬스터 간 회피는 동작.")]
    [SerializeField] protected LayerMask steerObstacleMask;

    // 이전 프레임 스티어링 방향 캐시 (부드러운 전환용)
    private Vector2 _steerDir;
    private bool _steerDirInit;

    // OverlapCircle 결과 버퍼 (static: 메인 스레드 직렬 실행 보장되므로 공유 안전)
    private static readonly Collider2D[] _steerBuffer = new Collider2D[8];


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

        // 프리팹이나 인스펙터에 MainSO가 할당되어 있다면 자동 초기화
        if (MainSO != null)
        {
            StatSet();
        }
        else
        {
            // SO가 없어도 일단 죽을 수는 있도록 임시 상태 부여 (문 열림 테스트용)
            isDead = false;
            StartCoroutine(SpawnDelayRoutine());
        }
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

        // 임시로 무적 및 이동 불가 상태
        live = false;
        isDead = false;
        invincibility = true;

        ResolvePlayer();
        OnStatSetDone();

        // 생성 연출: 1초 대기 후 활성화
        StartCoroutine(SpawnDelayRoutine());
    }

    private IEnumerator SpawnDelayRoutine()
    {
        // 소환 대기 동안 Collider2D 일시 비활성화하여 플레이어가 밀거나 끼이는 현상 방지
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        foreach (var c in colliders)
        {
            if (c != null) c.enabled = false;
        }

        // Rigidbody2D 속도 강제 리셋
        if (_rb2d != null)
        {
            _rb2d.linearVelocity = Vector2.zero;
            _rb2d.angularVelocity = 0f;
        }

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in renderers)
        {
            if (r != null) r.color = new Color(r.color.r, r.color.g, r.color.b, 0.5f);
        }

        yield return new WaitForSeconds(1.0f);

        if (isDead) yield break;

        live = true;
        invincibility = false;

        // 소환 완료 후 Collider2D 원복 활성화
        foreach (var c in colliders)
        {
            if (c != null) c.enabled = true;
        }

        foreach (var r in renderers)
        {
            if (r != null) r.color = new Color(r.color.r, r.color.g, r.color.b, 1f);
        }
    }

    /// <summary>StatSet 완료 직후 파생 클래스 훅.</summary>
    protected virtual void OnStatSetDone() { }

    // =========================================================
    // Context Steering
    // =========================================================

    /// <summary>
    /// Context Steering: desiredDir 방향으로 가고 싶지만
    /// 주변 몬스터나 벽이 있으면 자연스럽게 돌아서 접근한다.
    /// 힘을 합산하지 않고 가장 좋은 방향 1개를 선택 → 덕덕거림 없음.
    /// </summary>
    protected Vector2 ComputeContextSteering(Vector2 desiredDir)
    {
        if (!useContextSteering || steerRayCount < 2) return desiredDir;

        // 스티어 방향 초기화
        if (!_steerDirInit)
        {
            _steerDir = desiredDir;
            _steerDirInit = true;
        }

        float angleStep = 360f / steerRayCount;
        float bestScore = float.MinValue;
        Vector2 bestDir = desiredDir;

        for (int i = 0; i < steerRayCount; i++)
        {
            float rad = i * angleStep * Mathf.Deg2Rad;
            Vector2 rayDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            // ① Interest: 목표 방향과 얼마나 일치하는가 (-1 ~ 1)
            float interest = Vector2.Dot(rayDir, desiredDir);

            // 완전 반대 방향(-0.2 미만)은 후보 제외
            if (interest < -0.2f) continue;

            // ② Danger: 이 방향에 벽이 있는가
            float danger = 0f;
            if (steerObstacleMask != 0)
            {
                RaycastHit2D wallHit = Physics2D.Raycast(
                    (Vector2)transform.position, rayDir, steerRayLength, steerObstacleMask);
                if (wallHit.collider != null)
                    danger = Mathf.Max(danger, 1f - wallHit.distance / steerRayLength);
            }

            // ③ Danger: 이 방향에 다른 몬스터가 있는가
            // (steerRayLength * 0.6f 앞 지점에 반경 0.45f 원 검사)
            Vector2 probePos = (Vector2)transform.position + rayDir * steerRayLength * 0.6f;
            int hitCount = Physics2D.OverlapCircleNonAlloc(probePos, 0.45f, _steerBuffer);
            for (int j = 0; j < hitCount; j++)
            {
                Collider2D col = _steerBuffer[j];
                if (col == null || col.gameObject == gameObject) continue;
                EnemyBase other = col.GetComponentInParent<EnemyBase>();
                if (other != null && !other.isDead)
                {
                    danger = Mathf.Max(danger, 0.9f);
                    break;
                }
            }

            // 점수 = 관심 - 위험
            float score = interest - danger;
            if (score > bestScore)
            {
                bestScore = score;
                bestDir = rayDir;
            }
        }

        // 방향을 부드럽게 Lerp → 덕덕거림 근절
        _steerDir = Vector2.Lerp(_steerDir, bestDir, Time.deltaTime * steerSmoothSpeed).normalized;
        return _steerDir;
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

        Debug.Log($"[EnemyBase] Die() 실행됨: name={name}, ownerStage={(ownerStage != null ? ownerStage.name : "null")}");

        // 코인 드랍 (10% 확률)
        if (UnityEngine.Random.value <= 0.1f) // 10% 확률로 코인 드랍 시도
        {
            float coinRoll = UnityEngine.Random.value * 100f; // 0 ~ 100
            int amount = 1;
            if (coinRoll <= 93f) // 93% 확률로 1원
            {
                amount = 1;
            }
            else if (coinRoll <= 99f) // 6% 확률로 니켈 (5원)
            {
                amount = 5;
            }
            else // 1% 확률로 10원
            {
                amount = 10;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SpawnCoinsForAmount(transform.position, amount);
            }
            else if (TestGameManager.Instance != null)
            {
                TestGameManager.Instance.SpawnCoinsForAmount(transform.position, amount);
            }
        }

        // 플레이어 킬 이벤트
        GameObject player = null;
        if (GameManager.Instance != null && GameManager.Instance.playerOBJ != null)
        {
            player = GameManager.Instance.playerOBJ;
        }
        else
        {
            player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                player = Object.FindFirstObjectByType<PlayerStatControl>()?.gameObject;
            }
        }

        if (player != null)
        {
            PlayerStatControl stat = player.GetComponent<PlayerStatControl>();
            stat?.KillEvent();
        }

        // 스테이지 게이트 카운트 차감
        ownerStage?.NotifyNormalMonsterDied(1);

        OnDie();

        gameObject.SetActive(false);
    }

    /// <summary>사망 연출 훅 (SetActive 전). 파티클, 사운드 등 구현.</summary>
    protected virtual void OnDie() { }
}
