using System.Collections;
using UnityEngine;

/// <summary>
/// 돌진형 근접 일반 몬스터.
///
/// 행동 사이클:
///   [Chase] 플레이어를 향해 이동
///   → 사거리(dashRange) 진입
///   → [Windup] N초 동안 정지하며 플레이어 방향을 실시간 추적 + 빨간 예고선 표시
///   → [Dashing] 저장된 방향으로 고속 직진. 플레이어 or 벽에 충돌하면 즉시 정지
///   → [Cooldown] 기절 대기 후 Chase 재개
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class DashEnemy : EnemyBase
{
    // ─── 상태 정의 ────────────────────────────────────────────
    protected enum DashState { Chase, Windup, Dashing, Cooldown }

    // ─── 인스펙터 참조 ────────────────────────────────────────
    [Header("시각적 연출 설정")]
    [Tooltip("Windup 중 표시되는 빨간 방향 예고 실선 오브젝트")]
    public GameObject windupWarningVisual;

    [Tooltip("위협선 Y축 시작 높이 보정값 (로컬 좌표계 Y 오프셋)")]
    [SerializeField] private float warningLineYOffset = 0f;

    // ─── 내부 캐시 ────────────────────────────────────────────
    private DashEnemySO   _dashSO;
    private Animator      _animator;
    private SpriteRenderer _sr;
    private static readonly int AnimIsWalk = Animator.StringToHash("IsWalking");

    // ─── 상태 제어 ────────────────────────────────────────────
    protected DashState _state = DashState.Chase;
    private bool      _routineRunning = false;

    // 돌진 방향 (Windup 종료 직전에 최종 확정됨)
    private Vector2 _dashDir = Vector2.right;

    // 돌진 시작 위치 (최대 거리 판정용)
    private Vector3 _dashStartPos;

    // ─── 스프라이트 예고선 스케일 기준 ────────────────────────
    private float _warningBaseScaleX = 1f; // 예고선 오브젝트 원본 X 스케일
    private float _warningBaseScaleY = 1f; // 예고선 오브젝트 원본 Y 스케일

    // =========================================================
    // 초기화
    // =========================================================

    protected override void Start()
    {
        base.Start();
        _animator = GetComponentInChildren<Animator>();
        _sr       = GetComponentInChildren<SpriteRenderer>();

        if (windupWarningVisual != null)
        {
            _warningBaseScaleX = windupWarningVisual.transform.localScale.x;
            _warningBaseScaleY = windupWarningVisual.transform.localScale.y;
            windupWarningVisual.SetActive(false);
        }
    }

    public override void StatSet(EnemySO so = null)
    {
        base.StatSet(so);
        _dashSO = MainSO as DashEnemySO;
        if (_dashSO == null)
            Debug.LogWarning($"[DashEnemy] MainSO is not DashEnemySO: {name}");

        _state = DashState.Chase;
        _routineRunning = false;

        if (windupWarningVisual != null)
            windupWarningVisual.SetActive(false);
    }

    // =========================================================
    // AI 루프 (OnTick)
    // =========================================================

    protected override void OnTick()
    {
        if (Player == null)
        {
            ResolvePlayer();
            _rb2d.linearVelocity = Vector2.zero;
            SetWalk(false);
            return;
        }

        switch (_state)
        {
            case DashState.Chase:
                UpdateChase();
                break;

            case DashState.Windup:
                // Windup 상태에서는 대기하며 플레이어 추적을 멈춤
                break;

            case DashState.Dashing:
                UpdateDashing(); // 최대 거리 초과 감지
                break;

            case DashState.Cooldown:
                // 코루틴이 처리 중. 여기서는 아무것도 하지 않음 (기절 상태)
                break;
        }
    }

    // ─── Chase ───────────────────────────────────────────────

    private void UpdateChase()
    {
        float dist  = Vector2.Distance(transform.position, Player.transform.position);
        Vector2 dir = (Player.transform.position - transform.position).normalized;

        FlipSprite(dir);

        float range = _dashSO != null ? _dashSO.dashRange : 3.5f;

        if (dist <= range)
        {
            // 사거리 진입 → Windup 시작
            _rb2d.linearVelocity = Vector2.zero;
            SetWalk(false);
            TransitionTo(DashState.Windup);
            _dashDir = dir; // 윈드업 돌입 당시 방향 고정
            StartCoroutine(WindupRoutine());
        }
        else
        {
            // 추적 이동
            _rb2d.linearVelocity = dir * speed;
            SetWalk(true);
        }
    }

    // ─── Windup ───────────────────────────────────────────────

    private IEnumerator WindupRoutine()
    {
        _routineRunning = true;

        float windupTime = _dashSO != null ? _dashSO.windupTime : 1.2f;

        // 예고선 활성화 및 설정
        if (windupWarningVisual != null)
        {
            // Sorting Layer를 WORLD_GROUNDFX로 설정
            SpriteRenderer warningSr = windupWarningVisual.GetComponent<SpriteRenderer>();
            if (warningSr == null)
                warningSr = windupWarningVisual.GetComponentInChildren<SpriteRenderer>(true);
            if (warningSr != null)
            {
                warningSr.sortingLayerName = "WORLD_GROUNDFX";
            }

            windupWarningVisual.SetActive(true);

            // 초기 방향과 위치 설정
            float angle = Mathf.Atan2(_dashDir.y, _dashDir.x) * Mathf.Rad2Deg;
            windupWarningVisual.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            float range = _dashSO != null ? _dashSO.dashRange : 3.5f;
            float targetScaleX = _warningBaseScaleX * (range / 1.6f);

            Vector3 s = windupWarningVisual.transform.localScale;
            s.x = targetScaleX;
            windupWarningVisual.transform.localScale = s;

            Vector3 centerOffset = Vector3.up * warningLineYOffset;
            windupWarningVisual.transform.localPosition = (Vector3)(_dashDir * (targetScaleX * 0.5f)) + centerOffset;
        }

        // Windup 대기하며 스케일 보간 (1.0 -> 0.5)
        float elapsed = 0f;
        float rangeVal = _dashSO != null ? _dashSO.dashRange : 3.5f;
        while (elapsed < windupTime)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / windupTime);
            float scaleFactor = Mathf.Lerp(1.0f, 0.5f, progress);

            if (windupWarningVisual != null)
            {
                Vector3 s = windupWarningVisual.transform.localScale;
                s.x = _warningBaseScaleX * (rangeVal / 1.6f);
                s.y = _warningBaseScaleY * scaleFactor;
                windupWarningVisual.transform.localScale = s;
            }

            yield return null;
        }

        // 예고선 비활성화 + 돌진 직전 짧은 텀(긴장감 연출)
        if (windupWarningVisual != null)
            windupWarningVisual.SetActive(false);

        yield return new WaitForSeconds(0.2f);

        // _dashDir는 윈드업 진입 당시 고정한 방향을 그대로 사용하므로 여기서 덮어쓰지 않음
        _dashStartPos = transform.position;
        TransitionTo(DashState.Dashing);

        _routineRunning = false;
    }

    // ─── Dashing ──────────────────────────────────────────────

    private void UpdateDashing()
    {
        // 최대 돌진 거리 초과 시 자동 정지
        float maxDist = _dashSO != null ? _dashSO.dashMaxDistance : 15f;
        if (Vector3.Distance(transform.position, _dashStartPos) >= maxDist)
        {
            StopDash();
            return;
        }

        // 속도 지속 유지 (OnCollisionEnter2D에서 충돌 시 정지 처리)
        float dashSpeed = _dashSO != null ? _dashSO.dashSpeed : 14f;
        _rb2d.linearVelocity = _dashDir * dashSpeed;
    }

    // ─── Cooldown ─────────────────────────────────────────────

    private IEnumerator CooldownRoutine()
    {
        _routineRunning = true;

        float cooldown = _dashSO != null ? _dashSO.dashCooldown : 1.5f;
        yield return new WaitForSeconds(cooldown);

        TransitionTo(DashState.Chase);
        _routineRunning = false;
    }

    // ─── 상태 전환 헬퍼 ───────────────────────────────────────

    protected virtual void TransitionTo(DashState next)
    {
        _state = next;
    }

    /// <summary>
    /// 돌진을 즉시 멈추고 Cooldown 상태로 전환한다.
    /// 충돌 이벤트 및 최대거리 초과 시 모두 이 메서드를 거친다.
    /// </summary>
    private void StopDash()
    {
        if (_state != DashState.Dashing) return;

        _rb2d.linearVelocity = Vector2.zero;
        SetWalk(false);

        TransitionTo(DashState.Cooldown);
        StartCoroutine(CooldownRoutine());
    }

    // =========================================================
    // 충돌 처리 (돌진 정지)
    // =========================================================

    public override void OnCollisionEnter2D(Collision2D collision)
    {
        // 기본 접촉 데미지 처리 (CreatureBase가 플레이어 판정 및 데미지 처리)
        base.OnCollisionEnter2D(collision);

        // 돌진 중 무언가에 부딪히면 즉시 정지
        if (_state == DashState.Dashing)
        {
            StopDash();
        }
    }

    // =========================================================
    // 사망 처리
    // =========================================================

    protected override void Die()
    {
        StopAllCoroutines();
        _routineRunning = false;
        _rb2d.linearVelocity = Vector2.zero;
        SetWalk(false);

        if (windupWarningVisual != null)
            windupWarningVisual.SetActive(false);

        base.Die();
    }

    // =========================================================
    // 유틸
    // =========================================================

    private void FlipSprite(Vector2 dir)
    {
        if (_sr != null && Mathf.Abs(dir.x) > 0.01f)
            _sr.flipX = dir.x < 0f;
    }

    private void SetWalk(bool value)
    {
        if (_animator == null) return;
        _animator.SetBool(AnimIsWalk, value);
    }
}
