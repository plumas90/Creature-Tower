using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2.5D 점프형 슬라임 몬스터 클래스.
/// - 플레이어를 향해 천천히 다가오다가 주기적으로 높이 뛰어오릅니다.
/// - 점프하여 공중에 떠 있는 동안에는 콜리더를 비활성화하여 플레이어 총알이 통과하고, 플레이어에게도 닿지 않습니다 (무적 및 충돌 무시).
/// - 2.5D 비주얼 구현을 위해 슬라임 본체 이미지가 있는 Visual 트랜스폼만 로컬 Y축 위로 상승하며, 바닥에는 Shadow(그림자)가 남습니다.
/// - 공중에 높이 떠오를수록 그림자의 스케일이 작아지는 연출이 포함되어 있습니다.
/// - 지면(그림자 위치)으로 다시 내려와 착지하면 콜리더를 켜서 피격 및 몸빵 공격이 가능하게 전환됩니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SlimeEnemy : EnemyBase
{
    [Header("Slime Visual Elements")]
    [Tooltip("슬라임의 비주얼(스프라이트, 애니메이터 등)이 들어있는 자식 트랜스폼입니다.")]
    [SerializeField] private Transform visualTransform;

    [Tooltip("슬라임의 그림자 스프라이트가 들어있는 자식 트랜스폼입니다.")]
    [SerializeField] private Transform shadowTransform;

    [Header("Slime Jump Attack Settings")]
    [Tooltip("점프를 뛰는 쿨타임 주기 (초) 입니다.")]
    [SerializeField] private float jumpInterval = 3.5f;

    [Tooltip("점프 시 떠오르는 최대 높이(Y축) 입니다.")]
    [SerializeField] private float jumpHeight = 2.2f;

    [Tooltip("공중에 머무는 전체 시간 (초) 입니다.")]
    [SerializeField] private float jumpDuration = 0.9f;

    [Tooltip("점프 돌진 시 기본 이동 속도(speed)에 곱해질 속도 배율입니다.")]
    [SerializeField] private float jumpSpeedMultiplier = 1.8f;

    [Tooltip("점프 도약 직전 제자리에서 찌부러지며 멈춰있는 선딜레이 시간입니다.")]
    [SerializeField] private float jumpDelay = 0.4f;

    [Tooltip("착지 직후 제자리에서 멍때리며 멈춰있는 후딜레이 시간입니다.")]
    [SerializeField] private float landDelay = 0.5f;

    [Tooltip("플레이어가 이 거리 안으로 들어오면 점프를 시작합니다.")]
    [SerializeField] private float jumpTriggerRange = 7.0f;

    // 내부 상태 제어 변수
    private bool _isJumping = false;
    private float _jumpTimer;
    private Collider2D[] _myColliders;
    private Vector3 _initialVisualScale;
    private Vector3 _initialShadowScale;
    private Coroutine _jumpCoroutine;
    private float _keepDistance = 4.0f;

    private Animator _animator;
    private SpriteRenderer _visualSR;
    private static readonly int AnimIsWalk = Animator.StringToHash("IsWalking");

    protected override void Start()
    {
        base.Start();

        // 컴포넌트 및 자식 탐색 방어 코드
        _animator = GetComponentInChildren<Animator>();
        _myColliders = GetComponents<Collider2D>();

        if (visualTransform == null)
        {
            visualTransform = transform.Find("Visual");
            if (visualTransform == null)
            {
                // 차선책으로 자기 자신 밑에서 SpriteRenderer를 가진 첫 번째 자식을 찾음
                SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
                if (sr != null && sr.transform != transform)
                {
                    visualTransform = sr.transform;
                }
                else
                {
                    visualTransform = transform;
                    Debug.LogWarning($"[SlimeEnemy] '{name}'에 Visual 트랜스폼이 할당되지 않아 본체를 움직입니다. 2.5D 점프 연출이 부자연스러울 수 있습니다.");
                }
            }
        }

        if (visualTransform != null)
        {
            _visualSR = visualTransform.GetComponent<SpriteRenderer>();
            _initialVisualScale = visualTransform.localScale;
        }

        if (shadowTransform == null)
        {
            shadowTransform = transform.Find("Shadow");
            if (shadowTransform == null)
            {
                // 이름에 "shadow"가 들어간 자식을 탐색
                foreach (Transform child in GetComponentsInChildren<Transform>())
                {
                    if (child.name.ToLower().Contains("shadow") && child != transform)
                    {
                        shadowTransform = child;
                        break;
                    }
                }
            }
        }

        if (shadowTransform != null)
        {
            _initialShadowScale = shadowTransform.localScale;
        }

        // 점프 타이머 무작위 초기화 (생성 직후 동시에 다 뛰는 현상 방지)
        _jumpTimer = Random.Range(1.0f, jumpInterval);

        // SO 데이터 동기화 (조절 가능 변수 연동)
        if (MainSO != null)
        {
            landDelay = MainSO.slimeLandRestTime;
            _keepDistance = MainSO.slimeKeepDistance;
        }
    }

    // ─── AI 및 틱 관리 ──────────────────────────────────────────
    protected override void OnTick()
    {
        if (Player == null)
        {
            ResolvePlayer();
            _rb2d.linearVelocity = Vector2.zero;
            SetWalk(false);
            return;
        }

        // 점프 중일 때는 OnTick의 기본 걷기 AI가 속도를 덮어쓰지 않도록 완전 차단
        if (_isJumping) return;

        float dist = Vector2.Distance(transform.position, Player.transform.position);
        Vector2 dir = (Player.transform.position - transform.position).normalized;

        // 플레이어한테 점프 사거리/유지 거리 이상으로 다가가지 않음
        if (dist > _keepDistance)
        {
            _rb2d.linearVelocity = dir * speed;
            SetWalk(true);
        }
        else
        {
            _rb2d.linearVelocity = Vector2.zero;
            SetWalk(false);
        }

        // 비주얼 스프라이트 좌우 반전
        if (_visualSR != null && Mathf.Abs(dir.x) > 0.01f)
        {
            _visualSR.flipX = dir.x < 0f;
        }

        // 점프 타이머 갱신
        _jumpTimer -= Time.deltaTime;

        // 점프 트리거 조건: 타이머 완료 및 플레이어가 사거리 이내에 존재
        if (_jumpTimer <= 0f && dist <= jumpTriggerRange)
        {
            _jumpCoroutine = StartCoroutine(JumpRoutine());
        }
    }

    // ─── 2.5D 점프 물리 및 연출 루틴 ──────────────────────────────────────
    private IEnumerator JumpRoutine()
    {
        _isJumping = true;
        _jumpTimer = jumpInterval;

        // 1. 도약 준비 (선딜레이)
        // 물리 속도 일시 정지 및 걷기 애니메이션 비활성화
        _rb2d.linearVelocity = Vector2.zero;
        SetWalk(false);

        // 찌부러지는(Squish) 도약 준비 마이크로 애니메이션 연출 (비주얼 극대화)
        float elapsed = 0f;
        while (elapsed < jumpDelay)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDelay;
            // 로컬 Y는 누르고, X/Z는 퍼지게 스케일 보정
            if (visualTransform != null)
            {
                visualTransform.localScale = new Vector3(
                    _initialVisualScale.x * (1f + 0.2f * t),
                    _initialVisualScale.y * (1f - 0.25f * t),
                    _initialVisualScale.z
                );
            }
            yield return null;
        }

        // 스케일 복원
        if (visualTransform != null)
        {
            visualTransform.localScale = _initialVisualScale;
        }

        // 2. 점프 도약 (공중 상태 시작)
        // 벽을 뚫고 맵 밖으로 나가지 않도록 콜리더는 켜둔 채 플레이어와의 물리 충돌만 무시
        IgnorePlayerCollision(true);
        invincibility = true;

        Vector2 jumpDir = (Player.transform.position - transform.position).normalized;

        // 3. 체공 및 돌진 (포물선 궤적 운동)
        float jumpTime = 0f;
        while (jumpTime < jumpDuration)
        {
            jumpTime += Time.deltaTime;
            float t = jumpTime / jumpDuration;

            // 수평 돌격 이동 속도 적용
            _rb2d.linearVelocity = jumpDir * (speed * jumpSpeedMultiplier);

            // 2.5D 공중 상승 포물선 계산: y = 4 * H * t * (1 - t)
            float currentHeight = 4f * jumpHeight * t * (1f - t);

            if (visualTransform != null)
            {
                visualTransform.localPosition = new Vector3(0f, currentHeight, 0f);
                
                // 공중에서 위아래로 살짝 늘어나는(Stretch) 연출
                float stretchAmount = Mathf.Sin(t * Mathf.PI) * 0.15f;
                visualTransform.localScale = new Vector3(
                    _initialVisualScale.x * (1f - stretchAmount),
                    _initialVisualScale.y * (1f + stretchAmount),
                    _initialVisualScale.z
                );

                // 공중 이동 방향에 따른 비주얼 반전 유지
                if (_visualSR != null && Mathf.Abs(_rb2d.linearVelocity.x) > 0.01f)
                {
                    _visualSR.flipX = _rb2d.linearVelocity.x < 0f;
                }
            }

            // 공중으로 뜰수록 그림자 스케일 축소 연출
            if (shadowTransform != null)
            {
                float shadowScaleFactor = 1f - (currentHeight / jumpHeight) * 0.35f;
                shadowTransform.localScale = _initialShadowScale * shadowScaleFactor;
            }

            yield return null;
        }

        // 4. 착지
        // 비주얼 위치 및 그림자 스케일 강제 원복
        if (visualTransform != null)
        {
            visualTransform.localPosition = Vector3.zero;
            visualTransform.localScale = _initialVisualScale;
        }
        if (shadowTransform != null)
        {
            shadowTransform.localScale = _initialShadowScale;
        }

        // 착지 충격에 의한 짧은 좌우 찌부러짐 튕김 연출
        elapsed = 0f;
        float landBounceTime = 0.15f;
        while (elapsed < landBounceTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / landBounceTime;
            float bounce = Mathf.Sin(t * Mathf.PI) * 0.12f;
            if (visualTransform != null)
            {
                visualTransform.localScale = new Vector3(
                    _initialVisualScale.x * (1f + bounce),
                    _initialVisualScale.y * (1f - bounce),
                    _initialVisualScale.z
                );
            }
            yield return null;
        }
        if (visualTransform != null)
        {
            visualTransform.localScale = _initialVisualScale;
        }

        // 속도 초기화 및 물리 충돌 / 피격 복원
        _rb2d.linearVelocity = Vector2.zero;
        IgnorePlayerCollision(false);
        invincibility = false;

        // 5. 착지 후 딜레이 (착지 후 휴식/멍때리기)
        yield return new WaitForSeconds(landDelay);

        _isJumping = false;
    }

    /// <summary>
    /// 점프 시 플레이어와의 물리적 충돌만 무시하여 몸통 박치기가 뚫리도록 처리합니다. (벽은 통과하지 못함)
    /// </summary>
    private void IgnorePlayerCollision(bool ignore)
    {
        if (Player == null) return;
        Collider2D playerCol = Player.GetComponent<Collider2D>();
        if (playerCol == null) return;

        if (_myColliders == null)
            _myColliders = GetComponents<Collider2D>();

        foreach (var col in _myColliders)
        {
            if (col != null)
            {
                Physics2D.IgnoreCollision(col, playerCol, ignore);
            }
        }
    }

    private void SetWalk(bool value)
    {
        if (_animator == null) return;
        _animator.SetBool(AnimIsWalk, value);
    }

    // ─── 사망 시 예외 처리 ─────────────────────────────────────────
    protected override void Die()
    {
        if (_jumpCoroutine != null)
        {
            StopCoroutine(_jumpCoroutine);
        }

        // 사망 시 비주얼 원복 및 물리 복구
        if (visualTransform != null)
        {
            visualTransform.localPosition = Vector3.zero;
            visualTransform.localScale = _initialVisualScale;
        }
        if (shadowTransform != null)
        {
            shadowTransform.localScale = _initialShadowScale;
        }

        _rb2d.linearVelocity = Vector2.zero;
        IgnorePlayerCollision(false);
        invincibility = false;
        SetWalk(false);

        base.Die();
    }
}
