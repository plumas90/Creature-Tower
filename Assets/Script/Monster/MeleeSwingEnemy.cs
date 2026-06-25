using System.Collections;
using UnityEngine;

/// <summary>
/// 무기를 휘두르는 타입의 근접 일반 몬스터.
/// - 사거리 밖에서는 플레이어를 향해 걸어서 추적한다.
/// - 사거리 안에 도달하면 정지하여 무기를 휘둘러 데미지를 가한다.
/// - 휘두른 직후 N초 동안 정지 상태로 대기(쿨다운)한 뒤 다시 추적을 개시한다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MeleeSwingEnemy : EnemyBase
{
    [Header("시각적 연출 설정")]
    [Tooltip("평상시 들고 다니는 기본 무기 비주얼 (평소 ON, 공격 시 OFF)")]
    public GameObject weaponDefaultVisual;

    [Tooltip("공격 전 빨간색 경고 예고 비주얼 (공격 대기 시간 동안 ON)")]
    public GameObject weaponWarningVisual;

    [Tooltip("실제 타격 시 가해지는 궤적/타격 비주얼 (데미지 순간 ON)")]
    public GameObject weaponSwingVisual;

    [Tooltip("무기 스프라이트 원본이 기본적으로 기울어진 각도 (우측 상단 대각선 스프라이트인 경우 보통 45f)")]
    [SerializeField] private float weaponSpriteAngleOffset = 45f;

    [Header("스윙(Swing) 공격 물리/시각 설정")]
    [Tooltip("스윙 공격 시 회전 중심축을 몸 바깥쪽으로 밀어내는 오프셋 거리 (플레이어 방향으로 이동)")]
    [SerializeField] private float swingPivotOffset = 0.4f;

    [Tooltip("스윙 공격 시 휘두르는 반경(반지름)에 더해지는 추가 거리")]
    [SerializeField] private float swingRadiusBonus = 0.3f;

    [Tooltip("찌르기 공격 시 대기하는 오프셋 거리 (음수면 뒤로 당김, 양수면 앞으로 내밈, 기존 -0.4f에서 살짝 더 바깥인 -0.15f 기본값)")]
    [SerializeField] private float thrustPrepOffset = -0.15f;

    private MeleeSwingEnemySO _swingSO;

    private Animator _animator;
    private SpriteRenderer _sr;
    private static readonly int AnimIsWalk = Animator.StringToHash("IsWalking");
    private static readonly int AnimAttack = Animator.StringToHash("Attack");

    // 상태 제어
    private bool _isAttacking = false;
    private float _cooldownTimer = 0f;

    [Header("Sprite Animation (Fallback if no Animator)")]
    [SerializeField] private Sprite[] walkSprites;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private float walkFrameRate = 0.2f;

    private int _currentWalkFrame = 0;
    private float _walkAnimTimer = 0f;
    private bool _isWalkingState = false;

    // 평상시 무기 트랜스폼 캐싱
    private Vector3 _originalWeaponLocalPos;
    private Quaternion _originalWeaponLocalRot;
    private bool _hasCachedOriginalTransform = false;

    // 2D 무기 콜라이더 감지 제어
    private EnemyWeaponTrigger _weaponTrigger;
    private bool _hasDealtDamageThisAttack = false;

    // 최적화된 사거리 캐시
    private float _optimizedRange = 1.6f;

    protected override void Start()
    {
        base.Start();
        _animator = GetComponentInChildren<Animator>();
        _sr       = GetComponentInChildren<SpriteRenderer>();

        CacheOriginalWeaponTransform();
        InitializeWeaponBoundsAndCollider();
        ResetVisuals();
        InitWeaponTrigger();

        if (idleSprite == null && _sr != null)
        {
            idleSprite = _sr.sprite;
        }
    }

    public override void StatSet(EnemySO so = null)
    {
        base.StatSet(so);
        _swingSO = MainSO as MeleeSwingEnemySO;
        if (_swingSO == null)
            Debug.LogWarning($"[MeleeSwingEnemy] MainSO is not MeleeSwingEnemySO: {name}");

        _cooldownTimer = 0f;
        _isAttacking = false;

        CacheOriginalWeaponTransform();
        InitializeWeaponBoundsAndCollider();
        ResetVisuals();
        InitWeaponTrigger();

        if (idleSprite == null && _sr != null)
        {
            idleSprite = _sr.sprite;
        }
    }

    private void CacheOriginalWeaponTransform()
    {
        if (_hasCachedOriginalTransform) return;
        if (weaponDefaultVisual != null)
        {
            _originalWeaponLocalPos = weaponDefaultVisual.transform.localPosition;
            _originalWeaponLocalRot = weaponDefaultVisual.transform.localRotation;
            _hasCachedOriginalTransform = true;
        }
    }

    private void InitializeWeaponBoundsAndCollider()
    {
        if (weaponDefaultVisual == null)
        {
            _optimizedRange = _swingSO != null ? _swingSO.attackRange : 1.6f;
            return;
        }

        SpriteRenderer weaponSr = weaponDefaultVisual.GetComponent<SpriteRenderer>();
        if (weaponSr != null && weaponSr.sprite != null)
        {
            // 1. 무기 길이 분석 및 추격 정지 사거리 캐싱
            Bounds localB = weaponSr.localBounds;
            float weaponLen = Mathf.Max(localB.size.x, localB.size.y);
            float offset = _originalWeaponLocalPos.magnitude;

            // 정지 거리는 (파지 오프셋) + (무기 전체 길이 L의 절반)
            // 최소 1.2f 가드로 안전 충돌 여유값 확보
            _optimizedRange = Mathf.Max(offset + weaponLen * 0.5f, 1.2f);

            // 2. 무기 스프라이트 맞춤형 콜라이더 자동 피팅 (Auto-Collider Fitting)
            Collider2D existingCollider = weaponDefaultVisual.GetComponent<Collider2D>();

            if (existingCollider == null)
            {
                // 기존 콜라이더가 없으면 새 BoxCollider2D 추가 및 스프라이트 크기 맞춤
                BoxCollider2D boxCol = weaponDefaultVisual.AddComponent<BoxCollider2D>();
                boxCol.size = localB.size;
                boxCol.offset = localB.center;
                boxCol.isTrigger = true;
            }
            else
            {
                // 이미 설정된 콜라이더(PolygonCollider2D, BoxCollider2D 등)가 있다면 그대로 유지
                existingCollider.isTrigger = true;
            }
        }
        else
        {
            _optimizedRange = _swingSO != null ? _swingSO.attackRange : 1.6f;
        }
    }

    private void InitWeaponTrigger()
    {
        if (weaponDefaultVisual != null)
        {
            _weaponTrigger = weaponDefaultVisual.GetComponent<EnemyWeaponTrigger>();
            if (_weaponTrigger == null)
            {
                _weaponTrigger = weaponDefaultVisual.AddComponent<EnemyWeaponTrigger>();
            }
            _weaponTrigger.Init(this);
        }
    }

    private void ResetVisuals()
    {
        if (weaponDefaultVisual != null)
        {
            weaponDefaultVisual.SetActive(true);
            if (_hasCachedOriginalTransform)
            {
                weaponDefaultVisual.transform.localPosition = _originalWeaponLocalPos;
                weaponDefaultVisual.transform.localRotation = _originalWeaponLocalRot;
            }
        }
        if (weaponWarningVisual != null) weaponWarningVisual.SetActive(false);
        if (weaponSwingVisual != null) weaponSwingVisual.SetActive(false);

        if (_weaponTrigger != null)
        {
            _weaponTrigger.SetActiveTrigger(false);
        }
    }

    // ─── AI ───────────────────────────────────────────────────
    protected override void OnTick()
    {
        bool shouldLog = Time.frameCount % 60 == 0;
        if (shouldLog)
        {
            Debug.Log($"[MeleeSwingEnemy] {name} OnTick() - live: {live}, isDead: {isDead}, player: {(Player != null ? Player.name : "null")}, isAttacking: {_isAttacking}, cooldownTimer: {_cooldownTimer}, speed: {speed}");
        }

        if (Player == null)
        {
            ResolvePlayer();
            _rb2d.linearVelocity = Vector2.zero;
            SetWalk(false);
            return;
        }

        // 공격 액션(코루틴) 진행 중에는 OnTick 로직 차단
        if (_isAttacking)
        {
            if (shouldLog) Debug.Log($"[MeleeSwingEnemy] {name} OnTick() blocked because _isAttacking is true.");
            return;
        }

        // 쿨타임 타이머 연산
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
            _rb2d.linearVelocity = Vector2.zero;
            SetWalk(false);
            if (shouldLog) Debug.Log($"[MeleeSwingEnemy] {name} OnTick() blocked by cooldown. Remaining: {_cooldownTimer}");
            return;
        }

        float dist = Vector2.Distance(transform.position, Player.transform.position);
        Vector2 toPlayer = (Player.transform.position - transform.position).normalized;

        // 스프라이트 좌우 반전
        if (_sr != null && Mathf.Abs(toPlayer.x) > 0.01f)
            _sr.flipX = toPlayer.x < 0f;

        // 평상시 무기 위치 및 좌우 플립 보정
        if (weaponDefaultVisual != null && _sr != null)
        {
            Vector3 pos = weaponDefaultVisual.transform.localPosition;
            pos.x = _sr.flipX ? -Mathf.Abs(pos.x) : Mathf.Abs(pos.x);
            weaponDefaultVisual.transform.localPosition = pos;

            SpriteRenderer defaultSr = weaponDefaultVisual.GetComponent<SpriteRenderer>();
            if (defaultSr != null)
            {
                defaultSr.flipX = _sr.flipX;
            }
        }

        float range = _optimizedRange;
        if (shouldLog)
        {
            Debug.Log($"[MeleeSwingEnemy] {name} - Distance to player: {dist}, Attack Range: {range}");
        }

        if (dist > range)
        {
            // 사거리 밖: Context Steering으로 주변 몬스터를 피해 자연스럽게 접근
            Vector2 moveDir = ComputeContextSteering(toPlayer);
            _rb2d.linearVelocity = moveDir * speed;
            SetWalk(true);
            if (shouldLog)
            {
                Debug.Log($"[MeleeSwingEnemy] {name} moving to player. desiredDir: {toPlayer}, steerDir: {moveDir}, velocity: {_rb2d.linearVelocity}");
            }
        }
        else
        {
            // 사거리 안: 정지하여 공격 루틴 시작
            Debug.Log($"[MeleeSwingEnemy] {name} within range. Starting PerformAttackRoutine. Distance: {dist}, Range: {range}");
            StartCoroutine(PerformAttackRoutine());
        }
    }

    // ─── 찌르기 & 휘두르기 공격 코루틴 ──────────────────────────────
    private IEnumerator PerformAttackRoutine()
    {
        Debug.Log($"[MeleeSwingEnemy] {name} PerformAttackRoutine() started.");
        _isAttacking = true;
        _rb2d.linearVelocity = Vector2.zero;
        SetWalk(false);

        CacheOriginalWeaponTransform();

        float range = _swingSO != null ? _swingSO.attackRange : 1.6f;
        float warningTime = _swingSO != null ? _swingSO.attackWarningTime : 0.8f;
        bool isSwingType = _swingSO != null && _swingSO.attackType == MeleeAttackType.Swing;

        float curPivotOffset = _swingSO != null ? _swingSO.swingPivotOffset : swingPivotOffset;
        float curRadiusBonus = _swingSO != null ? _swingSO.swingRadiusBonus : swingRadiusBonus;
        float curThrustPrepOffset = _swingSO != null ? _swingSO.thrustPrepOffset : thrustPrepOffset;

        Vector3 dir = Vector3.right;
        float angle = 0f;

        if (Player != null)
        {
            dir = (Player.transform.position - transform.position).normalized;
            angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // 공격 시작 직전, 플레이어를 바라보도록 몬스터 스프라이트 플립 고정
            if (_sr != null && Mathf.Abs(dir.x) > 0.01f)
            {
                _sr.flipX = dir.x < 0f;
            }
        }

        // 조준 진행 각도에서 무기 자체의 스프라이트 기울기 오프셋을 빼서 보정
        float adjustedAngle = angle - weaponSpriteAngleOffset;

        // 평상시 무기 위치의 오프셋 크기를 스윙/공격 회전 반경으로 활용
        float swingRadius = _originalWeaponLocalPos.magnitude;
        if (swingRadius < 0.15f) swingRadius = 0.6f; // 너무 작을 시 기본 연출용 반지름 설정

        // 현재 1회 공격 내에서의 데미지 타격 처리 플래그 리셋
        _hasDealtDamageThisAttack = false;

        // 1. 공격 예고/준비 (Warning) 단계
        if (weaponDefaultVisual != null)
        {
            weaponDefaultVisual.SetActive(true);
            SpriteRenderer defaultSr = weaponDefaultVisual.GetComponent<SpriteRenderer>();
            if (defaultSr != null)
            {
                // 공격 도중에는 무기 스프라이트 flipX를 해제하여 오직 Z축 회전값에 의해서만 돌아가도록 조절
                defaultSr.flipX = false;
            }
        }

        if (isSwingType)
        {
            // Swing 타입: 붉은 예고선 없음. 
            // 무기를 스윙이 시작할 각도(angle + 70도)의 반지름 지점에 일치시켜 젖혀둠으로써 팝핑 현상 원천 방지!
            if (weaponWarningVisual != null) weaponWarningVisual.SetActive(false);
            if (weaponSwingVisual != null) weaponSwingVisual.SetActive(false);
            
            if (weaponDefaultVisual != null)
            {
                float startAngle = angle + 70f;
                float adjustedStartAngle = startAngle - weaponSpriteAngleOffset;
                
                // SO에 기재된 Pivot Offset 및 Radius Bonus 반영하여 준비 위치 설정
                Vector3 centerPos = dir * curPivotOffset;
                float actualRadius = swingRadius + curRadiusBonus;
                
                Vector3 prepPos = centerPos + Quaternion.Euler(0f, 0f, startAngle) * Vector3.right * actualRadius;
                weaponDefaultVisual.transform.localPosition = prepPos;
                weaponDefaultVisual.transform.localRotation = Quaternion.Euler(0f, 0f, adjustedStartAngle);
            }
        }
        else
        {
            // Thrust 타입: 빨간선 예고 없앰 (weaponWarningVisual 비활성화)
            if (weaponWarningVisual != null)
            {
                weaponWarningVisual.SetActive(false);
            }
            if (weaponSwingVisual != null) weaponSwingVisual.SetActive(false);

            // 플립 보정된 원래 무기 로컬 위치 계산
            Vector3 targetLocalPos = _originalWeaponLocalPos;
            if (_sr != null && _sr.flipX)
            {
                targetLocalPos.x = -Mathf.Abs(targetLocalPos.x);
            }

            if (weaponDefaultVisual != null)
            {
                weaponDefaultVisual.transform.localPosition = targetLocalPos + dir * curThrustPrepOffset;
                weaponDefaultVisual.transform.localRotation = Quaternion.Euler(0f, 0f, adjustedAngle);
            }
        }

        // 2. 예고 시간 대기 (0.8초 동안 준비 자세 유지)
        yield return new WaitForSeconds(warningTime);

        // 3. 실제 타격 및 공격 연출 실행 단계
        if (isSwingType)
        {
            // Swing 타입: 0.15초 동안 부채꼴로 무기를 시각적 궤적(weaponSwingVisual)과 함께 맹렬히 회전시킴
            float startAngleVal = angle + 70f;
            float endAngleVal = angle - 70f;

            // SO에 기재된 Pivot Offset 및 Radius Bonus 반영
            Vector3 centerPos = dir * curPivotOffset;
            float actualRadius = swingRadius + curRadiusBonus;

            if (weaponSwingVisual != null)
            {
                weaponSwingVisual.SetActive(true);
                Vector3 prepPos = centerPos + Quaternion.Euler(0f, 0f, startAngleVal) * Vector3.right * actualRadius;
                weaponSwingVisual.transform.localPosition = prepPos;
                float adjustedStartAngleVal = startAngleVal - weaponSpriteAngleOffset;
                weaponSwingVisual.transform.localRotation = Quaternion.Euler(0f, 0f, adjustedStartAngleVal);
            }

            if (_animator != null) _animator.SetTrigger(AnimAttack);

            float swingDuration = 0.15f;
            float elapsed = 0f;

            // 스윙 타격 동안 물리 콜라이더 활성화
            if (_weaponTrigger != null)
            {
                _weaponTrigger.SetActiveTrigger(true);
            }

            while (elapsed < swingDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / swingDuration);
                float tEased = Mathf.Sin(t * Mathf.PI * 0.5f); // EaseOut 보간
                float currentAngle = Mathf.Lerp(startAngleVal, endAngleVal, tEased);
                float adjustedCurrentAngle = currentAngle - weaponSpriteAngleOffset;

                if (weaponDefaultVisual != null)
                {
                    weaponDefaultVisual.transform.localRotation = Quaternion.Euler(0f, 0f, adjustedCurrentAngle);
                    Vector3 pos = centerPos + Quaternion.Euler(0f, 0f, currentAngle) * Vector3.right * actualRadius;
                    weaponDefaultVisual.transform.localPosition = pos;
                }
                if (weaponSwingVisual != null)
                {
                    weaponSwingVisual.transform.localRotation = Quaternion.Euler(0f, 0f, adjustedCurrentAngle);
                    Vector3 pos = centerPos + Quaternion.Euler(0f, 0f, currentAngle) * Vector3.right * actualRadius;
                    weaponSwingVisual.transform.localPosition = pos;
                }
                yield return null;
            }

            // 스윙이 끝났으므로 물리 콜라이더 비활성화
            if (_weaponTrigger != null)
            {
                _weaponTrigger.SetActiveTrigger(false);
            }

            // 스윙 종료 후: 무기가 뚝 끊기지 않고 원래 들고 다니던 평상시 오프셋으로 부드럽게 복귀 (0.15초)
            float recoverDuration = 0.15f;
            elapsed = 0f;

            Vector3 lastPos = weaponDefaultVisual != null ? weaponDefaultVisual.transform.localPosition : Vector3.zero;
            Quaternion lastRot = weaponDefaultVisual != null ? weaponDefaultVisual.transform.localRotation : Quaternion.identity;

            // 현재 바라보는 좌우 반전(flipX) 방향에 맞춰 원래 로컬 위치의 좌우 대칭 보정
            Vector3 targetLocalPos = _originalWeaponLocalPos;
            if (_sr != null && _sr.flipX)
            {
                targetLocalPos.x = -Mathf.Abs(targetLocalPos.x);
            }
            Quaternion targetLocalRot = _originalWeaponLocalRot;

            // 스윙이 끝났으므로 궤적 비활성화
            if (weaponSwingVisual != null) weaponSwingVisual.SetActive(false);

            while (elapsed < recoverDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / recoverDuration);
                float tEased = t * (2f - t); // EaseOut Quadratic

                if (weaponDefaultVisual != null)
                {
                    weaponDefaultVisual.transform.localPosition = Vector3.Lerp(lastPos, targetLocalPos, tEased);
                    weaponDefaultVisual.transform.localRotation = Quaternion.Slerp(lastRot, targetLocalRot, tEased);
                }
                yield return null;
            }
        }
        else
        {
            // Thrust 타입: 예고선 끄기
            if (weaponWarningVisual != null) weaponWarningVisual.SetActive(false);

            if (_animator != null) _animator.SetTrigger(AnimAttack);

            // 플립 보정된 원래 무기 로컬 위치 계산
            Vector3 targetLocalPos = _originalWeaponLocalPos;
            if (_sr != null && _sr.flipX)
            {
                targetLocalPos.x = -Mathf.Abs(targetLocalPos.x);
            }

            // 찌르기(돌진): 0.08초 동안 뒤로 당긴 위치에서 전방 타격 지점까지 맹렬히 뻗어나가도록 Lerp 보간
            float thrustDuration = 0.08f;
            float elapsed = 0f;
            Vector3 startPos = targetLocalPos + dir * curThrustPrepOffset;
            Vector3 endPos = targetLocalPos + dir * (range * 0.8f);

            // 찌르는 돌진 동안 물리 콜라이더 활성화
            if (_weaponTrigger != null)
            {
                _weaponTrigger.SetActiveTrigger(true);
            }

            while (elapsed < thrustDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / thrustDuration);
                float tEased = Mathf.Sin(t * Mathf.PI * 0.5f); // EaseOut

                if (weaponDefaultVisual != null)
                {
                    weaponDefaultVisual.transform.localPosition = Vector3.Lerp(startPos, endPos, tEased);
                    weaponDefaultVisual.transform.localRotation = Quaternion.Euler(0f, 0f, adjustedAngle);
                }
                yield return null;
            }

            if (weaponDefaultVisual != null)
            {
                weaponDefaultVisual.transform.localPosition = endPos;
            }

            // 찌른 지점에서 강렬한 타격감을 표현하기 위해 0.05초 동안 자세 일시정지 유지
            yield return new WaitForSeconds(0.05f);

            // 타격 단계 완료로 인한 물리 콜라이더 비활성화
            if (_weaponTrigger != null)
            {
                _weaponTrigger.SetActiveTrigger(false);
            }

            // 복귀 단계: 0.15초 동안 찌른 위치에서 평상시의 원래 대기 위치로 부드럽게 복귀 Lerp
            float recoverDuration = 0.15f;
            elapsed = 0f;

            Vector3 lastPos = weaponDefaultVisual != null ? weaponDefaultVisual.transform.localPosition : endPos;
            Quaternion lastRot = weaponDefaultVisual != null ? weaponDefaultVisual.transform.localRotation : Quaternion.Euler(0f, 0f, adjustedAngle);

            Quaternion targetLocalRot = _originalWeaponLocalRot;

            while (elapsed < recoverDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / recoverDuration);
                float tEased = t * (2f - t); // EaseOut

                if (weaponDefaultVisual != null)
                {
                    weaponDefaultVisual.transform.localPosition = Vector3.Lerp(lastPos, targetLocalPos, tEased);
                    weaponDefaultVisual.transform.localRotation = Quaternion.Slerp(lastRot, targetLocalRot, tEased);
                }
                yield return null;
            }
        }

        // 4. 최종 원복 및 안전 리셋
        ResetVisuals();

        float cooldown = _swingSO != null ? _swingSO.attackCooldown : 2.0f;
        _cooldownTimer = cooldown;
        _isAttacking = false;
        Debug.Log($"[MeleeSwingEnemy] {name} PerformAttackRoutine() finished. Cooldown set to: {cooldown}");
    }

    /// <summary>
    /// 실시간 물리 무기 콜라이더가 플레이어에게 충돌했을 때 트리거에서 호출됨
    /// </summary>
    public void OnWeaponHit(GameObject playerObj)
    {
        if (isDead) return;
        if (_hasDealtDamageThisAttack) return;

        PlayerStatControl playerStat = playerObj.GetComponent<PlayerStatControl>();
        if (playerStat != null)
        {
            // 플레이어 무적 상태가 제대로 연동되어 깎이도록 데미지 가함
            playerStat.Damage(atk);
            _hasDealtDamageThisAttack = true; // 동일 프레임/동일 스윙 중 중복 피격 원천 차단
        }
    }

    // ─── 사망 처리 ────────────────────────────────────────────
    protected override void Die()
    {
        StopAllCoroutines();
        _rb2d.linearVelocity = Vector2.zero;
        SetWalk(false);

        // 사망 시 모든 무기 비주얼 비활성화
        if (weaponDefaultVisual != null) weaponDefaultVisual.SetActive(false);
        if (weaponWarningVisual != null) weaponWarningVisual.SetActive(false);
        if (weaponSwingVisual != null) weaponSwingVisual.SetActive(false);

        if (_weaponTrigger != null)
        {
            _weaponTrigger.SetActiveTrigger(false);
        }

        base.Die();
    }

    // ─── 유틸 ─────────────────────────────────────────────────
    private void SetWalk(bool value)
    {
        _isWalkingState = value;
        if (_animator != null)
        {
            _animator.SetBool(AnimIsWalk, value);
        }

        if (_animator == null && _sr != null)
        {
            if (!value)
            {
                if (idleSprite != null)
                {
                    _sr.sprite = idleSprite;
                }
            }
        }
    }

    protected override void Update()
    {
        base.Update();

        // 걷기 애니메이션 처리 (애니메이터가 없고 walkSprites가 지정되어 있을 때)
        if (_animator == null && _isWalkingState && walkSprites != null && walkSprites.Length > 0 && _sr != null)
        {
            _walkAnimTimer += Time.deltaTime;
            if (_walkAnimTimer >= walkFrameRate)
            {
                _walkAnimTimer = 0f;
                _currentWalkFrame = (_currentWalkFrame + 1) % walkSprites.Length;
                _sr.sprite = walkSprites[_currentWalkFrame];
            }
        }
    }
}
