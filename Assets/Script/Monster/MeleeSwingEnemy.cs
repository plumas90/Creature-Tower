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

    private MeleeSwingEnemySO _swingSO;

    private Animator _animator;
    private SpriteRenderer _sr;
    private static readonly int AnimIsWalk = Animator.StringToHash("IsWalking");
    private static readonly int AnimAttack = Animator.StringToHash("Attack");

    // 상태 제어
    private bool _isAttacking = false;
    private float _cooldownTimer = 0f;

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
            Collider2D oldCollider = weaponDefaultVisual.GetComponent<Collider2D>();
            BoxCollider2D boxCol = weaponDefaultVisual.GetComponent<BoxCollider2D>();

            if (boxCol == null)
            {
                if (oldCollider != null)
                {
                    Destroy(oldCollider);
                }
                boxCol = weaponDefaultVisual.AddComponent<BoxCollider2D>();
            }

            boxCol.size = localB.size;
            boxCol.offset = localB.center;
            boxCol.isTrigger = true;
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
        if (Player == null)
        {
            ResolvePlayer();
            _rb2d.linearVelocity = Vector2.zero;
            SetWalk(false);
            return;
        }

        // 공격 액션(코루틴) 진행 중에는 OnTick 로직 차단
        if (_isAttacking) return;

        // 쿨타임 타이머 연산
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
            _rb2d.linearVelocity = Vector2.zero;
            SetWalk(false);
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

        if (dist > range)
        {
            // 사거리 밖: 플레이어를 향해 접근
            _rb2d.linearVelocity = toPlayer * speed;
            SetWalk(true);
        }
        else
        {
            // 사거리 안: 정지하여 공격 루틴 시작
            StartCoroutine(PerformAttackRoutine());
        }
    }

    // ─── 찌르기 & 휘두르기 공격 코루틴 ──────────────────────────────
    private IEnumerator PerformAttackRoutine()
    {
        _isAttacking = true;
        _rb2d.linearVelocity = Vector2.zero;
        SetWalk(false);

        CacheOriginalWeaponTransform();

        float range = _swingSO != null ? _swingSO.attackRange : 1.6f;
        float warningTime = _swingSO != null ? _swingSO.attackWarningTime : 0.8f;
        bool isSwingType = _swingSO != null && _swingSO.attackType == MeleeAttackType.Swing;

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
                Vector3 prepPos = Quaternion.Euler(0f, 0f, startAngle) * Vector3.right * swingRadius;
                weaponDefaultVisual.transform.localPosition = prepPos;
                weaponDefaultVisual.transform.localRotation = Quaternion.Euler(0f, 0f, startAngle);
            }
        }
        else
        {
            // Thrust 타입: 붉은 예고선 ON. 무기는 조준선상의 뒤쪽으로 당겨 찌르기 대기 자세 취함
            if (weaponWarningVisual != null)
            {
                weaponWarningVisual.SetActive(true);
                weaponWarningVisual.transform.localPosition = dir * (range * 0.6f);
                weaponWarningVisual.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            if (weaponSwingVisual != null) weaponSwingVisual.SetActive(false);

            if (weaponDefaultVisual != null)
            {
                weaponDefaultVisual.transform.localPosition = -dir * 0.4f;
                weaponDefaultVisual.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
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

            if (weaponSwingVisual != null)
            {
                weaponSwingVisual.SetActive(true);
                Vector3 prepPos = Quaternion.Euler(0f, 0f, startAngleVal) * Vector3.right * swingRadius;
                weaponSwingVisual.transform.localPosition = prepPos;
                weaponSwingVisual.transform.localRotation = Quaternion.Euler(0f, 0f, startAngleVal);
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

                if (weaponDefaultVisual != null)
                {
                    weaponDefaultVisual.transform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);
                    Vector3 pos = Quaternion.Euler(0f, 0f, currentAngle) * Vector3.right * swingRadius;
                    weaponDefaultVisual.transform.localPosition = pos;
                }
                if (weaponSwingVisual != null)
                {
                    weaponSwingVisual.transform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);
                    Vector3 pos = Quaternion.Euler(0f, 0f, currentAngle) * Vector3.right * swingRadius;
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

            // 찌르기(돌진): 0.08초 동안 뒤로 당긴 위치에서 전방 타격 지점까지 맹렬히 뻗어나가도록 Lerp 보간
            float thrustDuration = 0.08f;
            float elapsed = 0f;
            Vector3 startPos = -dir * 0.4f;
            Vector3 endPos = dir * (range * 0.8f);

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
                    weaponDefaultVisual.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
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
            Quaternion lastRot = weaponDefaultVisual != null ? weaponDefaultVisual.transform.localRotation : Quaternion.Euler(0f, 0f, angle);

            Vector3 targetLocalPos = _originalWeaponLocalPos;
            if (_sr != null && _sr.flipX)
            {
                targetLocalPos.x = -Mathf.Abs(targetLocalPos.x);
            }
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
        if (_animator == null) return;
        _animator.SetBool(AnimIsWalk, value);
    }
}
