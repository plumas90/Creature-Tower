using System.Collections;
using UnityEngine;

/// <summary>
/// 2.5D 점프형 슬라임 몬스터.
///
/// [충돌 설계 - 레이어 전환 방식]
/// ─────────────────────────────────────────────────────────────
/// 지상 상태 → GameObject.layer = "Creatuer"
///   - Physics2D Layer Matrix에 의해 정상적인 모든 충돌 처리
///   - 메인 콜라이더(isTrigger=true) : 접촉 데미지 감지
///   - _wallCollider(isTrigger=false) : 벽/바닥 물리 충돌
///
/// 점프 상태 → GameObject.layer = "CreatureJump"
///   Physics2D Layer Matrix (CreatureJump 기준):
///     ✅ Wall, Ground : COLLIDE  → 벽/바닥은 막힘
///     ❌ Player       : IGNORE   → 플레이어 물리 충돌 없음 (밀치지 않음)
///     ❌ Bullet       : IGNORE   → 총알 통과 (점프 중 무적)
///   - 콜라이더 Enable/Disable 없음
///   - isTrigger 변경 없음
///   - IgnoreCollision 없음
///   → 착지 시 오버랩 위치보정 문제 없음
/// ─────────────────────────────────────────────────────────────
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SlimeEnemy : EnemyBase
{
    // ─── 인스펙터 필드 ───────────────────────────────────────────
    [Header("Visual Elements")]
    [SerializeField] private Transform visualTransform;
    [SerializeField] private Transform shadowTransform;

    [Header("Jump Settings")]
    [SerializeField] private float jumpInterval       = 3.5f;
    [SerializeField] private float jumpHeight         = 2.2f;
    [SerializeField] private float jumpDuration       = 0.9f;
    [SerializeField] private float jumpSpeedMultiplier= 1.8f;
    [SerializeField] private float jumpDelay          = 0.4f;
    [SerializeField] private float landDelay          = 0.5f;
    [SerializeField] private float jumpTriggerRange   = 7.0f;

    [Header("Sprite Animation")]
    [SerializeField] private Sprite[] idleSprites = new Sprite[4];
    [SerializeField] private Sprite[] jumpSprites = new Sprite[4];
    [SerializeField] private Sprite   holdSprite;
    [SerializeField] private Sprite[] downSprites = new Sprite[3];

    // ─── 내부 상태 ───────────────────────────────────────────────
    private bool  _isJumping;
    private float _jumpTimer;

    private CircleCollider2D _wallCollider;  // 벽 전용 물리 콜라이더 (isTrigger=false)
    private Collider2D[]     _mainColliders; // 데미지 감지 콜라이더들 (isTrigger=true)

    // 레이어 인덱스 (Start에서 캐싱)
    private int _layerGround;   // "Creatuer" 레이어 (지상)
    private int _layerAirborne; // "CreatureJump" 레이어 (점프 중)

    private Vector3 _initialVisualScale;
    private Vector3 _initialShadowScale;
    private Coroutine _jumpCoroutine;
    private Coroutine _idleAnimCoroutine;

    private readonly float _keepDistance = 4.0f;

    private Animator       _animator;
    private SpriteRenderer _visualSR;
    private int            _currentIdleFrame;
    private readonly float _idleFrameRate = 0.15f;

    // =========================================================
    // 초기화
    // =========================================================

    protected override void Start()
    {
        base.Start();

        _animator = GetComponentInChildren<Animator>();

        // 레이어 인덱스 캐싱
        _layerGround   = LayerMask.NameToLayer("Creatuer");
        _layerAirborne = LayerMask.NameToLayer("CreatureJump");

        if (_layerAirborne < 0)
            Debug.LogError("[SlimeEnemy] 'CreatureJump' 레이어가 없습니다. " +
                           "Project Settings > Tags and Layers에서 추가하세요.");

        // Visual 트랜스폼 탐색
        if (visualTransform == null)
        {
            visualTransform = transform.Find("Visual");
            if (visualTransform == null)
            {
                SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
                visualTransform = (sr != null && sr.transform != transform)
                    ? sr.transform : transform;
            }
        }
        if (visualTransform != null)
        {
            _visualSR           = visualTransform.GetComponent<SpriteRenderer>();
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

        _jumpTimer = Random.Range(1.0f, jumpInterval);

        SetupColliders();

        if (_animator != null) _animator.enabled = false;
        _idleAnimCoroutine = StartCoroutine(CoIdleAnimation());
    }

    /// <summary>
    /// 콜라이더 초기 설정.
    /// 기존 콜라이더 → isTrigger=true (접촉 데미지 전용)
    /// _wallCollider → isTrigger=false (벽/바닥 물리 충돌 전용)
    /// </summary>
    private void SetupColliders()
    {
        _mainColliders = GetComponentsInChildren<Collider2D>(true);
        foreach (var c in _mainColliders)
            if (c != null) c.isTrigger = true;

        _wallCollider         = gameObject.AddComponent<CircleCollider2D>();
        _wallCollider.radius  = 0.42f;
        _wallCollider.isTrigger = false;
    }

    // =========================================================
    // AI 틱
    // =========================================================

    protected override void OnTick()
    {
        if (Player == null)
        {
            ResolvePlayer();
            _rb2d.linearVelocity = Vector2.zero;
            return;
        }

        if (_isJumping) return;

        float  dist = Vector2.Distance(transform.position, Player.transform.position);
        Vector2 dir = (Player.transform.position - transform.position).normalized;

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

        if (_visualSR != null && Mathf.Abs(dir.x) > 0.01f)
            _visualSR.flipX = dir.x < 0f;

        _jumpTimer -= Time.deltaTime;
        if (_jumpTimer <= 0f && dist <= jumpTriggerRange)
            _jumpCoroutine = StartCoroutine(JumpRoutine());
    }

    // =========================================================
    // 점프 루틴
    // =========================================================

    private IEnumerator JumpRoutine()
    {
        _isJumping = true;
        _jumpTimer = jumpInterval;

        // ── 1. 선딜레이 (찌부러짐 연출) ─────────────────────────
        _rb2d.linearVelocity = Vector2.zero;
        SetWalk(false);

        float elapsed = 0f;
        int   lastJumpFrame = -1;
        while (elapsed < jumpDelay)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDelay;

            int fi = Mathf.Clamp(Mathf.FloorToInt(t * 4f), 0, 3);
            if (fi != lastJumpFrame && _visualSR != null
                && jumpSprites != null && fi < jumpSprites.Length
                && jumpSprites[fi] != null)
            {
                _visualSR.sprite = jumpSprites[fi];
                lastJumpFrame    = fi;
            }

            if (visualTransform != null)
                visualTransform.localScale = new Vector3(
                    _initialVisualScale.x * (1f + 0.2f * t),
                    _initialVisualScale.y * (1f - 0.25f * t),
                    _initialVisualScale.z);

            yield return null;
        }
        if (visualTransform != null)
            visualTransform.localScale = _initialVisualScale;

        // ── 2. 공중 상태 ON ──────────────────────────────────────
        // 레이어를 "CreatureJump"로 전환
        // → Layer Matrix: Player=IGNORE, Wall/Ground=COLLIDE
        // → 콜라이더는 그대로 유지 (isTrigger 변경 없음)
        SetAirborneState(true);

        Vector2 jumpDir = Player != null
            ? (Player.transform.position - transform.position).normalized
            : Vector2.up;

        // ── 3. 체공 + 돌진 ────────────────────────────────────────
        float jumpTime    = 0f;
        int   lastDownFrame = -1;
        while (jumpTime < jumpDuration)
        {
            jumpTime += Time.deltaTime;
            float t = jumpTime / jumpDuration;

            // 물리 엔진이 벽/바닥을 처리하므로 속도만 설정
            _rb2d.linearVelocity = jumpDir * (speed * jumpSpeedMultiplier);

            // 포물선 높이: y = 4H·t·(1-t)
            float currentHeight = 4f * jumpHeight * t * (1f - t);

            // 스프라이트 갱신
            if (_visualSR != null)
            {
                if (t < 0.3f)
                {
                    if (jumpSprites != null && jumpSprites.Length > 3 && jumpSprites[3] != null)
                        _visualSR.sprite = jumpSprites[3];
                }
                else if (t < 0.85f)
                {
                    if (holdSprite != null) _visualSR.sprite = holdSprite;
                }
                else
                {
                    float t2 = (t - 0.85f) / 0.15f;
                    int   di = Mathf.Clamp(Mathf.FloorToInt(t2 * 3f), 0, 2);
                    if (di != lastDownFrame && downSprites != null
                        && di < downSprites.Length && downSprites[di] != null)
                    {
                        _visualSR.sprite = downSprites[di];
                        lastDownFrame    = di;
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

            if (shadowTransform != null)
            {
                float shadowScale = 1f - (currentHeight / jumpHeight) * 0.35f;
                shadowTransform.localScale = _initialShadowScale * shadowScale;
            }

            yield return null;
        }

        // ── 4. 착지 ──────────────────────────────────────────────
        _rb2d.linearVelocity = Vector2.zero;

        if (visualTransform != null)
        {
            visualTransform.localPosition = Vector3.zero;
            visualTransform.localScale    = _initialVisualScale;
        }
        if (shadowTransform != null)
            shadowTransform.localScale = _initialShadowScale;

        // 착지 바운스 연출
        elapsed = 0f;
        const float landBounceTime = 0.15f;
        while (elapsed < landBounceTime)
        {
            elapsed += Time.deltaTime;
            float bounce = Mathf.Sin((elapsed / landBounceTime) * Mathf.PI) * 0.12f;
            if (visualTransform != null)
                visualTransform.localScale = new Vector3(
                    _initialVisualScale.x * (1f + bounce),
                    _initialVisualScale.y * (1f - bounce),
                    _initialVisualScale.z);
            yield return null;
        }
        if (visualTransform != null)
            visualTransform.localScale = _initialVisualScale;

        // ── 5. 공중 상태 OFF ─────────────────────────────────────
        // 착지 시 플레이어와 겹쳐있을 경우 위치 보정하여 밀치기/벽뚫기 현상 방지
        CorrectLandingPosition();

        // 레이어를 "Creatuer"로 복구
        SetAirborneState(false);

        yield return new WaitForSeconds(landDelay);
        _isJumping = false;
    }

    /// <summary>
    /// 착지 직전 플레이어와의 겹침을 감지하고 안전한 위치로 슬라임을 보정합니다.
    /// 플레이어가 벽에 닿아있다면 그 방향으로 플레이어를 밀지 않는 방향으로 대피합니다.
    /// </summary>
    private void CorrectLandingPosition()
    {
        if (Player == null) return;

        // 1. 플레이어 및 슬라임의 반경 계산
        float rPlayer = 0.4f; // 기본값
        Collider2D playerCol = Player.GetComponent<Collider2D>();
        if (playerCol != null)
        {
            if (playerCol is CircleCollider2D circleCol)
            {
                rPlayer = circleCol.radius * Player.transform.lossyScale.x;
            }
            else if (playerCol is CapsuleCollider2D capsuleCol)
            {
                rPlayer = capsuleCol.size.x * 0.5f * Player.transform.lossyScale.x;
            }
            else
            {
                rPlayer = playerCol.bounds.extents.x;
            }
        }

        float rSlime = 0.42f;
        if (_wallCollider != null)
        {
            rSlime = _wallCollider.radius * transform.lossyScale.x;
        }

        // 안전한 최소 분리 거리 (두 콜라이더 반경 합 + 여유분)
        float minDist = rSlime + rPlayer + 0.05f;

        Vector2 slimePos = transform.position;
        Vector2 playerPos = Player.transform.position;
        Vector2 toPlayer = playerPos - slimePos;
        float currentDist = toPlayer.magnitude;

        // 겹쳐있지 않으면 보정 필요 없음
        if (currentDist >= minDist) return;

        // 2. 플레이어의 벽 접촉 확인 (상/하/좌/우 4방향 검사)
        LayerMask wallLayerMask = LayerMask.GetMask("Wall", "Ground");
        Vector2[] cardinalDirs = new Vector2[] { Vector2.right, Vector2.left, Vector2.up, Vector2.down };
        Vector2 playerWallVector = Vector2.zero;

        for (int i = 0; i < 4; i++)
        {
            Vector2 dir = cardinalDirs[i];
            // 플레이어 중심에서 해당 방향으로 살짝 이동한 위치에서 벽 오버랩 검사
            Vector2 checkPos = playerPos + dir * 0.08f;
            Collider2D wallCol = Physics2D.OverlapCircle(checkPos, rPlayer, wallLayerMask);
            if (wallCol != null)
            {
                playerWallVector += dir;
            }
        }

        // 3. 슬라임 대피 후보 방향 (8방향) 정의 및 정렬
        Vector2 defaultEscapeDir = currentDist > 0.01f ? -toPlayer.normalized : Vector2.right;
        Vector2[] candidates = new Vector2[]
        {
            new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1),
            new Vector2(1, 1).normalized, new Vector2(-1, 1).normalized,
            new Vector2(1, -1).normalized, new Vector2(-1, -1).normalized
        };

        // 원래 대피 방향과 가장 가까운 후보 순으로 정렬
        System.Array.Sort(candidates, (a, b) =>
        {
            float dotA = Vector2.Dot(a, defaultEscapeDir);
            float dotB = Vector2.Dot(b, defaultEscapeDir);
            return dotB.CompareTo(dotA);
        });

        Vector2 bestPos = slimePos;
        bool foundValid = false;
        float bestDistToOriginal = float.MaxValue;

        // 1단계: 벽 밀기 검사 + 슬라임 벽 충돌 검사를 모두 만족하는 최적의 위치 탐색
        foreach (var dir in candidates)
        {
            // 플레이어 기준 dir 방향으로 minDist만큼 떨어진 후보 위치 계산
            Vector2 targetPos = playerPos + dir * minDist;

            // 플레이어를 벽 쪽으로 미는 방향인지 검사
            if (playerWallVector != Vector2.zero)
            {
                // 슬라임이 플레이어를 미는 방향은 -dir 이다.
                // -dir과 플레이어의 벽 접촉 벡터(playerWallVector)가 같은 방향이면 배제한다.
                float pushDot = Vector2.Dot(-dir, playerWallVector.normalized);
                if (pushDot > 0.1f) continue;
            }

            // 슬라임 자체가 벽에 파묻히는지 검사
            Collider2D slimeWallOverlap = Physics2D.OverlapCircle(targetPos, rSlime * 0.95f, wallLayerMask);
            if (slimeWallOverlap == null)
            {
                float distToOriginal = Vector2.Distance(slimePos, targetPos);
                if (distToOriginal < bestDistToOriginal)
                {
                    bestDistToOriginal = distToOriginal;
                    bestPos = targetPos;
                    foundValid = true;
                }
            }
        }

        // 2단계: 만족하는 최적 위치가 없을 경우(예: 구석), 벽 밀기 조건을 제외하고 슬라임 벽 충돌만 피하는 위치 선택
        if (!foundValid)
        {
            bestDistToOriginal = float.MaxValue;
            foreach (var dir in candidates)
            {
                Vector2 targetPos = playerPos + dir * minDist;
                Collider2D slimeWallOverlap = Physics2D.OverlapCircle(targetPos, rSlime * 0.95f, wallLayerMask);
                if (slimeWallOverlap == null)
                {
                    float distToOriginal = Vector2.Distance(slimePos, targetPos);
                    if (distToOriginal < bestDistToOriginal)
                    {
                        bestDistToOriginal = distToOriginal;
                        bestPos = targetPos;
                        foundValid = true;
                    }
                }
            }
        }

        // 4. 보정 위치 적용
        if (foundValid)
        {
            transform.position = new Vector3(bestPos.x, bestPos.y, transform.position.z);
            _rb2d.position = bestPos;
        }
    }

    // =========================================================
    // 공중 상태 제어 (레이어 전환)
    // =========================================================

    /// <summary>
    /// 공중/지상 상태 전환.
    /// 레이어만 바꾸는 방식으로 콜라이더 구조는 일절 변경하지 않음.
    ///
    /// isAirborne=true  → layer = "CreatureJump"
    ///   Layer Matrix: Player=IGNORE, Wall/Ground=COLLIDE
    ///   → 플레이어 물리 밀침 없음, 벽은 막힘
    ///
    /// isAirborne=false → layer = "Creatuer"
    ///   모든 충돌 정상 복구
    /// </summary>
    private void SetAirborneState(bool isAirborne)
    {
        invincibility        = isAirborne;
        IsPassThroughBullets = isAirborne;

        // 레이어 전환 (GameObject.layer만 변경)
        int targetLayer = isAirborne ? _layerAirborne : _layerGround;
        gameObject.layer = targetLayer;

        // 자식 오브젝트의 레이어도 함께 전환
        // (자식 콜라이더가 있을 경우 부모 RB2D가 제어하지만 레이어는 별도)
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child != transform)
                child.gameObject.layer = targetLayer;
        }

        // 공중 상태일 때는 플레이어와의 모든 물리 충돌을 무시 (IgnoreCollision)
        if (Player != null)
        {
            Collider2D[] playerColliders = Player.GetComponentsInChildren<Collider2D>(true);
            Collider2D[] slimeColliders  = GetComponentsInChildren<Collider2D>(true);

            foreach (var pCol in playerColliders)
            {
                if (pCol == null) continue;
                foreach (var sCol in slimeColliders)
                {
                    if (sCol == null) continue;
                    Physics2D.IgnoreCollision(pCol, sCol, isAirborne);
                }
            }
        }
    }

    // =========================================================
    // 유틸리티
    // =========================================================

    private void SetWalk(bool value)
    {
        if (_animator == null) return;
        _animator.SetBool(Animator.StringToHash("IsWalking"), value);
    }

    // =========================================================
    // 사망 처리
    // =========================================================

    protected override void Die()
    {
        if (_idleAnimCoroutine != null) StopCoroutine(_idleAnimCoroutine);
        if (_jumpCoroutine     != null) StopCoroutine(_jumpCoroutine);

        if (visualTransform != null)
        {
            visualTransform.localPosition = Vector3.zero;
            visualTransform.localScale    = _initialVisualScale;
        }
        if (shadowTransform != null)
            shadowTransform.localScale = _initialShadowScale;

        _rb2d.linearVelocity = Vector2.zero;

        if (_mainColliders != null)
            foreach (var c in _mainColliders)
                if (c != null) c.enabled = false;
        if (_wallCollider != null)
            _wallCollider.enabled = false;

        invincibility        = false;
        IsPassThroughBullets = false;
        SetWalk(false);

        base.Die();
    }

    // =========================================================
    // Idle 애니메이션
    // =========================================================

    private IEnumerator CoIdleAnimation()
    {
        while (!isDead)
        {
            if (!_isJumping && _visualSR != null
                && idleSprites != null && idleSprites.Length > 0
                && idleSprites[0] != null)
            {
                _visualSR.sprite  = idleSprites[_currentIdleFrame];
                _currentIdleFrame = (_currentIdleFrame + 1) % idleSprites.Length;
            }
            yield return new WaitForSeconds(_idleFrameRate);
        }
    }
}
