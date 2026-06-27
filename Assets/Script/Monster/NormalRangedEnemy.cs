using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 원거리 근접 몬스터.
/// - 사거리 안에 들어오면 정지하고 플레이어를 향해 투사체를 발사한다.
/// - 플레이어가 최소 유지거리보다 가까워지면 뒤로 물러난다.
/// - 한 번에 여러 발 발사(부채꼴 배열) 가능.
/// - 투사체는 기존 Bullet 시스템 사용 (BulletTarget.Player 설정).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class NormalRangedEnemy : EnemyBase
{
    [Header("Spawn Configurations")]
    [Tooltip("총알이 생성될 위치를 지정하는 Transform입니다. 지정하지 않으면 몬스터의 중심에서 생성됩니다.")]
    [SerializeField] private Transform bulletSpawnPoint;

    // SO 캐스팅 캐시
    private RangedEnemySO _rangedSO;

    private Animator _animator;
    private SpriteRenderer _sr;
    private static readonly int AnimIsWalk = Animator.StringToHash("IsWalking");
    private static readonly int AnimAttack = Animator.StringToHash("Attack");

    // 투사체 타겟 딕셔너리 (Bullet.cs 호환)
    private Dictionary<string, int> _projectileTargets;

    // 공격 쿨타임 타이머
    private float _atkTimer;

    // ─── 초기화 ───────────────────────────────────────────────
    public override void StatSet(EnemySO so = null)
    {
        base.StatSet(so);
        _rangedSO = MainSO as RangedEnemySO;
        if (_rangedSO == null)
            Debug.LogWarning($"[NormalRangedEnemy] MainSO is not RangedEnemySO: {name}");

        _atkTimer = 0f;
    }

    protected override void Start()
    {
        base.Start();
        _animator = GetComponentInChildren<Animator>();
        _sr       = GetComponentInChildren<SpriteRenderer>();

        // 투사체 타겟: 플레이어만 맞추는 총알
        _projectileTargets = new Dictionary<string, int>
        {
            { "Player", (int)BulletTarget.Player }
        };
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

        float dist = Vector2.Distance(transform.position, Player.transform.position);
        Vector2 toPlayer = (Player.transform.position - transform.position).normalized;

        // 방향에 따른 스프라이트 반전
        if (_sr != null && Mathf.Abs(toPlayer.x) > 0.01f)
            _sr.flipX = toPlayer.x < 0f;

        float range      = _rangedSO != null ? _rangedSO.attackRange  : 8f;
        float keepDist   = _rangedSO != null ? _rangedSO.keepDistance : 4f;

        if (dist > range)
        {
            // 사거리 밖: 플레이어를 향해 접근
            _rb2d.linearVelocity = toPlayer * speed;
            SetWalk(true);
        }
        else if (dist < keepDist)
        {
            // 너무 가까움: 뒤로 물러남
            Vector2 fleeDir = -toPlayer;

            // 벽 우회 회피 옵션이 켜진 경우
            if (_rangedSO != null && _rangedSO.evadeObstaclesWhileFleeing)
            {
                int wallMask = 1 << LayerMask.NameToLayer("Wall");
                float detectDist = 1.0f; // 감지할 벽 거리
                
                // 도망 방향에 벽이 있는지 Raycast 검사
                RaycastHit2D hit = Physics2D.Raycast(transform.position, fleeDir, detectDist, wallMask);
                if (hit.collider != null)
                {
                    // 수직인 양옆 방향 계산
                    Vector2 leftDir = new Vector2(-fleeDir.y, fleeDir.x).normalized;
                    Vector2 rightDir = new Vector2(fleeDir.y, -fleeDir.x).normalized;

                    // 양옆 방향의 장애물 감지 거리 측정
                    RaycastHit2D hitLeft = Physics2D.Raycast(transform.position, leftDir, detectDist, wallMask);
                    RaycastHit2D hitRight = Physics2D.Raycast(transform.position, rightDir, detectDist, wallMask);

                    float leftDist = hitLeft.collider != null ? hitLeft.distance : detectDist;
                    float rightDist = hitRight.collider != null ? hitRight.distance : detectDist;

                    // 덜 막혀있는(거리가 더 먼) 쪽으로 방향 선택
                    if (leftDist >= rightDist && leftDist > 0.1f)
                    {
                        fleeDir = leftDir;
                    }
                    else if (rightDist > leftDist && rightDist > 0.1f)
                    {
                        fleeDir = rightDir;
                    }
                }
            }

            _rb2d.linearVelocity = fleeDir * speed;
            SetWalk(true);

            if (_rangedSO != null && _rangedSO.attackWhileFleeing)
            {
                _atkTimer -= Time.deltaTime;
                if (_atkTimer <= 0f)
                {
                    FireProjectiles(toPlayer);
                    float cooldown = _rangedSO.attackCooldown;
                    _atkTimer = cooldown;
                }
            }
        }
        else
        {
            // 사거리 안: 정지 후 공격
            _rb2d.linearVelocity = Vector2.zero;
            SetWalk(false);

            _atkTimer -= Time.deltaTime;
            if (_atkTimer <= 0f)
            {
                FireProjectiles(toPlayer);
                float cooldown = _rangedSO != null ? _rangedSO.attackCooldown : 2f;
                _atkTimer = cooldown;
            }
        }
    }

    // ─── 투사체 발사 ──────────────────────────────────────────
    protected virtual Vector3 GetBulletSpawnPosition()
    {
        if (bulletSpawnPoint != null)
        {
            if (_sr != null && _sr.flipX)
            {
                Vector3 offset = bulletSpawnPoint.position - transform.position;
                offset.x = -offset.x;
                return transform.position + offset;
            }
            return bulletSpawnPoint.position;
        }
        return transform.position;
    }

    protected virtual void FireProjectiles(Vector2 toPlayer)
    {
        if (_rangedSO == null || _rangedSO.projectilePrefab == null)
        {
            Debug.LogWarning($"[NormalRangedEnemy] projectilePrefab이 RangedEnemySO에 설정되지 않았습니다: {name}");
            return;
        }

        int count        = _rangedSO.projectileCount;
        float spread     = _rangedSO.spreadAngle;
        float projSpeed  = _rangedSO.projectileSpeed;
        float lifeTime   = _rangedSO.projectileLifeTime;
        float damage     = atk;

        // 발사 기준 각도
        float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

        if (count <= 1)
        {
            SpawnBullet(baseAngle, damage, projSpeed, lifeTime);
        }
        else
        {
            // 부채꼴: count발을 spread 범위 안에 균등 분배
            float halfSpread = spread * 0.5f;
            float step       = count > 1 ? spread / (count - 1) : 0f;

            for (int i = 0; i < count; i++)
            {
                float angle = baseAngle - halfSpread + step * i;
                SpawnBullet(angle, damage, projSpeed, lifeTime);
            }
        }

        // 공격 애니메이션 트리거
        if (_animator != null)
            _animator.SetTrigger(AnimAttack);
    }

    protected virtual void SpawnBullet(float angleDeg, float damage, float projSpeed, float lifeTime)
    {
        Quaternion rot = Quaternion.Euler(0f, 0f, angleDeg);
        GameObject obj = Object.Instantiate(_rangedSO.projectilePrefab, GetBulletSpawnPosition(), rot);

        Bullet bullet = obj.GetComponent<Bullet>();
        if (bullet == null)
        {
            Debug.LogWarning($"[NormalRangedEnemy] projectilePrefab에 Bullet 컴포넌트가 없습니다.");
            Object.Destroy(obj);
            return;
        }

        bullet.ATK            = damage;
        bullet.BulletSpeed    = projSpeed;
        bullet.BulletLifeTime = lifeTime;
        bullet.targets        = _projectileTargets;
        bullet.IsDamage       = true;
        bullet.Init();

        obj.SetActive(true);
    }

    // ─── 사망 ─────────────────────────────────────────────────
    protected override void Die()
    {
        _rb2d.linearVelocity = Vector2.zero;
        SetWalk(false);
        base.Die();
    }

    // ─── 유틸 ─────────────────────────────────────────────────
    private void SetWalk(bool value)
    {
        if (_animator == null) return;
        _animator.SetBool(AnimIsWalk, value);
    }
}
