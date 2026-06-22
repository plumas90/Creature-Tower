using System.Collections;
using UnityEngine;

/// <summary>
/// 2.5D 점프형 슬라임 몬스터.
///
/// [충돌 구조]
/// - 기존 CircleCollider2D → isTrigger=true : 물리 밀침 차단, OnTriggerEnter2D로 접촉 데미지 처리
/// - _wallCollider (동적 추가, isTrigger=false) : Wall/Ground 레이어와만 물리 충돌 → 벽 통과 방지
///   Player/Creatuer 레이어는 excludeLayers로 제외
/// - Physics2D Layer Matrix에서 Player↔Creatuer 물리 충돌 비활성화 (이중 보호)
///
/// [점프 동작]
/// - 점프 중(invincibility=true, IsPassThroughBullets=true): 총알 통과, 접촉 데미지 차단
/// - 착지 후: 무적 해제, 접촉 데미지 재활성화
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SlimeEnemy : EnemyBase
{
    [Header("Visual Elements")]
    [Tooltip("비주얼(스프라이트)이 있는 자식 트랜스폼")]
    [SerializeField] private Transform visualTransform;

    [Tooltip("그림자 스프라이트 자식 트랜스폼")]
    [SerializeField] private Transform shadowTransform;

    [Header("Jump Settings")]
    [Tooltip("점프 쿨타임 (초)")]
    [SerializeField] private float jumpInterval = 3.5f;

    [Tooltip("최대 점프 높이 (로컬 Y축)")]
    [SerializeField] private float jumpHeight = 2.2f;

    [Tooltip("체공 시간 (초)")]
    [SerializeField] private float jumpDuration = 0.9f;

    [Tooltip("점프 시 이동속도 배율")]
    [SerializeField] private float jumpSpeedMultiplier = 1.8f;

    [Tooltip("도약 전 선딜레이 (초)")]
    [SerializeField] private float jumpDelay = 0.4f;

    [Tooltip("착지 후 후딜레이 (초)")]
    [SerializeField] private float landDelay = 0.5f;

    [Tooltip("점프 트리거 사거리")]
    [SerializeField] private float jumpTriggerRange = 7.0f;

    [Header("Sprite Animation")]
    [SerializeField] private Sprite[] idleSprites = new Sprite[4];
    [SerializeField] private Sprite[] jumpSprites = new Sprite[4];
    [SerializeField] private Sprite holdSprite;
    [SerializeField] private Sprite[] downSprites = new Sprite[3];

    // ─── 내부 상태 ───────────────────────────────────────────────
    private bool _isJumping;
    private float _jumpTimer;
    private CircleCollider2D _wallCollider;
    private Vector3 _initialVisualScale;
    private Vector3 _initialShadowScale;
    private Coroutine _jumpCoroutine;
    private Coroutine _idleAnimCoroutine;
    private readonly float _keepDistance = 4.0f;

    private Animator _animator;
    private SpriteRenderer _visualSR;
    private int _currentIdleFrame;
    private readonly float _idleFrameRate = 0.15f;

    // ─── 초기화 ─────────────────────────────────────────────────
    protected override void Start()
    {
        base.Start();

        _animator = GetComponentInChildren<Animator>();

        // Visual 트랜스폼 탐색
        if (visualTransform == null)
        {
            visualTransform = transform.Find("Visual");
            if (visualTransform == null)
            {
                SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
                visualTransform = (sr != null && sr.transform != transform) ? sr.transform : transform;
            }
        }
        if (visualTransform != null)
        {
            _visualSR = visualTransform.GetComponent<SpriteRenderer>();
            _initialVisualScale = visualTransform.localScale;
        }

        // Shadow 트랜스폼 탐색
        if (shadowTransform == null)
        {
            shadowTransform = transform.Find("Shadow");
            if (shadowTransform == null)
            {
                foreach (Transform child in GetComponentsInChildren<Transform>())
                {
                    if (child != transform && child.name.ToLower().Contains("shadow"))
                    {
                        shadowTransform = child;
                        break;
                    }
                }
            }
        }
        if (shadowTransform != null)
            _initialShadowScale = shadowTransform.localScale;

        // 점프 타이머 랜덤 초기화 (동시 점프 방지)
        _jumpTimer = Random.Range(1.0f, jumpInterval);

        // ── 충돌 설정 ───────────────────────────────────────────
        // 1. 기존 루트 콜라이더 전부 Trigger로 전환 → 물리 밀침 차단
        foreach (var c in GetComponents<Collider2D>())
        {
            if (c != null) c.isTrigger = true;
        }

        // 2. 벽 전용 물리 콜라이더 추가
        //    - includeLayers: Wall|Ground 와만 충돌
        //    - excludeLayers: Player|Creatuer 명시 제외 (이중 보호)
        _wallCollider = gameObject.AddComponent<CircleCollider2D>();
        _wallCollider.radius = 0.42f;
        _wallCollider.isTrigger = false;
        _wallCollider.includeLayers = LayerMask.GetMask("Wall", "Ground");
        _wallCollider.excludeLayers = LayerMask.GetMask("Player", "Creatuer");

        // 애니메이터 비활성화 (커스텀 프레임 애니메이션 사용)
        if (_animator != null)
            _animator.enabled = false;

        _idleAnimCoroutine = StartCoroutine(CoIdleAnimation());
    }

    // ─── AI 틱 ─────────────────────────────────────────────────
    protected override void OnTick()
    {
        if (Player == null)
        {
            ResolvePlayer();
            _rb2d.linearVelocity = Vector2.zero;
            return;
        }

        // 점프 중에는 OnTick이 속도를 덮어쓰지 않음
        if (_isJumping) return;

        float dist = Vector2.Distance(transform.position, Player.transform.position);
        Vector2 dir = (Player.transform.position - transform.position).normalized;

        // 유지 거리 이상이면 플레이어를 향해 이동
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

        // 스프라이트 좌우 반전
        if (_visualSR != null && Mathf.Abs(dir.x) > 0.01f)
            _visualSR.flipX = dir.x < 0f;

        // 점프 타이머
        _jumpTimer -= Time.deltaTime;
        if (_jumpTimer <= 0f && dist <= jumpTriggerRange)
            _jumpCoroutine = StartCoroutine(JumpRoutine());
    }

    // ─── 점프 루틴 ──────────────────────────────────────────────
    private IEnumerator JumpRoutine()
    {
        _isJumping = true;
        _jumpTimer = jumpInterval;

        // 1. 도약 전 선딜레이 (찌부러짐 연출)
        _rb2d.linearVelocity = Vector2.zero;
        SetWalk(false);

        float elapsed = 0f;
        int lastJumpFrame = -1;
        while (elapsed < jumpDelay)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDelay;

            int frameIndex = Mathf.Clamp(Mathf.FloorToInt(t * 4f), 0, 3);
            if (frameIndex != lastJumpFrame && _visualSR != null && jumpSprites.Length > frameIndex && jumpSprites[frameIndex] != null)
            {
                _visualSR.sprite = jumpSprites[frameIndex];
                lastJumpFrame = frameIndex;
            }

            if (visualTransform != null)
            {
                visualTransform.localScale = new Vector3(
                    _initialVisualScale.x * (1f + 0.2f * t),
                    _initialVisualScale.y * (1f - 0.25f * t),
                    _initialVisualScale.z);
            }
            yield return null;
        }

        if (visualTransform != null)
            visualTransform.localScale = _initialVisualScale;

        // 2. 도약 시작: 무적 + 총알통과 ON
        SetJumpState(true);

        Vector2 jumpDir = Player != null
            ? (Player.transform.position - transform.position).normalized
            : Vector2.up;

        // 3. 체공 + 돌진 (포물선 궤적)
        float jumpTime = 0f;
        int lastDownFrame = -1;
        while (jumpTime < jumpDuration)
        {
            jumpTime += Time.deltaTime;
            float t = jumpTime / jumpDuration;

            _rb2d.linearVelocity = jumpDir * (speed * jumpSpeedMultiplier);

            // 포물선 높이: y = 4H·t·(1-t)
            float currentHeight = 4f * jumpHeight * t * (1f - t);

            // 스프라이트 프레임 갱신
            if (_visualSR != null)
            {
                if (t < 0.3f)
                {
                    if (jumpSprites.Length > 3 && jumpSprites[3] != null)
                        _visualSR.sprite = jumpSprites[3];
                }
                else if (t < 0.85f)
                {
                    if (holdSprite != null)
                        _visualSR.sprite = holdSprite;
                }
                else
                {
                    float t2 = (t - 0.85f) / 0.15f;
                    int downIdx = Mathf.Clamp(Mathf.FloorToInt(t2 * 3f), 0, 2);
                    if (downIdx != lastDownFrame && downSprites.Length > downIdx && downSprites[downIdx] != null)
                    {
                        _visualSR.sprite = downSprites[downIdx];
                        lastDownFrame = downIdx;
                    }
                }
            }

            if (visualTransform != null)
            {
                visualTransform.localPosition = new Vector3(0f, currentHeight, 0f);

                float stretch = Mathf.Sin(t * Mathf.PI) * 0.15f;
                visualTransform.localScale = new Vector3(
                    _initialVisualScale.x * (1f - stretch),
                    _initialVisualScale.y * (1f + stretch),
                    _initialVisualScale.z);

                if (_visualSR != null && Mathf.Abs(_rb2d.linearVelocity.x) > 0.01f)
                    _visualSR.flipX = _rb2d.linearVelocity.x < 0f;
            }

            // 그림자 크기 축소
            if (shadowTransform != null)
            {
                float shadowScale = 1f - (currentHeight / jumpHeight) * 0.35f;
                shadowTransform.localScale = _initialShadowScale * shadowScale;
            }

            yield return null;
        }

        // 4. 착지: 비주얼 원복
        if (visualTransform != null)
        {
            visualTransform.localPosition = Vector3.zero;
            visualTransform.localScale = _initialVisualScale;
        }
        if (shadowTransform != null)
            shadowTransform.localScale = _initialShadowScale;

        // 착지 충격 바운스 연출
        elapsed = 0f;
        const float landBounceTime = 0.15f;
        while (elapsed < landBounceTime)
        {
            elapsed += Time.deltaTime;
            float bounce = Mathf.Sin((elapsed / landBounceTime) * Mathf.PI) * 0.12f;
            if (visualTransform != null)
            {
                visualTransform.localScale = new Vector3(
                    _initialVisualScale.x * (1f + bounce),
                    _initialVisualScale.y * (1f - bounce),
                    _initialVisualScale.z);
            }
            yield return null;
        }
        if (visualTransform != null)
            visualTransform.localScale = _initialVisualScale;

        // 속도 초기화 + 무적/총알통과 OFF
        _rb2d.linearVelocity = Vector2.zero;
        SetJumpState(false);

        // 5. 착지 후 딜레이
        yield return new WaitForSeconds(landDelay);

        _isJumping = false;
    }

    /// <summary>점프 상태 토글: 무적 + 총알 통과 ON/OFF</summary>
    private void SetJumpState(bool isAirborne)
    {
        invincibility = isAirborne;
        IsPassThroughBullets = isAirborne;
    }

    private void SetWalk(bool value)
    {
        if (_animator == null) return;
        _animator.SetBool(Animator.StringToHash("IsWalking"), value);
    }

    // ─── 사망 처리 ───────────────────────────────────────────────
    protected override void Die()
    {
        if (_idleAnimCoroutine != null) StopCoroutine(_idleAnimCoroutine);
        if (_jumpCoroutine != null)    StopCoroutine(_jumpCoroutine);

        if (visualTransform != null)
        {
            visualTransform.localPosition = Vector3.zero;
            visualTransform.localScale = _initialVisualScale;
        }
        if (shadowTransform != null)
            shadowTransform.localScale = _initialShadowScale;

        _rb2d.linearVelocity = Vector2.zero;
        invincibility = false;
        IsPassThroughBullets = false;
        SetWalk(false);

        base.Die();
    }

    // ─── Idle 애니메이션 ─────────────────────────────────────────
    private IEnumerator CoIdleAnimation()
    {
        while (!isDead)
        {
            if (!_isJumping && _visualSR != null
                && idleSprites != null && idleSprites.Length > 0
                && idleSprites[0] != null)
            {
                _visualSR.sprite = idleSprites[_currentIdleFrame];
                _currentIdleFrame = (_currentIdleFrame + 1) % idleSprites.Length;
            }
            yield return new WaitForSeconds(_idleFrameRate);
        }
    }
}
